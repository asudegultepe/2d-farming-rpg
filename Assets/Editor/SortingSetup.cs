using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Sets up draw order for the whole project:
//   Default  - ground tilemaps and flat props (carpets, bridges...), unchanged
//   Marker   - the tile selection marker, above the ground but under characters
//   World    - player, NPCs, trees, rocks, buildings, crops... all at order 0 and
//              sorted by where they stand (see Sort By Feet below and SortByFeet)
//   Overhead - the interaction highlight that floats above objects
// Renderers already on a layer other than Default are left alone, so hand-made
// changes survive running it again.
public static class SortingSetup
{
    public const string MarkerLayer = "Marker";
    public const string WorldLayer = "World";
    public const string OverheadLayer = "Overhead";

    static readonly string[] NewLayers = { MarkerLayer, WorldLayer, OverheadLayer };

    // Sprites the player walks on, kept under everything.
    // Matched against object names, so avoid words that appear in sprite sheet names
    // (Town objects are named after "building-shadowless").
    static readonly string[] FlatKeywords = { "carpet", "rug", "drain", "bridge" };

    // Tilemaps whose tiles are standing objects, sorted tile by tile
    static readonly string[] ObjectTilemapKeywords = { "tree", "stone", "rock" };

    const string PrefabFolder = "Assets/Prefabs";
    const string SceneFolder = "Assets/Scenes";

    static int markerId;
    static int worldId;
    static int overheadId;

    [MenuItem("Tools/Setup/Sorting Layers (Y-sort)")]
    public static void Run()
    {
        if (EditorUtility.DisplayDialog("Sorting Setup",
            "This adds the Marker, World and Overhead sorting layers and updates every prefab in " + PrefabFolder +
            " and every scene in " + SceneFolder + ", then saves them.\n\nCommit or back up first.", "Run", "Cancel") == false)
        {
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) { return; }

        CreateSortingLayers();

        StringBuilder report = new StringBuilder("Sorting setup done.\n");
        SceneSetup[] openScenes = EditorSceneManager.GetSceneManagerSetup();

        // prefabs first so scene instances already inherit the new values
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            if (Process(new[] { root }, report))
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.AppendLine("  saved " + path);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            if (Process(scene.GetRootGameObjects(), report))
            {
                EditorSceneManager.SaveScene(scene);
                report.AppendLine("  saved " + path);
            }
        }

        if (openScenes.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(openScenes);
        }

