#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace TodaRush.EditorTools
{
    // Editor-only visual review. No Play mode, scene loading or gameplay logic.
    // Editor-only manual review tool; never auto-runs when a project opens.
    public static class StaticHUDGameViewReview
    {
        const string PrefabPath="Assets/TodaRushUI/Prefabs/GameplayHUD.prefab";
        const string Output="C:/Users/quian/Documents/Codex/2026-11-12/unity-plugin-unity-openai-curated-remote/outputs";
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        static GameObject preview;
        static readonly List<(Canvas canvas,bool enabled)> canvases=new List<(Canvas,bool)>();
        static readonly List<(Camera camera,CameraClearFlags flags,Color color)> cameras=new List<(Camera,CameraClearFlags,Color)>();
        static EditorWindow view;
        static int oldSize,index;
        static double next;
        static string report;
        static readonly Vector2Int[] sizes={new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720)};

        static void Auto(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Auto;return;}if(!File.Exists(Output+"/HUD-GameView-Review.txt")||!File.ReadAllText(Output+"/HUD-GameView-Review.txt").Contains("SCREENSHOTS ORIENTED"))StartReview();}
        static void Check(bool value,string name){if(!value)throw new Exception(name);report+="PASS "+name+"\n";}
        [MenuItem("Tools/TODA-RUSH/Review Static HUD In Game View")]
        public static void StartReview()
        {
            try
            {
                if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before this UI-only review.");
                Directory.CreateDirectory(Output);report="";index=0;
                Check(!EditorUtility.scriptCompilationFailed,"No compilation errors");
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);Check(asset,"Existing GameplayHUD prefab found");
                foreach(var t in asset.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing scripts: "+t.name);
                Check(asset.GetComponentsInChildren<MonoBehaviour>(true).All(c=>c&&(c.GetType().Namespace=="UnityEngine.UI"||c.GetType().Namespace=="TMPro")),"HUD uses only standard uGUI/TMP components");
                var scaler=asset.GetComponent<UnityEngine.UI.CanvasScaler>();Check(scaler.referenceResolution==new Vector2(1920,1080)&&scaler.matchWidthOrHeight==.5f&&scaler.uiScaleMode==UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize,"Existing CanvasScaler preserved");
                Check(asset.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"Screen Space Overlay preserved");
                var icons=asset.transform.Find("PassengerPanel/PassengerIcons");Check(icons.childCount==3,"Three static passenger indicators");
                Sprite passenger=null;for(int n=0;n<3;n++)
                {
                    var image=icons.GetChild(n).Find("CircleBackground").GetComponent<UnityEngine.UI.Image>();Check(new SerializedObject(image).FindProperty("m_Color")!=null,"Passenger "+(n+1)+" circle color editable in Inspector");
                    Check(image.color==(Color)(n<2?new Color32(62,155,108,255):new Color32(119,119,119,255)),"Passenger "+(n+1)+" static color");
                    var person=icons.GetChild(n).Find("CircleBackground/PassengerIcon").GetComponent<UnityEngine.UI.Image>();Check(person.color==Color.white&&person.sprite,"Passenger silhouette white and assigned");if(n==0)passenger=person.sprite;else Check(person.sprite==passenger,"Reusable passenger PNG shared");
                }
                string png=AssetDatabase.GetAssetPath(passenger.texture);var bytes=File.ReadAllBytes(png);Check(png.EndsWith(".png")&&bytes.Length>26&&bytes[25]==6,"Passenger PNG has RGBA transparency");
                foreach(var text in asset.GetComponentsInChildren<TMP_Text>(true))Check(text.font&&text.fontSharedMaterial,"TMP references assigned: "+text.name);
                Check(asset.transform.Find("TimerPanel/TimerValue").GetComponent<TMP_Text>().text=="01:24","Static timer 01:24");Check(asset.transform.Find("ScorePanel/ScoreValue").GetComponent<TMP_Text>().text=="12,480","Static score 12,480");Check(asset.transform.Find("HealthPanel/HealthValue").GetComponent<TMP_Text>().text=="68 / 100","Static health 68/100");Check(asset.transform.Find("DestinationPanel/DestinationName").GetComponent<TMP_Text>().text=="PUROK 4 TERMINAL","Static destination");
                // Instantiate only a transient UI overlay in an already open scene. Never save the scene.
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/LevelSelection.unity");Check(scene.isLoaded,"Existing LevelSelection scene remains loaded");
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var c in root.GetComponentsInChildren<Canvas>(true)){canvases.Add((c,c.enabled));c.enabled=false;}
                    foreach(var c in root.GetComponentsInChildren<Camera>(true)){cameras.Add((c,c.clearFlags,c.backgroundColor));c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.14f,.20f,.26f);}
                }
                preview=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);preview.name="HUD_UI_ONLY_REVIEW_UNSAVED";preview.hideFlags=HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;
                var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");view=EditorWindow.GetWindow(type);oldSize=(int)type.GetProperty("selectedSizeIndex").GetValue(view);
                EditorApplication.update-=Tick;EditorApplication.update+=Tick;SelectSize();
            }
            catch(Exception e){Fail(e);}
        }
        static void SelectSize()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameViewSizes");var single=typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);var group=type.GetMethod("GetGroup").Invoke(single,new object[]{0});int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);int selected=-1;
            for(int i=0;i<count;i++){var size=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});var st=size.GetType();if((int)st.GetProperty("width").GetValue(size)==sizes[index].x&&(int)st.GetProperty("height").GetValue(size)==sizes[index].y){selected=i;break;}}
            if(selected<0){var st=assembly.GetType("UnityEditor.GameViewSize");var et=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(st,new object[]{Enum.Parse(et,"FixedResolution"),sizes[index].x,sizes[index].y,"HUD UI "+sizes[index]});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});selected=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;}
            view.GetType().GetProperty("selectedSizeIndex").SetValue(view,selected);view.Focus();view.Repaint();EditorApplication.QueuePlayerLoopUpdate();next=EditorApplication.timeSinceStartup+5;
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup<next){view.Repaint();EditorApplication.QueuePlayerLoopUpdate();return;}
                Canvas.ForceUpdateCanvases();
                foreach(var text in preview.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Text fits "+sizes[index]+" "+text.name);}
                var canvas=preview.GetComponent<Canvas>();Check(Mathf.Abs(canvas.GetComponent<RectTransform>().rect.width-1920)<2,"HUD anchor space remains 1920 wide at "+sizes[index]);
                foreach(string section in new[]{"PassengerPanel","TimerPanel","DestinationPanel","ScorePanel","HealthPanel","MiniMapPanel"})
                {var rect=preview.transform.Find(section).GetComponent<RectTransform>();var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var corner in corners){Vector2 point=RectTransformUtility.WorldToScreenPoint(null,corner);Check(point.x>=-2&&point.y>=-2&&point.x<=sizes[index].x+2&&point.y<=sizes[index].y+2,"Panel inside Game view "+sizes[index]+" "+section);}}
                RenderTexture texture=null;for(Type t=view.GetType();t!=null&&texture==null;t=t.BaseType)foreach(var field in t.GetFields(Flags|BindingFlags.DeclaredOnly))if(typeof(RenderTexture).IsAssignableFrom(field.FieldType)){var rt=field.GetValue(view) as RenderTexture;if(rt&&rt.width==sizes[index].x&&rt.height==sizes[index].y){texture=rt;break;}}
                Check(texture,"Actual Game view render texture "+sizes[index]);
                var old=RenderTexture.active;RenderTexture.active=texture;var image=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);if(SystemInfo.graphicsUVStartsAtTop){var pixels=image.GetPixels32();var flipped=new Color32[pixels.Length];for(int y=0;y<texture.height;y++)Array.Copy(pixels,y*texture.width,flipped,(texture.height-1-y)*texture.width,texture.width);image.SetPixels32(flipped);}image.Apply();File.WriteAllBytes(Output+"/HUD-GameView-"+sizes[index].x+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
                index++;if(index<sizes.Length){SelectSize();return;}
                Restore();PrefabUtility.SavePrefabAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));report+="SCREENSHOTS ORIENTED. PASS Existing prefab saved; no scene saved\nFonts missing: Barlow Condensed and Inter. Existing Liberation Sans SDF placeholder retained and reported.\n";File.WriteAllText(Output+"/HUD-GameView-Review.txt",report);AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            }
            catch(Exception e){Fail(e);}
        }
        static void Restore()
        {
            EditorApplication.update-=Tick;if(preview)UnityEngine.Object.DestroyImmediate(preview);
            foreach(var entry in canvases)if(entry.canvas)entry.canvas.enabled=entry.enabled;canvases.Clear();
            foreach(var entry in cameras)if(entry.camera){entry.camera.clearFlags=entry.flags;entry.camera.backgroundColor=entry.color;}cameras.Clear();
            if(view){view.GetType().GetProperty("selectedSizeIndex").SetValue(view,oldSize);view.Repaint();}
        }
        static void Fail(Exception e){Restore();File.WriteAllText(Output+"/HUD-GameView-Review-Error.txt",report+"\n"+e);Debug.LogException(e);}
    }
}
#endif



