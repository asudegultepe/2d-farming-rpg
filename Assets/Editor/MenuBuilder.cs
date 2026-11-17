using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UIBuild;

// Builds the pause menu into the Essentials canvas and the MainMenu scene,
// both in the HUD style, and puts MainMenu + Essentials at the top of the build list.
public static class MenuBuilder
{
    public const string HudSettingsButton = "SettingsButton";
    const string PauseRootName = "PauseMenu";
    const string MainMenuPath = "Assets/Scenes/" + PauseMenu.MainMenuScene + ".unity";
    const string EssentialsPath = "Assets/Scenes/" + MainMenu.GameScene + ".unity";
    const string GameTitle = "Farming RPG";

    static readonly Color MenuBackground = new Color32(0x7f, 0xa8, 0x5c, 255);

    // ---------- Pause menu ----------

    [MenuItem("Tools/Setup/Pause Menu (open Essentials first)")]
    public static void BuildPauseMenu()
    {
        InventoryManager inventoryManager = Object.FindObjectOfType<InventoryManager>();
        if (inventoryManager == null || Object.FindObjectOfType<GameManager>() == null)
        {
            EditorUtility.DisplayDialog("Pause Menu", "Open the Essentials scene first.", "OK");
            return;
        }

        GameObject toolbar = new SerializedObject(inventoryManager).FindProperty("toolbarPanel").objectReferenceValue as GameObject;
        Canvas canvas = HudBuilder.FindCanvas(toolbar);
        Begin(canvas);

        Undo.SetCurrentGroupName("Build Pause Menu");
        int undoGroup = Undo.GetCurrentGroup();

        Transform previous = canvas.transform.Find(PauseRootName);
        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous.gameObject);
        }

        // the root stays active so PauseMenu can listen for Escape; the overlay is what shows
        RectTransform root = Stretch(PauseRootName, canvas.transform);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Pause Menu");
        root.SetAsLastSibling();
        PauseMenu pauseMenu = root.gameObject.AddComponent<PauseMenu>();

        RectTransform overlay = Overlay("Overlay", root);

        RectTransform window = Panel("Window", overlay, Center, Vector2.zero, new Vector2(300f, 405f));
        Txt("Title", window, "Paused", 0f, 24f, 300f, 36f, 32f, true, TextMain, TextAlignmentOptions.Center);
        TextButton("ResumeButton", window, "Resume", 81f, 240f, pauseMenu.Resume);
        TextButton("SettingsButton", window, "Settings", 144f, 240f, pauseMenu.OpenSettings);
        TextButton("SaveButton", window, "Save Game", 207f, 240f, pauseMenu.SaveGame);
        TextButton("MainMenuButton", window, "Main Menu", 270f, 240f, pauseMenu.ExitToMainMenu);
        Button quit = TextButton("QuitButton", window, "Quit", 333f, 240f, pauseMenu.Quit);

        SettingsPanel settings = BuildSettingsPanel(overlay);
        overlay.gameObject.SetActive(false);

        SerializedObject data = new SerializedObject(pauseMenu);
        data.FindProperty("overlay").objectReferenceValue = overlay.gameObject;
        data.FindProperty("window").objectReferenceValue = window.gameObject;
        data.FindProperty("settings").objectReferenceValue = settings;
        data.FindProperty("quitButton").objectReferenceValue = quit.gameObject;
        data.ApplyModifiedPropertiesWithoutUndo();

        ConnectHudSettingsButton(canvas);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Pause menu built. Save the Essentials scene to keep it.");
    }

    // Shows the HUD's settings button and points it at the pause menu, once both exist
    public static void ConnectHudSettingsButton(Canvas canvas)
    {
        Transform hud = canvas.transform.Find(HudBuilder.RootName);
        PauseMenu pauseMenu = canvas.GetComponentInChildren<PauseMenu>(true);
        if (hud == null || pauseMenu == null) { return; }

        Transform buttonTransform = FindDeep(hud, HudSettingsButton);
        if (buttonTransform == null) { return; }

        Button button = buttonTransform.GetComponent<Button>();
        Undo.RecordObject(button, "Connect settings button");
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }
        UnityEventTools.AddPersistentListener(button.onClick, pauseMenu.OpenSettings);

        Undo.RecordObject(button.gameObject, "Show settings button");
        button.gameObject.SetActive(true);
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) { return child; }

            Transform found = FindDeep(child, name);
            if (found != null) { return found; }
        }
        return null;
    }

    // ---------- Settings ----------

    static SettingsPanel BuildSettingsPanel(Transform parent)
    {
        RectTransform window = Panel("Settings", parent, Center, Vector2.zero, new Vector2(420f, 300f));
        Txt("Title", window, "Settings", 0f, 24f, 420f, 36f, 32f, true, TextMain, TextAlignmentOptions.Center);

        Slider master = SliderRow("MasterVolume", window, "Volume", 84f);
        Slider music = SliderRow("MusicVolume", window, "Music", 129f);
        Toggle fullscreen = ToggleRow("Fullscreen", window, "Fullscreen", 174f);

        SettingsPanel settings = window.gameObject.AddComponent<SettingsPanel>();
        TextButton("BackButton", window, "Back", 228f, 200f, settings.Close);

        SerializedObject data = new SerializedObject(settings);
        data.FindProperty("masterVolume").objectReferenceValue = master;
        data.FindProperty("musicVolume").objectReferenceValue = music;
        data.FindProperty("fullscreen").objectReferenceValue = fullscreen;
        data.ApplyModifiedPropertiesWithoutUndo();

        window.gameObject.SetActive(false);
        return settings;
    }

    // ---------- Main menu scene ----------

    [MenuItem("Tools/Setup/Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath) != null &&
            EditorUtility.DisplayDialog("Main Menu", MainMenuPath + " already exists. Rebuild it?", "Rebuild", "Cancel") == false)
        {
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) { return; }
        SceneSetup[] openScenes = EditorSceneManager.GetSceneManagerSetup();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = MenuBackground;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // same scaling as the Essentials canvas
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Begin(canvas);

        RectTransform root = Stretch("MainMenu", canvas.transform);
        MainMenu menu = root.gameObject.AddComponent<MainMenu>();

        RectTransform title = Rect("Title", root, TopCenter, TopCenter, new Vector2(0f, -90f), new Vector2(900f, 100f));
        Label(title, GameTitle, 72f, true, TextMain, TextAlignmentOptions.Center);

        RectTransform window = Panel("Window", root, Center, new Vector2(0f, -40f), new Vector2(300f, 300f));
        Button continueButton = TextButton("ContinueButton", window, "Continue", 24f, 240f, menu.Continue);
        TextButton("NewGameButton", window, "New Game", 87f, 240f, menu.NewGame);
        TextButton("SettingsButton", window, "Settings", 150f, 240f, menu.OpenSettings);
        Button quit = TextButton("QuitButton", window, "Quit", 213f, 240f, menu.Quit);

        RectTransform confirm = Panel("ConfirmNewGame", root, Center, new Vector2(0f, -40f), new Vector2(480f, 240f));
        Txt("Message", confirm, "Start a new game?\nYour saved game will be overwritten.", 0f, 30f, 480f, 60f, 22f, false, TextMain, TextAlignmentOptions.Center);
        TextButton("StartButton", confirm, "Start New Game", 111f, 280f, menu.StartNewGame);
        TextButton("CancelButton", confirm, "Cancel", 171f, 280f, menu.CancelNewGame);
        confirm.gameObject.SetActive(false);

        SettingsPanel settings = BuildSettingsPanel(root);

        SerializedObject data = new SerializedObject(menu);
        data.FindProperty("window").objectReferenceValue = window.gameObject;
        data.FindProperty("continueButton").objectReferenceValue = continueButton;
        data.FindProperty("quitButton").objectReferenceValue = quit.gameObject;
        data.FindProperty("confirmNewGame").objectReferenceValue = confirm.gameObject;
        data.FindProperty("settings").objectReferenceValue = settings;
        data.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, MainMenuPath);
        UpdateBuildSettings();

        if (openScenes.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(openScenes);
        }

        Debug.Log("Main menu scene saved to " + MainMenuPath + " and added to the build settings.");
    }

    // MainMenu opens first, then Essentials; the gameplay scenes keep their order
    static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(MainMenuPath, true));
        scenes.Add(new EditorBuildSettingsScene(EssentialsPath, true));

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path == MainMenuPath || scene.path == EssentialsPath) { continue; }
            scenes.Add(scene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
