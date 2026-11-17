using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

// Keeps Resources/SaveDatabase.asset in sync with every Item, Crop and CropsContainer
// in the project, so new assets are saveable without any manual setup.
[InitializeOnLoad]
public static class SaveDatabaseBuilder
{
    const string AssetPath = "Assets/Resources/" + SaveDatabase.ResourcePath + ".asset";

    static SaveDatabaseBuilder()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            Refresh();
        }
    }

    [MenuItem("Tools/Save System/Refresh Database")]
    public static void Refresh()
    {
        SaveDatabase database = AssetDatabase.LoadAssetAtPath<SaveDatabase>(AssetPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<SaveDatabase>();
            AssetDatabase.CreateAsset(database, AssetPath);
        }

        database.items = FindAll<Item>();
        database.crops = FindAll<Crop>();
        database.cropsContainers = FindAll<CropsContainer>();

        WarnDuplicateNames(database.items);
        WarnDuplicateNames(database.crops);
        WarnDuplicateNames(database.cropsContainers);

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Save System/Delete Save File")]
    public static void DeleteSave()
    {
        if (File.Exists(SaveManager.SavePath))
        {
            File.Delete(SaveManager.SavePath);
            Debug.Log("Save file deleted: " + SaveManager.SavePath);
        }
        else
        {
            Debug.Log("No save file found at " + SaveManager.SavePath);
        }
    }

    [MenuItem("Tools/Save System/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(SaveManager.SavePath);
    }

    static List<T> FindAll<T>() where T : Object
    {
        List<T> result = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null)
            {
                result.Add(asset);
            }
        }
        return result;
    }

    // Assets are saved by name, so two assets with the same name would be confused on load
    static void WarnDuplicateNames<T>(List<T> assets) where T : Object
    {
        HashSet<string> names = new HashSet<string>();
        foreach (T asset in assets)
        {
            if (names.Add(asset.name) == false)
            {
                Debug.LogWarning("Save system: more than one " + typeof(T).Name + " is named '" + asset.name + "'. Rename one so it saves correctly.");
            }
        }
    }
}
