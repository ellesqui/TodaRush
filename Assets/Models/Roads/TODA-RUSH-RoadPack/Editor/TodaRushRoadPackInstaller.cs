#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TodaRushRoadPackInstaller
{
    private const string Root = "Assets/TODA-RUSH-RoadPack";
    private const string ManifestPath = Root + "/Data/RoadPackManifest.json";
    private const string MaterialsRoot = Root + "/Materials";
    private const string PrefabsRoot = Root + "/Prefabs";

    [Serializable] private class Manifest { public MaterialEntry[] materials; public RoadEntry[] road_objects; public AccessoryEntry[] accessories; }
    [Serializable] private class MaterialEntry { public string name, baseColor, normal, roughness, ao, metallicSmoothness; public float tileSizeMeters; }
    [Serializable] private class RoadEntry { public string name, kind, material, modelPath; }
    [Serializable] private class AccessoryEntry { public string name, category, material, modelPath; }

    [MenuItem("TODA-RUSH/Road Pack/Build Materials and Prefabs")]
    public static void Build()
    {
        if (!File.Exists(ManifestPath)) { Debug.LogError("TODA-RUSH Road Pack manifest missing: " + ManifestPath); return; }
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
        if (manifest == null) { Debug.LogError("TODA-RUSH Road Pack manifest could not be parsed."); return; }
        EnsureFolder(MaterialsRoot); EnsureFolder(PrefabsRoot);
        AssetDatabase.Refresh();
        ConfigureModelImporters(manifest);
        var mats = BuildMaterials(manifest.materials);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        BuildRoadPrefabs(manifest.road_objects, mats);
        BuildAccessoryPrefabs(manifest.accessories, mats);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("TODA-RUSH Road Pack installed. Review prefabs in " + PrefabsRoot + " before placing RoadNetwork_Main into a test scene.");
    }

    private static void ConfigureModelImporters(Manifest manifest)
    {
        var paths = new List<string>();
        foreach (var e in manifest.road_objects ?? Array.Empty<RoadEntry>()) paths.Add(ToAssetPath(e.modelPath));
        foreach (var e in manifest.accessories ?? Array.Empty<AccessoryEntry>()) paths.Add(ToAssetPath(e.modelPath));
        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { Debug.LogWarning("Model importer not found: " + path); continue; }
            bool changed = false;
            if (Math.Abs(importer.globalScale - 1f) > 0.0001f) { importer.globalScale = 1f; changed = true; }
            if (!importer.isReadable) { importer.isReadable = true; changed = true; }
            if (importer.meshCompression != ModelImporterMeshCompression.Off) { importer.meshCompression = ModelImporterMeshCompression.Off; changed = true; }
            if (importer.importNormals != ModelImporterNormals.Import) { importer.importNormals = ModelImporterNormals.Import; changed = true; }
            if (changed) importer.SaveAndReimport();
        }
        AssetDatabase.Refresh();
    }

    private static Dictionary<string, Material> BuildMaterials(MaterialEntry[] entries)
    {
        var result = new Dictionary<string, Material>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) { Debug.LogError("No supported Lit or Standard shader found."); return result; }
        foreach (var e in entries ?? Array.Empty<MaterialEntry>())
        {
            string path = MaterialsRoot + "/" + e.name + ".mat";
            Material old = AssetDatabase.LoadAssetAtPath<Material>(path);
            Material mat = new Material(shader) { name = e.name, enableInstancing = true };
            Texture2D baseTex = LoadTexture(e.baseColor, false);
            Texture2D normalTex = LoadTexture(e.normal, true);
            Texture2D aoTex = LoadTexture(e.ao, false);
            Texture2D msTex = LoadTexture(e.metallicSmoothness, false);
            SetTexture(mat, new[]{"_BaseMap", "_MainTex"}, baseTex);
            SetTexture(mat, new[]{"_BumpMap"}, normalTex);
            SetTexture(mat, new[]{"_OcclusionMap"}, aoTex);
            SetTexture(mat, new[]{"_MetallicGlossMap"}, msTex);
            SetFloat(mat, new[]{"_Metallic"}, 0f);
            SetFloat(mat, new[]{"_BumpScale"}, 0.55f);
            SetFloat(mat, new[]{"_OcclusionStrength"}, 0.75f);
            SetFloat(mat, new[]{"_Smoothness", "_Glossiness"}, 0.45f);
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_METALLICGLOSSMAP");
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.EnableKeyword("_OCCLUSIONMAP");
            if (old != null) { EditorUtility.CopySerialized(mat, old); UnityEngine.Object.DestroyImmediate(mat); mat = old; }
            else AssetDatabase.CreateAsset(mat, path);
            EditorUtility.SetDirty(mat);
            result[e.name] = mat;
        }
        return result;
    }

    private static string ToAssetPath(string path)
    {
        return Root + "/" + path.Replace("Unity_Assets/TODA-RUSH-RoadPack/", "");
    }

    private static Texture2D LoadTexture(string relativePath, bool normal)
    {
        string path = ToAssetPath(relativePath);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !path.Contains("Roughness") && !path.Contains("AO") && !path.Contains("MetallicSmoothness");
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void SetTexture(Material m, string[] names, Texture t)
    {
        if (t == null) return;
        foreach (string n in names) if (m.HasProperty(n)) { m.SetTexture(n, t); return; }
    }
    private static void SetFloat(Material m, string[] names, float value)
    {
        foreach (string n in names) if (m.HasProperty(n)) { m.SetFloat(n, value); return; }
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static GameObject CreateModelInstance(GameObject model, string name, Scene preview)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
        if (go == null) throw new InvalidOperationException("Failed to instantiate model " + model.name);
        if (PrefabUtility.IsPartOfPrefabInstance(go))
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = name;
        return go;
    }

    private static void ApplyMaterial(GameObject go, Dictionary<string, Material> mats, string materialName)
    {
        if (!mats.TryGetValue(materialName, out Material mat) || mat == null) { Debug.LogWarning("Material not found: " + materialName); return; }
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var old = r.sharedMaterials;
            var next = new Material[old.Length == 0 ? 1 : old.Length];
            for (int i = 0; i < next.Length; i++) next[i] = mat;
            r.sharedMaterials = next;
        }
    }

    private static void AddStaticMeshColliders(GameObject go)
    {
        foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var col = filter.GetComponent<MeshCollider>();
            if (col == null) col = filter.gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = filter.sharedMesh;
            col.convex = false;
        }
    }

    private static void BuildRoadPrefabs(RoadEntry[] roads, Dictionary<string, Material> mats)
    {
        EnsureFolder(PrefabsRoot + "/RoadNetwork");
        EnsureFolder(PrefabsRoot + "/Segments");
        EnsureFolder(PrefabsRoot + "/Junctions");
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            // Individual road and junction prefabs.
            foreach (var e in roads ?? Array.Empty<RoadEntry>())
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ToAssetPath(e.modelPath));
                if (model == null) { Debug.LogWarning("Missing road model: " + e.modelPath); continue; }
                GameObject instance = CreateModelInstance(model, e.name, preview);
                ApplyMaterial(instance, mats, e.material);
                AddStaticMeshColliders(instance);
                string folder = e.kind == "junction" ? PrefabsRoot + "/Junctions" : PrefabsRoot + "/Segments";
                PrefabUtility.SaveAsPrefabAsset(instance, folder + "/" + e.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(instance);
            }
            // One network prefab made from the same model source meshes, all in world coordinates.
            GameObject root = new GameObject("TODA_RUSH_RoadNetwork");
            SceneManager.MoveGameObjectToScene(root, preview);
            foreach (var e in roads ?? Array.Empty<RoadEntry>())
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ToAssetPath(e.modelPath));
                if (model == null) continue;
                GameObject instance = CreateModelInstance(model, e.name, preview);
                instance.transform.SetParent(root.transform, false);
                ApplyMaterial(instance, mats, e.material);
                AddStaticMeshColliders(instance);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabsRoot + "/RoadNetwork/RoadNetwork_Main.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void BuildAccessoryPrefabs(AccessoryEntry[] accessories, Dictionary<string, Material> mats)
    {
        EnsureFolder(PrefabsRoot + "/Accessories");
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var e in accessories ?? Array.Empty<AccessoryEntry>())
            {
                string category = String.IsNullOrEmpty(e.category) ? "Shared" : e.category;
                EnsureFolder(PrefabsRoot + "/Accessories/" + category);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ToAssetPath(e.modelPath));
                if (model == null) { Debug.LogWarning("Missing accessory model: " + e.modelPath); continue; }
                GameObject instance = CreateModelInstance(model, e.name, preview);
                ApplyMaterial(instance, mats, e.material);
                AddStaticMeshColliders(instance);
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabsRoot + "/Accessories/" + category + "/" + e.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
}
#endif
