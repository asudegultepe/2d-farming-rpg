using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BedSetup
{
    // Interactables are found with a physics overlap, so the bed needs a collider.
    // A BoxCollider2D added to a sprite sizes itself to the sprite.
    [MenuItem("Tools/Setup/Make Selected Object a Bed")]
    public static void MakeBed()
    {
        GameObject bed = Selection.activeGameObject;
        if (bed == null || bed.GetComponent<SpriteRenderer>() == null)
        {
            EditorUtility.DisplayDialog("Bed", "Select the bed sprite in the HouseInterior scene first.", "OK");
            return;
        }

        if (bed.GetComponent<Collider2D>() == null)
        {
            Undo.AddComponent<BoxCollider2D>(bed);
        }

        if (bed.GetComponent<Bed>() == null)
        {
            Undo.AddComponent<Bed>(bed);
        }

        EditorSceneManager.MarkSceneDirty(bed.scene);
        Debug.Log(bed.name + " is now a bed. Right click it to sleep.");
    }
}
