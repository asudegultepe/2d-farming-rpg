using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Builds the HUD from the sprites in Assets/Sprites/UI/HUD into the existing Essentials canvas
// and wires it to the game: clock and calendar (DayTimeController), gold (MoneyDisplay)
// and the toolbar (ItemToolbarPanel). Running it again replaces the previous HUD.
// Layout values are in design pixels (1280x720) and scaled to the canvas reference resolution.
public static class HudBuilder
{
    const string Folder = HudSpriteImporter.Folder;
    const string FontPath = "Assets/Fonts/PixelifySans/PixelifySans.ttf";
    const string FontAssetPath = "Assets/Fonts/PixelifySans/PixelifySans SDF.asset";
    const string RootName = "HUD";
    const float DesignHeight = 720f;

    static readonly Color TextMain = new Color32(0x5b, 0x3f, 0x4d, 255);
    static readonly Color TextSoft = new Color32(0x6b, 0x4b, 0x5b, 255);
    static readonly Color Divider = new Color32(0xc4, 0x9a, 0x6c, 255);
    static readonly Color BarFill = new Color32(0xa4, 0xc2, 0x63, 255);

    static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    static readonly Vector2 TopRight = new Vector2(1f, 1f);
    static readonly Vector2 BottomRight = new Vector2(1f, 0f);
    static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    static TMP_FontAsset font;
    static float scale = 1f;

    [MenuItem("Tools/Setup/HUD (open Essentials first)")]
    public static void Build()
    {
        DayTimeController timeController = Object.FindObjectOfType<DayTimeController>();
        InventoryManager inventoryManager = Object.FindObjectOfType<InventoryManager>();
        ToolbarController toolbarController = Object.FindObjectOfType<ToolbarController>();
        GameManager gameManager = Object.FindObjectOfType<GameManager>();

        if (timeController == null || inventoryManager == null || toolbarController == null || gameManager == null)
        {
            EditorUtility.DisplayDialog("HUD", "Open the Essentials scene first (it needs the Game Manager, Player and Canvas).", "OK");
            return;
        }

        SerializedObject inventoryManagerData = new SerializedObject(inventoryManager);
        Canvas canvas = FindCanvas(inventoryManagerData.FindProperty("toolbarPanel").objectReferenceValue as GameObject);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("HUD", "Couldn't find the UI canvas.", "OK");
            return;
        }

