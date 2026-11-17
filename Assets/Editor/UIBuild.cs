using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Shared building blocks for the editor tools that create UI (HUD, menus).
// Positions and sizes are in design pixels (1280x720); Begin() sets the scale
// from the target canvas so the same numbers work at any reference resolution.
public static class UIBuild
{
    public const string SpriteFolder = HudSpriteImporter.Folder;
    const string FontPath = "Assets/Fonts/PixelifySans/PixelifySans.ttf";
    const string FontAssetPath = "Assets/Fonts/PixelifySans/PixelifySans SDF.asset";
    const float DesignHeight = 720f;

    public static readonly Color TextMain = new Color32(0x5b, 0x3f, 0x4d, 255);
    public static readonly Color TextSoft = new Color32(0x6b, 0x4b, 0x5b, 255);
    public static readonly Color Divider = new Color32(0xc4, 0x9a, 0x6c, 255);
    public static readonly Color BarFill = new Color32(0xa4, 0xc2, 0x63, 255);
    public static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);

    public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    public static readonly Vector2 TopRight = new Vector2(1f, 1f);
    public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
    public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    public static float Scale { get; private set; } = 1f;
    public static TMP_FontAsset Font { get; private set; }

    public static void Begin(Canvas canvas)
    {
        Font = FindOrCreateFont();

        Scale = 1f;
        CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            Scale = scaler.referenceResolution.y / DesignHeight;
        }
    }

    // ---------- Assets ----------

    public static Sprite Load(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/" + name + ".png");
        if (sprite == null)
        {
            Debug.LogWarning("UI: sprite not found: " + name);
        }
        return sprite;
    }

    // The sprites are drawn at 3x for a 1280x720 screen, so their pixel size is their design size
    public static Vector2 NativeSize(Sprite sprite, Vector2 fallback)
    {
        return sprite != null ? sprite.rect.size : fallback;
    }

    static TMP_FontAsset FindOrCreateFont()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) { return existing; }

        UnityEngine.Font source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(FontPath);
        if (source == null)
        {
            Debug.LogWarning("UI: " + FontPath + " not found, using the default TextMeshPro font.");
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

    // ---------- Layout ----------

    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position * Scale;
        rect.sizeDelta = size * Scale;
        return rect;
    }

    public static RectTransform Stretch(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // Full screen dark backdrop that also blocks clicks to the world behind it
    public static RectTransform Overlay(string name, Transform parent)
    {
        RectTransform rect = Stretch(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Dim;
        return rect;
    }

    // Box pinned to a corner of its parent: anchor = pivot = corner
    public static RectTransform Panel(string name, Transform parent, Vector2 corner, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(name, parent, corner, corner, position, size);
        Sliced(rect.gameObject.AddComponent<Image>(), "panel");
        return rect;
    }

    // x, y: pixels from the parent's top left corner
    public static RectTransform Img(string name, Transform parent, string sprite, float x, float y, float w, float h, bool sliced = false)
    {
        RectTransform rect = Rect(name, parent, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        if (sliced)
        {
            Sliced(image, sprite);
        }
        else
        {
            image.sprite = Load(sprite);
        }
        return rect;
    }

    public static RectTransform Solid(string name, Transform parent, Color color, float x, float y, float w, float h)
    {
        RectTransform rect = Rect(name, parent, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    public static TextMeshProUGUI Txt(string name, Transform parent, string text, float x, float y, float w, float h,
        float size, bool bold, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = Rect(name, parent, TopLeft, TopLeft, new Vector2(x, -y), new Vector2(w, h));
        return Label(rect, text, size, bold, color, alignment);
    }

    // Text that fills its RectTransform
    public static TextMeshProUGUI Label(RectTransform rect, string text, float size, bool bold, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) { label.font = Font; }
        label.text = text;
        label.fontSize = size * Scale;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    // ---------- Controls ----------

    // Panel-styled button stacked in a column: y is pixels from the parent's top edge
    public static Button TextButton(string name, Transform parent, string text, float y, float width, UnityAction onClick)
    {
        RectTransform rect = Rect(name, parent, TopCenter, TopCenter, new Vector2(0f, -y), new Vector2(width, 51f));
        Image image = rect.gameObject.AddComponent<Image>();
        Sliced(image, "panel");

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.93f, 0.8f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.78f, 0.7f);
        colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
        button.colors = colors;

        RectTransform labelRect = Stretch("Label", rect);
        Label(labelRect, text, 24f, true, TextMain, TextAlignmentOptions.Center);

        if (onClick != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        }
        return button;
    }

    // Label on the left, 0-1 slider on the right
    public static Slider SliderRow(string name, Transform parent, string text, float y)
    {
        RectTransform row = Rect(name, parent, TopLeft, TopLeft, new Vector2(30f, -y), new Vector2(360f, 30f));
        Label(Rect("Label", row, TopLeft, TopLeft, Vector2.zero, new Vector2(150f, 30f)), text, 20f, false, TextSoft, TextAlignmentOptions.Left);

        RectTransform sliderRect = Rect("Slider", row, TopLeft, TopLeft, new Vector2(150f, -2f), new Vector2(210f, 27f));
        Slider slider = sliderRect.gameObject.AddComponent<Slider>();

        RectTransform background = Stretch("Background", sliderRect);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        Sliced(backgroundImage, "bar_frame");

        RectTransform fillArea = Stretch("Fill Area", sliderRect);
        fillArea.offsetMin = new Vector2(6f, 6f) * Scale;
        fillArea.offsetMax = new Vector2(-6f, -9f) * Scale;
        RectTransform fill = Stretch("Fill", fillArea);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = BarFill;
        fillImage.raycastTarget = false;

        RectTransform handleArea = Stretch("Handle Slide Area", sliderRect);
        handleArea.offsetMin = new Vector2(6f, 0f) * Scale;
        handleArea.offsetMax = new Vector2(-6f, 0f) * Scale;
        RectTransform handle = Stretch("Handle", handleArea);
        handle.sizeDelta = new Vector2(12f * Scale, 0f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = TextMain;

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    // Label on the left, check box on the right
    public static Toggle ToggleRow(string name, Transform parent, string text, float y)
    {
        RectTransform row = Rect(name, parent, TopLeft, TopLeft, new Vector2(30f, -y), new Vector2(360f, 30f));
        Label(Rect("Label", row, TopLeft, TopLeft, Vector2.zero, new Vector2(150f, 30f)), text, 20f, false, TextSoft, TextAlignmentOptions.Left);

        RectTransform box = Rect("Box", row, TopLeft, TopLeft, new Vector2(150f, 0f), new Vector2(30f, 30f));
        Image boxImage = box.gameObject.AddComponent<Image>();
        boxImage.sprite = Load("slot");

        RectTransform check = Rect("Checkmark", box, Center, Center, Vector2.zero, new Vector2(15f, 15f));
        Image checkImage = check.gameObject.AddComponent<Image>();
        checkImage.color = BarFill;
        checkImage.raycastTarget = false;

        Toggle toggle = row.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = true;
        return toggle;
    }

    static void Sliced(Image image, string sprite)
    {
        image.sprite = Load(sprite);
        image.type = Image.Type.Sliced;
        // scale the 9-slice borders together with the layout
        image.pixelsPerUnitMultiplier = 1f / Scale;
    }
}