        Debug.Log(report.ToString());
    }

    // ---------- Layers ----------

    static void CreateSortingLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

        foreach (string layerName in NewLayers)
        {
            if (FindLayer(layers, layerName) != null) { continue; }

            // appended after Default, so each new layer draws above the ones before it
            layers.InsertArrayElementAtIndex(layers.arraySize);
            SerializedProperty layer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            layer.FindPropertyRelative("name").stringValue = layerName;
            layer.FindPropertyRelative("uniqueID").intValue = NewLayerId(layers);
            layer.FindPropertyRelative("locked").boolValue = false;
        }

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        markerId = FindLayer(layers, MarkerLayer).FindPropertyRelative("uniqueID").intValue;
        worldId = FindLayer(layers, WorldLayer).FindPropertyRelative("uniqueID").intValue;
        overheadId = FindLayer(layers, OverheadLayer).FindPropertyRelative("uniqueID").intValue;
    }

    static SerializedProperty FindLayer(SerializedProperty layers, string layerName)
    {
        for (int i = 0; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (layer.FindPropertyRelative("name").stringValue == layerName) { return layer; }
        }
        return null;
    }

    static int NewLayerId(SerializedProperty layers)
    {
        while (true)
        {
            int id = Random.Range(1, int.MaxValue);
            bool taken = false;
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue == id) { taken = true; }
            }
            if (taken == false) { return id; }
        }
    }

    // ---------- Objects ----------

    static bool Process(GameObject[] roots, StringBuilder report)
    {
        bool changed = false;

        HashSet<GameObject> markers = new HashSet<GameObject>();
        HashSet<GameObject> overhead = new HashSet<GameObject>();
        foreach (GameObject root in roots)
        {
            foreach (MarkerManager marker in root.GetComponentsInChildren<MarkerManager>(true))
            {
                AddReference(markers, marker, "targetTilemap");
            }
            foreach (HighlightManager highlight in root.GetComponentsInChildren<HighlightManager>(true))
            {
                AddReference(overhead, highlight, "highlighter");
            }
        }

        foreach (GameObject root in roots)
        {
            foreach (Light2D light in root.GetComponentsInChildren<Light2D>(true))
            {
                changed |= LightNewLayers(light);
            }

            foreach (TilemapRenderer tilemap in root.GetComponentsInChildren<TilemapRenderer>(true))
            {
                changed |= SortTilemap(tilemap, markers.Contains(tilemap.gameObject));
            }

            foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                changed |= SortSprite(sprite, overhead.Contains(sprite.gameObject), report);
            }
        }

        return changed;
    }

    static void AddReference(HashSet<GameObject> set, Object owner, string property)
    {
        Object value = new SerializedObject(owner).FindProperty(property).objectReferenceValue;
        if (value is GameObject go) { set.Add(go); }
        else if (value is Component component) { set.Add(component.gameObject); }
    }

    // 2D lights only light the sorting layers they target, so add the new ones
    // to every light that lights Default
    static bool LightNewLayers(Light2D light)
    {
        SerializedObject data = new SerializedObject(light);
        SerializedProperty targets = data.FindProperty("m_ApplyToSortingLayers");
        if (targets == null || Contains(targets, 0) == false) { return false; }

        bool changed = false;
        foreach (int id in new[] { markerId, worldId, overheadId })
        {
            if (Contains(targets, id)) { continue; }

            targets.InsertArrayElementAtIndex(targets.arraySize);
            targets.GetArrayElementAtIndex(targets.arraySize - 1).intValue = id;
            changed = true;
        }

        data.ApplyModifiedPropertiesWithoutUndo();
        return changed;
    }

    static bool Contains(SerializedProperty array, int value)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            if (array.GetArrayElementAtIndex(i).intValue == value) { return true; }
        }
        return false;
    }

    static bool SortTilemap(TilemapRenderer tilemap, bool isMarker)
    {
        if (tilemap.sortingLayerID != 0) { return false; }

        if (isMarker)
        {
            SetLayer(tilemap, markerId, 0);
            return true;
        }

        if (NameContains(tilemap.gameObject, ObjectTilemapKeywords))
        {
            // Individual mode sorts each tile by its own position instead of the whole map at once
            tilemap.mode = TilemapRenderer.Mode.Individual;
            SetLayer(tilemap, worldId, 0);
            return true;
        }

        return false;
    }

    static bool SortSprite(SpriteRenderer sprite, bool isOverhead, StringBuilder report)
    {
        if (sprite.sortingLayerID != 0) { return false; }

        if (isOverhead)
        {
            SetLayer(sprite, overheadId, sprite.sortingOrder);
            return true;
        }

        if (NameContains(sprite.gameObject, FlatKeywords))
        {
            report.AppendLine("  kept on Default (flat): " + sprite.name);
            return false;
        }

        // part of a bigger sprite object, keeps its order inside that object's sorting group
        if (sprite.transform.parent != null && sprite.transform.parent.GetComponentInParent<SpriteRenderer>(true) != null)
        {
            SetLayer(sprite, worldId, sprite.sortingOrder);
            return true;
        }

        // an object made of several sprites sorts as one piece, at its own position
        if (sprite.GetComponentsInChildren<SpriteRenderer>(true).Length > 1)
        {
            SortingGroup group = sprite.GetComponent<SortingGroup>();
            if (group == null)
            {
                group = sprite.gameObject.AddComponent<SortingGroup>();
            }
            group.sortingLayerID = worldId;
            group.sortingOrder = 0;
            Record(group);

            SetLayer(sprite, worldId, sprite.sortingOrder);
            return true;
        }

        SetLayer(sprite, worldId, 0);
        return true;
    }

    static void SetLayer(Renderer renderer, int layerId, int order)
    {
        renderer.sortingLayerID = layerId;
        renderer.sortingOrder = order;
        Record(renderer);
    }

    // so changes on prefab instances in scenes are stored as overrides
    static void Record(Component component)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(component))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorUtility.SetDirty(component);
    }

    static bool NameContains(GameObject go, string[] keywords)
    {
        string name = go.name.ToLowerInvariant();
        foreach (string keyword in keywords)
        {
            if (name.Contains(keyword)) { return true; }
        }
        return false;
    }

    // ---------- Sort by feet ----------

    // Turns on sorting along (0, 1, 1) in the 2D renderer and sets every World object's z
    // so it sorts by its bottom edge (see SortByFeet). Safe to run again.
    [MenuItem("Tools/Setup/Sort By Feet")]
    public static void SortByFeetSetup()
    {
        int world = SortingLayer.NameToID(WorldLayer);
        if (world == 0)
        {
            EditorUtility.DisplayDialog("Sort By Feet", "Run Tools/Setup/Sorting Layers first.", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog("Sort By Feet",
            "This changes the 2D renderer's sort axis, adds SortByFeet to the prefabs in " + PrefabFolder +
            " and sets the z position of objects in every scene in " + SceneFolder + ", then saves them.", "Run", "Cancel") == false)
        {
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) { return; }

        SetSortAxis();
        ClearSheets();

        StringBuilder report = new StringBuilder("Sort by feet done.\n");
        SceneSetup[] openScenes = EditorSceneManager.GetSceneManagerSetup();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            // prefabs get spawned, moved or change sprite at runtime, so they update themselves;
            // textures aren't readable in a build, so the grass skirt is measured here
            if (HasWorldSprite(root, world))
            {
                SortByFeet sortByFeet = root.GetComponent<SortByFeet>();
                bool added = sortByFeet == null;
                if (added)
                {
                    sortByFeet = root.AddComponent<SortByFeet>();
                }

                SpriteRenderer lowest = SortByFeet.LowestSprite(root.GetComponentsInChildren<SpriteRenderer>(true));
                float skirt = lowest != null ? MeasureGroundSkirt(lowest.sprite) : 0f;

                SerializedObject data = new SerializedObject(sortByFeet);
                SerializedProperty skirtProperty = data.FindProperty("groundSkirt");
                bool skirtChanged = Mathf.Approximately(skirtProperty.floatValue, skirt) == false;
                skirtProperty.floatValue = skirt;
                data.ApplyModifiedPropertiesWithoutUndo();

                if (added || skirtChanged)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    report.AppendLine("  " + path + ": ground skirt " + skirt.ToString("0.00"));
                }
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            int baked = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                baked += BakeFeetZ(root, world);
                baked += PullBackCameras(root);
            }

            if (baked > 0)
            {
                EditorSceneManager.SaveScene(scene);
                report.AppendLine("  " + path + ": " + baked + " objects");
            }
        }

        if (openScenes.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(openScenes);
        }

        ClearSheets();
        Debug.Log(report.ToString());
    }

    static void SetSortAxis()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Renderer2DData"))
        {
            Object rendererData = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            SerializedObject data = new SerializedObject(rendererData);
            data.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            data.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0f, 1f, 1f);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
        }
        AssetDatabase.SaveAssets();
    }

    static bool HasWorldSprite(GameObject root, int world)
    {
        foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite.sortingLayerID == world) { return true; }
        }
        return false;
    }

    // Static objects get a fixed z; anything with SortByFeet (player, prefab instances) does it at runtime
    static int BakeFeetZ(GameObject root, int world)
    {
        int baked = 0;

        foreach (Player player in root.GetComponentsInChildren<Player>(true))
        {
            if (player.GetComponent<SortByFeet>() == null)
            {
                player.gameObject.AddComponent<SortByFeet>();
                baked++;
            }
        }

        foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite.sortingLayerID != world || sprite.GetComponentInParent<SortByFeet>(true) != null) { continue; }

            // an object made of several sprites is moved as a whole, from its sorting group
            SortingGroup group = sprite.GetComponentInParent<SortingGroup>(true);
            Transform target = group != null ? group.transform : sprite.transform;
            if (group != null && sprite.transform != target && target.GetComponent<SpriteRenderer>() != null) { continue; }

            SpriteRenderer[] targetSprites = target.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer lowest = SortByFeet.LowestSprite(targetSprites);
            float skirt = lowest != null ? MeasureGroundSkirt(lowest.sprite) * Mathf.Abs(lowest.transform.lossyScale.y) : 0f;

            float z = SortByFeet.FeetZ(target, group, targetSprites, skirt);
            if (SetZ(target, z)) { baked++; }
        }

        // tiles sort by their cell center, move them down to the cell's bottom edge
        foreach (TilemapRenderer tilemap in root.GetComponentsInChildren<TilemapRenderer>(true))
        {
            if (tilemap.sortingLayerID != world) { continue; }

            Grid grid = tilemap.GetComponentInParent<Grid>();
            float halfCell = grid != null ? grid.cellSize.y * tilemap.transform.lossyScale.y / 2f : 0.5f;
            if (SetZ(tilemap.transform, -halfCell)) { baked++; }
        }

        return baked;
    }

    // ---------- Ground skirt ----------

    static readonly Dictionary<string, Texture2D> sheets = new Dictionary<string, Texture2D>();

    // Grass: clearly greener than it is red or blue
    static bool IsGrass(Color32 color)
    {
        return color.g > color.r + 12 && color.g > color.b + 12;
    }

    // Height (in units at scale 1) of the rows at the bottom of a sprite that hold only grass.
    // The trees are drawn standing on a patch of grass that reaches below their trunk; the
    // trunk is where they meet the ground. A sprite that is all grass (a bush) gets 0.
    static float MeasureGroundSkirt(Sprite sprite)
    {
        if (sprite == null) { return 0f; }

        Texture2D sheet = LoadSheet(AssetDatabase.GetAssetPath(sprite.texture));
        if (sheet == null) { return 0f; }

        // sprite.rect is in the imported texture's pixels, which may be smaller than the file
        float scaleX = (float)sheet.width / sprite.texture.width;
        float scaleY = (float)sheet.height / sprite.texture.height;
        Rect rect = sprite.rect;
        Color32[] pixels = sheet.GetPixels32();

        for (int row = 0; row < (int)rect.height; row++)
        {
            int y = (int)((rect.y + row) * scaleY);
            for (int column = 0; column < (int)rect.width; column++)
            {
                int x = (int)((rect.x + column) * scaleX);
                Color32 color = pixels[y * sheet.width + x];
                if (color.a > 200 && IsGrass(color) == false)
                {
                    return row / sprite.pixelsPerUnit;
                }
            }
        }

        return 0f;
    }

    static void ClearSheets()
    {
        foreach (Texture2D sheet in sheets.Values)
        {
            if (sheet != null) { Object.DestroyImmediate(sheet); }
        }
        sheets.Clear();
    }

    // The imported texture isn't readable, so read the source image instead
    static Texture2D LoadSheet(string path)
    {
        if (sheets.TryGetValue(path, out Texture2D sheet)) { return sheet; }

        sheet = null;
        if (string.IsNullOrEmpty(path) == false && System.IO.File.Exists(path))
        {
            sheet = new Texture2D(2, 2);
            if (sheet.LoadImage(System.IO.File.ReadAllBytes(path)) == false)
            {
                Object.DestroyImmediate(sheet);
                sheet = null;
            }
        }

        sheets[path] = sheet;
        return sheet;
    }

    // Tall sprites end up several units behind z = 0. The camera is orthographic, so its
    // distance doesn't change the picture, but it has to stay in front of all of them.
    const float MinCameraDistance = 20f;

    static int PullBackCameras(GameObject root)
    {
        int changed = 0;

        foreach (CinemachineVirtualCamera virtualCamera in root.GetComponentsInChildren<CinemachineVirtualCamera>(true))
        {
            CinemachineFramingTransposer transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer == null || transposer.m_CameraDistance >= MinCameraDistance) { continue; }

            transposer.m_CameraDistance = MinCameraDistance;
            EditorUtility.SetDirty(transposer);
            changed++;
        }

        return changed;
    }

    static bool SetZ(Transform target, float z)
    {
        Vector3 position = target.position;
        if (Mathf.Approximately(position.z, z)) { return false; }

        position.z = z;
        target.position = position;
        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        EditorUtility.SetDirty(target);
        return true;
    }
}