        PrepareSprites();
        font = FindOrCreateFont();

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scale = 1f;
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scale = scaler.referenceResolution.y / DesignHeight;
        }

        Undo.SetCurrentGroupName("Build HUD");
        int undoGroup = Undo.GetCurrentGroup();

        Transform previous = canvas.transform.Find(RootName);
        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous.gameObject);
            inventoryManagerData.Update();
        }

        // the old toolbar and clock stay in the scene, switched off, in case they're needed again
        HideOldElement(inventoryManagerData.FindProperty("toolbarPanel"));
        HideOldElement(inventoryManagerData.FindProperty("dayTime"));
        RemoveOldMoneyLabels();

        RectTransform root = Stretch(RootName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build HUD");
        // first child so menus, dialogue and the screen fade draw on top of it
        root.SetAsFirstSibling();

        // everything that InventoryManager hides while the inventory is open
        RectTransform info = Stretch("Info", root);
        BuildDatePanel(info, timeController);
        BuildMoneyPanel(info);
        BuildMenuButtons(info);

        RectTransform hotbar = BuildHotbar(root, gameManager.inventoryContainer, toolbarController);
        BuildVitalsPanel(root);

        inventoryManagerData.FindProperty("toolbarPanel").objectReferenceValue = hotbar.gameObject;
        inventoryManagerData.FindProperty("dayTime").objectReferenceValue = info.gameObject;
        inventoryManagerData.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("HUD built and connected. Save the Essentials scene to keep it.");
    }

    // ---------- Sections ----------

    static void BuildDatePanel(Transform parent, DayTimeController timeController)
    {
        RectTransform date = Panel("DatePanel", parent, TopRight, new Vector2(-24f, -24f), new Vector2(252f, 198f));
        Image seasonIcon = Img("SeasonIcon", date, "season_spring", 18f, 18f, 36f, 36f).GetComponent<Image>();
        TextMeshProUGUI dayText = Txt("DayText", date, "Mon 1", 66f, 18f, 168f, 36f, 30f, true, TextMain, TextAlignmentOptions.Left);
        TextMeshProUGUI seasonText = Txt("SeasonText", date, "Spring · Year 1", 18f, 60f, 216f, 21f, 18f, false, TextSoft, TextAlignmentOptions.Left);
        Solid("Divider", date, Divider, 18f, 90f, 216f, 3f);
        TextMeshProUGUI clockText = Txt("ClockText", date, "06:00", 18f, 102f, 216f, 39f, 39f, true, TextMain, TextAlignmentOptions.Left);
        RectTransform bar = Img("DayBar", date, "bar_frame", 18f, 147f, 216f, 27f, true);
        // laid out at full width; DayTimeController shrinks it as the day passes
        RectTransform fill = Solid("DayBarFill", bar, BarFill, 6f, 6f, 204f, 12f);

        SerializedObject data = new SerializedObject(timeController);
        data.FindProperty("text").objectReferenceValue = clockText;
        data.FindProperty("dayText").objectReferenceValue = dayText;
        data.FindProperty("seasonText").objectReferenceValue = seasonText;
        data.FindProperty("seasonIcon").objectReferenceValue = seasonIcon;
        data.FindProperty("dayProgressFill").objectReferenceValue = fill;

        string[] seasons = { "season_spring", "season_summer", "season_fall", "season_winter" };
        SerializedProperty icons = data.FindProperty("seasonIcons");
        icons.arraySize = seasons.Length;
        for (int i = 0; i < seasons.Length; i++)
        {
            icons.GetArrayElementAtIndex(i).objectReferenceValue = Load(seasons[i]);
        }
        data.ApplyModifiedProperties();
    }

    static void BuildMoneyPanel(Transform parent)
    {
        RectTransform money = Panel("MoneyPanel", parent, TopRight, new Vector2(-24f, -231f), new Vector2(252f, 78f));
        Img("Coin", money, "coin", 18f, 18f, 27f, 36f);
        TextMeshProUGUI text = Txt("MoneyText", money, "100", 57f, 18f, 177f, 36f, 30f, true, TextMain, TextAlignmentOptions.Right);

        MoneyDisplay display = text.gameObject.AddComponent<MoneyDisplay>();
        SerializedObject data = new SerializedObject(display);
        // the coin icon replaces the "g"
        data.FindProperty("suffix").stringValue = "";
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildMenuButtons(Transform parent)
    {
        string[] sprites = { "button_quests", "button_map", "button_settings" };
        string[] names = { "QuestsButton", "MapButton", "SettingsButton" };

        for (int i = 0; i < sprites.Length; i++)
        {
            RectTransform button = Rect(names[i], parent, TopRight, TopRight, new Vector2(-24f - (2 - i) * 75f, -318f), new Vector2(66f, 72f));
            Image image = button.gameObject.AddComponent<Image>();
            image.sprite = Load(sprites[i]);
            button.gameObject.AddComponent<Button>();
            // turned on once the matching menu exists
            button.gameObject.SetActive(false);
        }
    }

    static RectTransform BuildHotbar(Transform parent, ItemContainer inventory, ToolbarController toolbarController)
    {
        int slotCount = new SerializedObject(toolbarController).FindProperty("toolbarSize").intValue;
        const float slotSpacing = 72f;
        const float padding = 18f;
        float width = padding * 2f + slotCount * slotSpacing - 6f;

        RectTransform hotbar = Panel("Hotbar", parent, BottomCenter, new Vector2(0f, 24f), new Vector2(width, 108f));
        List<InventoryButton> buttons = new List<InventoryButton>();

        for (int i = 0; i < slotCount; i++)
        {
            Vector2 slotCenter = new Vector2(padding + i * slotSpacing + 33f, -(padding + 33f));
            buttons.Add(BuildSlot("Slot" + (i + 1), hotbar, slotCenter));
        }

        ItemToolbarPanel panel = hotbar.gameObject.AddComponent<ItemToolbarPanel>();
        SerializedObject data = new SerializedObject(panel);
        data.FindProperty("inventory").objectReferenceValue = inventory;
        data.FindProperty("toolbarController").objectReferenceValue = toolbarController;
        SerializedProperty buttonList = data.FindProperty("buttons");
        buttonList.arraySize = buttons.Count;
        for (int i = 0; i < buttons.Count; i++)
        {
            buttonList.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo();

        return hotbar;
    }

    // Slot frame with a selected overlay, the item icon and the stack count,
    // laid out the way InventoryButton expects
    static InventoryButton BuildSlot(string name, Transform hotbar, Vector2 center)
    {
        Sprite frame = Load("slot");
        Sprite selected = Load("slot_selected");

        RectTransform slot = Rect(name, hotbar, TopLeft, Center, center, NativeSize(frame));
        Image frameImage = slot.gameObject.AddComponent<Image>();
        frameImage.sprite = frame;

        RectTransform highlight = Rect("Highlight", slot, Center, Center, Vector2.zero, NativeSize(selected));
        Image highlightImage = highlight.gameObject.AddComponent<Image>();
        highlightImage.sprite = selected;
        highlightImage.raycastTarget = false;
        highlight.gameObject.SetActive(false);

        RectTransform icon = Rect("Icon", slot, Center, Center, Vector2.zero, new Vector2(39f, 39f));
        Image iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        icon.gameObject.SetActive(false);

        TextMeshProUGUI count = Txt("Count", slot, "1", 6f, 36f, 48f, 21f, 18f, true, TextMain, TextAlignmentOptions.BottomRight);
        count.gameObject.SetActive(false);

        InventoryButton button = slot.gameObject.AddComponent<InventoryButton>();
        button.icon = iconImage;
        button.highlight = highlightImage;
        button.text = count;
        return button;
    }

    // Health and energy, hidden until those systems exist
    static void BuildVitalsPanel(Transform parent)
    {
        RectTransform vitals = Panel("VitalsPanel", parent, BottomRight, new Vector2(-24f, 24f), new Vector2(183f, 162f));
        Txt("HealthLabel", vitals, "Health", 18f, 18f, 147f, 18f, 18f, false, TextSoft, TextAlignmentOptions.Left);
        for (int i = 0; i < 5; i++)
        {
            Img("Heart" + (i + 1), vitals, "heart_full", 18f + i * 30f, 42f, 27f, 27f);
        }
        Txt("EnergyLabel", vitals, "Energy", 18f, 78f, 147f, 18f, 18f, false, TextSoft, TextAlignmentOptions.Left);
        for (int i = 0; i < 10; i++)
        {
            Img("Energy" + (i + 1), vitals, "energy_full", 18f + i * 15f, 102f, 12f, 36f);
        }
        vitals.gameObject.SetActive(false);
    }

    // ---------- Scene helpers ----------

    // The canvas the current toolbar lives on, otherwise the first one in the scene
    static Canvas FindCanvas(GameObject toolbar)
    {
        if (toolbar != null)
        {
            for (Transform t = toolbar.transform; t != null; t = t.parent)
            {
                Canvas canvas = t.GetComponent<Canvas>();
                if (canvas != null && canvas.isRootCanvas) { return canvas; }
            }
        }
        return Object.FindObjectOfType<Canvas>();
    }

    static void HideOldElement(SerializedProperty property)
    {
        GameObject old = property.objectReferenceValue as GameObject;
        if (old == null || old.activeSelf == false) { return; }

        Undo.RecordObject(old, "Hide old HUD element");
        old.SetActive(false);
    }

    // The plain money label from EconomySetup, replaced by the money panel
    static void RemoveOldMoneyLabels()
    {
        foreach (MoneyDisplay display in Object.FindObjectsOfType<MoneyDisplay>(true))
        {
            Undo.DestroyObjectImmediate(display.gameObject);
        }
    }

    // ---------- Assets ----------

    // Sprites copied in before HudSpriteImporter compiled miss its settings, so reimport those
    static void PrepareSprites()
    {
        foreach (string file in Directory.GetFiles(Folder, "*.png"))
        {
            string path = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { continue; }

            if (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    static TMP_FontAsset FindOrCreateFont()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) { return existing; }

        Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (source == null)
        {
            Debug.LogWarning("HUD: " + FontPath + " not found, using the default TextMeshPro font.");
            return null;
        }

        // dynamic asset: glyphs are added to the atlas the first time they're used
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
        AssetDatabase.CreateAsset(asset, FontAssetPath);

        asset.atlasTextures[0].name = "PixelifySans Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = "PixelifySans Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        AssetDatabase.SaveAssets();

        return asset;
    }

    static Sprite Load(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
        if (sprite == null)
        {
            Debug.LogWarning("HUD: sprite not found: " + name);
        }
        return sprite;
    }

    // The sprites are drawn at 3x for a 1280x720 screen, so their pixel size is their design size
    static Vector2 NativeSize(Sprite sprite)
    {
        return sprite != null ? sprite.rect.size : new Vector2(60f, 63f);
    }

    // ---------- Layout helpers (all sizes in design pixels) ----------

    static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position * scale;
        rect.sizeDelta = size * scale;
        return rect;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // Box pinned to a corner of its parent: anchor = pivot = corner
    static RectTransform Panel(string name, Transform parent, Vector2 corner, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(name, parent, corner, corner, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = Load("panel");
        image.type = Image.Type.Sliced;
        // scale the 9-slice borders together with the layout
        image.pixelsPerUnitMultiplier = 1f / scale;
        return rect;
    }

    // x, y: pixels from the parent's top left corner
    static RectTransform Img(string name, Transform parent, string sprite, float x, float y, float w, float h, bool sliced = false)
    {
        RectTransform rect = Rect(name, parent, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = Load(sprite);
        image.raycastTarget = false;
        if (sliced)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / scale;
        }
        return rect;
    }

    static RectTransform Solid(string name, Transform parent, Color color, float x, float y, float w, float h)
    {
        RectTransform rect = Rect(name, parent, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, float x, float y, float w, float h,
        float size, bool bold, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = Rect(name, parent, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) { label.font = font; }
        label.text = text;
        label.fontSize = size * scale;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }
}
