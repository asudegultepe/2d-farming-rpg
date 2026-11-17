using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click scene setup for the economy: notification label, shop panel,
// shipping bin and shopkeeper. Every step supports Undo.
public static class EconomySetup
{
    const string ChestSpritePath = "Assets/Sprites/chest2.png";

    [MenuItem("Tools/Setup/Economy UI (open Essentials first)")]
    public static void CreateEconomyUI()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameManager gameManager = Object.FindObjectOfType<GameManager>();
        RecipePanel recipePanel = Object.FindObjectOfType<RecipePanel>(true);

        if (canvas == null || gameManager == null || recipePanel == null)
        {
            EditorUtility.DisplayDialog("Economy Setup", "Open the Essentials scene first (it needs the Canvas, Game Manager and Crafting Panel).", "OK");
            return;
        }

        // Find returns the first canvas, which may not be the UI one, so prefer the crafting panel's
        for (Transform t = recipePanel.transform; t != null; t = t.parent)
        {
            Canvas parentCanvas = t.GetComponent<Canvas>();
            if (parentCanvas != null && parentCanvas.isRootCanvas)
            {
                canvas = parentCanvas;
            }
        }

        // gold is shown by the HUD (Tools/Setup/HUD)
        if (Object.FindObjectOfType<Notification>(true) == null)
        {
            TextMeshProUGUI label = CreateLabel("Notification", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 230f), TextAlignmentOptions.Bottom);
            label.rectTransform.sizeDelta = new Vector2(800f, 60f);
            Undo.AddComponent<CanvasGroup>(label.gameObject);
            Undo.AddComponent<Notification>(label.gameObject);
        }

        if (gameManager.shopPanel == null)
        {
            // the shop reuses the crafting panel's look and buttons
            GameObject shop = Object.Instantiate(recipePanel.gameObject, canvas.transform);
            shop.name = "ShopPanel";
            Undo.RegisterCreatedObjectUndo(shop, "Create Shop Panel");

            RecipePanel copiedRecipePanel = shop.GetComponent<RecipePanel>();
            List<InventoryButton> buttons = new List<InventoryButton>(copiedRecipePanel.buttons);
            Object.DestroyImmediate(copiedRecipePanel);

            ShopPanel shopPanel = shop.AddComponent<ShopPanel>();
            shopPanel.buttons = buttons;

            RectTransform rect = shop.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            shop.SetActive(false);

            Undo.RecordObject(gameManager, "Assign Shop Panel");
            gameManager.shopPanel = shopPanel;
        }

        EditorSceneManager.MarkSceneDirty(gameManager.gameObject.scene);
        Debug.Log("Economy UI ready. Adjust the Money and Notification labels in the Canvas if needed, then save the scene.");
    }

    [MenuItem("Tools/Setup/Shipping Bin (at scene view center)")]
    public static void CreateShippingBin()
    {
        GameObject bin = new GameObject("ShippingBin");
        Undo.RegisterCreatedObjectUndo(bin, "Create Shipping Bin");

        if (SceneView.lastActiveSceneView != null)
        {
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            bin.transform.position = new Vector3(Mathf.Round(pivot.x) + 0.5f, Mathf.Round(pivot.y) + 0.5f, 0f);
        }

        SpriteRenderer renderer = bin.AddComponent<SpriteRenderer>();
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(ChestSpritePath);
        if (sprites.Length > 0)
        {
            renderer.sprite = sprites[0] as Sprite;
        }

        bin.AddComponent<BoxCollider2D>();
        bin.AddComponent<ShippingBin>();

        Selection.activeGameObject = bin;
        EditorSceneManager.MarkSceneDirty(bin.scene);
    }

    [MenuItem("Tools/Setup/Make Selected NPC a Shopkeeper")]
    public static void MakeShopkeeper()
    {
        GameObject npc = Selection.activeGameObject;
        if (npc == null)
        {
            EditorUtility.DisplayDialog("Economy Setup", "Select an NPC in the scene first.", "OK");
            return;
        }

        // InteractableManager only uses the first Interactable it finds
        foreach (Interactable other in npc.GetComponents<Interactable>())
        {
            if (other is ShopInteract == false)
            {
                Undo.DestroyObjectImmediate(other);
            }
        }

        ShopInteract shop = npc.GetComponent<ShopInteract>();
        if (shop == null)
        {
            shop = Undo.AddComponent<ShopInteract>(npc);
        }

        // stock every item that has a buy price
        SerializedObject serialized = new SerializedObject(shop);
        SerializedProperty stock = serialized.FindProperty("stock");
        stock.ClearArray();
        foreach (string guid in AssetDatabase.FindAssets("t:Item"))
        {
            Item item = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || item.buyPrice <= 0) { continue; }

            stock.InsertArrayElementAtIndex(stock.arraySize);
            stock.GetArrayElementAtIndex(stock.arraySize - 1).objectReferenceValue = item;
        }
        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(npc.scene);
        Debug.Log(npc.name + " is now a shopkeeper with " + stock.arraySize + " items.");
    }

    static TextMeshProUGUI CreateLabel(string name, Transform parent, Vector2 anchor, Vector2 position, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300f, 60f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 36;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
