#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System;
using System.IO;
using System.Linq;

namespace TodaRush.EditorTools
{
    [InitializeOnLoad]
    public static class GameplayHUDBuilder
    {
        const string Root="Assets/TodaRushUI";
        const string Output="C:/Users/quian/Documents/Codex/2026-11-12/unity-plugin-unity-openai-curated-remote/outputs";
        const string PrefabPath=Root+"/Prefabs/GameplayHUD.prefab";
        static string report="";
        static GameplayHUDBuilder(){EditorApplication.delayCall+=FinalAudit;}
        static void FinalAudit()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=FinalAudit;return;}
            if(!File.Exists(Output+"/StaticHUD-Verified.txt"))return;
            try
            {
                var root=PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    if(root.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Missing prefab script");
                    if(root.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>!c || c.GetType().Namespace=="TodaRush.UI"))throw new Exception("Unexpected runtime HUD component");
                    if(root.GetComponentsInChildren<TMP_Text>(true).Any(t=>!t.font || !t.fontSharedMaterial))throw new Exception("Missing TMP font/material");
                    if(root.transform.Find("PassengerPanel/PassengerIcons").GetComponentsInChildren<UnityEngine.UI.Image>().Any(i=>!i.sprite))throw new Exception("Passenger sprite missing");
                    if(EditorUtility.scriptCompilationFailed)throw new Exception("Compilation failed");
                    if(File.ReadAllText("Assets/TodaRushLevelSelection/LevelSelectionController.cs").Contains("GameplayHUDLaunchContext"))throw new Exception("Launch hook still present");
                    File.AppendAllText(Output+"/StaticHUD-Verified.txt","FINAL AUDIT: clean compilation, no custom runtime components, no missing scripts or UI font/sprite references; launch hook removed.\n");
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            catch(Exception e){File.WriteAllText(Output+"/StaticHUD-FinalAudit-Error.txt",e.ToString());Debug.LogException(e);}
        }
        static void AutoConvert()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=AutoConvert;return;}
            if(File.Exists(Output+"/StaticHUD-Verified.txt")&&File.ReadAllText(Output+"/StaticHUD-Verified.txt").Contains("STATIC DESIGN FINAL"))return;
            try{Convert();}catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/StaticHUD-Error.txt",e.ToString());Debug.LogException(e);}
        }
        static void Check(bool pass,string name){if(!pass)throw new Exception("Static HUD verification: "+name);report+="PASS "+name+"\n";}
        static void RemoveNewRuntimeComponents(GameObject root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach(var c in t.GetComponents<MonoBehaviour>())
                    if(c && c.GetType().Namespace=="TodaRush.UI")UnityEngine.Object.DestroyImmediate(c);
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }
        static void SetText(GameObject root,string path,string value)
        {var t=root.transform.Find(path).GetComponent<TMP_Text>();t.text=value;t.color=path.Contains("ScoreValue")?new Color32(255,194,71,255):path.Contains("DestinationName")?new Color32(37,39,43,255):new Color32(255,248,235,255);}
        static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 position,Vector2 size,Color color,Sprite sprite=null)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));g.layer=5;g.transform.SetParent(parent,false);
            var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;
            var im=g.GetComponent<UnityEngine.UI.Image>();im.color=color;im.sprite=sprite;im.raycastTarget=false;return im;
        }
        [MenuItem("Tools/TODA-RUSH/Convert HUD To Static Visuals")]
        public static void Convert()
        {
            report="";Directory.CreateDirectory(Output);
            // Prefab editing is confined to assets created for this HUD task.
            var indicator=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/PassengerIndicator.prefab");
            try{RemoveNewRuntimeComponents(indicator);indicator.transform.Find("CircleBackground").GetComponent<UnityEngine.UI.Image>().color=new Color32(119,119,119,255);indicator.transform.Find("CircleBackground/PassengerIcon").GetComponent<UnityEngine.UI.Image>().color=Color.white;PrefabUtility.SaveAsPrefabAsset(indicator,Root+"/Prefabs/PassengerIndicator.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(indicator);}
            var hud=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                RemoveNewRuntimeComponents(hud);
                SetText(hud,"PassengerPanel/PassengerCounter","2 / 3");SetText(hud,"TimerPanel/TimerValue","01:24");SetText(hud,"ScorePanel/ScoreValue","12,480");SetText(hud,"HealthPanel/HealthValue","68 / 100");SetText(hud,"DestinationPanel/DestinationName","PUROK 4 TERMINAL");
                var timerHeader=hud.transform.Find("TimerPanel/TimerHeader").GetComponent<TMP_Text>();timerHeader.fontSize=28;timerHeader.rectTransform.sizeDelta=new Vector2(128,40);timerHeader.rectTransform.anchoredPosition=new Vector2(28,-72);
                var fill=hud.transform.Find("HealthPanel/HealthBarFill").GetComponent<UnityEngine.UI.Image>();fill.fillAmount=.68f;fill.color=new Color32(62,155,108,255);
                var list=hud.transform.Find("PassengerPanel/PassengerIcons");foreach(Transform child in list.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                var indicatorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/PassengerIndicator.prefab");
                for(int i=0;i<3;i++){var slot=(GameObject)PrefabUtility.InstantiatePrefab(indicatorPrefab,list);slot.name="PassengerIndicator"+(i+1);slot.transform.Find("CircleBackground").GetComponent<UnityEngine.UI.Image>().color=i<2?new Color32(62,155,108,255):new Color32(119,119,119,255);}
                // A plain hierarchy of colored Images: no route graphic, tracking or data source.
                var viewport=hud.transform.Find("MiniMapPanel/MapViewport");var route=viewport.Find("RouteLine");if(!route){var g=new GameObject("RouteLine",typeof(RectTransform));g.transform.SetParent(viewport,false);route=g.transform;}
                foreach(Transform child in route.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                var circle=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/CircleWhite.png");
                var rr=route.GetComponent<RectTransform>();rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.offsetMin=rr.offsetMax=Vector2.zero;
                Vector2[] points={new Vector2(-119,-101),new Vector2(-102,-67),new Vector2(-74,-35),new Vector2(-38,-11),new Vector2(8,0),new Vector2(48,9),new Vector2(76,34),new Vector2(96,70),new Vector2(115,104)};
                for(int pass=0;pass<2;pass++)
                {
                    float thickness=pass==0?13:7;Color color=pass==0?new Color32(255,248,235,255):new Color32(70,181,216,255);
                    for(int i=1;i<points.Length;i++){var d=points[i]-points[i-1];var segment=Image((pass==0?"RouteBorder":"BlueRouteSegment")+i,route,(points[i]+points[i-1])*.5f,new Vector2(d.magnitude,thickness),color);segment.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
                    for(int i=0;i<points.Length;i++)Image((pass==0?"BorderJoin":"BlueJoin")+i,route,points[i],new Vector2(thickness,thickness),color,circle);
                }
                var player=viewport.Find("PlayerMarker").GetComponent<RectTransform>();player.gameObject.SetActive(true);player.anchorMin=player.anchorMax=player.pivot=new Vector2(.5f,.5f);player.anchoredPosition=points[0];player.localRotation=Quaternion.identity;player.SetAsLastSibling();
                var destination=viewport.Find("DestinationMarker").GetComponent<RectTransform>();destination.gameObject.SetActive(true);destination.anchorMin=destination.anchorMax=destination.pivot=new Vector2(.5f,.5f);destination.anchoredPosition=points.Last();destination.SetAsLastSibling();
                var oldRing=viewport.Find("PlayerMarkerRing");if(oldRing)UnityEngine.Object.DestroyImmediate(oldRing.gameObject);Image("PlayerMarkerRing",viewport,points[0],new Vector2(28,28),Color.white,circle);player.SetAsLastSibling();
                var status=viewport.Find("NavigationStatus").GetComponent<TMP_Text>();status.text="";
                // Keep ordinary marker Images editable; no preview/behaviour component is needed.
                foreach(var image in hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))image.raycastTarget=false;
                Check(hud.GetComponentsInChildren<MonoBehaviour>(true).All(c=>c && (c.GetType().Namespace=="UnityEngine.UI"||c.GetType().Namespace=="TMPro")),"Only standard uGUI/TMP components in HUD");
                Check(hud.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No missing scripts");
                Check(list.childCount==3,"Three editable passenger indicator instances");
                var sprites=list.GetComponentsInChildren<UnityEngine.UI.Image>().Where(i=>i.name=="PassengerIcon").Select(i=>i.sprite).Distinct().ToArray();Check(sprites.Length==1&&sprites[0],"One reusable passenger PNG sprite");
                Check(list.GetChild(0).Find("CircleBackground").GetComponent<UnityEngine.UI.Image>().color==(Color)new Color32(62,155,108,255)&&list.GetChild(2).Find("CircleBackground").GetComponent<UnityEngine.UI.Image>().color==(Color)new Color32(119,119,119,255),"Static 2 green / 1 gray circles");
                PrefabUtility.SaveAsPrefabAsset(hud,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(hud);}
            // Remove only the obsolete HUD reference by reserializing this component's scene.
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/LevelSelection.unity");bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/LevelSelection.unity",OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())foreach(var controller in root.GetComponentsInChildren<LevelSelectionController>(true))
            {Check(new SerializedObject(controller).FindProperty("gameplayHUDPrefab")==null,"HUD launch field removed; existing level selection retained");EditorUtility.SetDirty(controller);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            AssetDatabase.SaveAssets();
            Capture(1920,1080);Capture(1600,900);Capture(1280,720);
            Check(!EditorUtility.scriptCompilationFailed,"Unity compilation clean");
            File.WriteAllText(Output+"/StaticHUD-Verified.txt",report+"\nSTATIC DESIGN FINAL. Visual-only prefab. No gameplay simulation, scene launch, data binding or navigation behaviour.\nBarlow Condensed Bold / Inter unavailable; Liberation Sans SDF remains the documented temporary font.\n");
            EditorApplication.delayCall+=()=>{AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));EditorApplication.delayCall+=VerifyOpen;};
        }
        static void VerifyOpen()
        {var stage=UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null&&stage.assetPath==PrefabPath)File.AppendAllText(Output+"/StaticHUD-Verified.txt","PASS GameplayHUD opened in Unity Prefab Mode\n");}
        static void Capture(int width,int height)
        {
            var scene=EditorSceneManager.NewPreviewScene();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);var instance=UnityEngine.Object.Instantiate(prefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance,scene);
            var cg=new GameObject("StaticHUDPreviewCamera",typeof(Camera));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);var camera=cg.GetComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.20f,.26f);camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=100;
            var canvas=instance.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var texture=new RenderTexture(width,height,24);camera.targetTexture=texture;texture.Create();instance.GetComponent<UnityEngine.UI.CanvasScaler>().enabled=false;canvas.scaleFactor=width/1920f;Canvas.ForceUpdateCanvases();
            foreach(var t in instance.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate();
            camera.Render();var old=RenderTexture.active;RenderTexture.active=texture;var png=new Texture2D(width,height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(Output+"/StaticHUD-Preview-"+width+".png",png.EncodeToPNG());foreach(var t in instance.GetComponentsInChildren<TMP_Text>())Check(!t.isTextOverflowing,"Text fit "+width+" "+t.name);RenderTexture.active=old;camera.targetTexture=null;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(png);EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif




