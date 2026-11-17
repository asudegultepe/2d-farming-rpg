using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Escape pauses the game. Lives on an always active object in the Essentials canvas;
// the dimmed overlay with the buttons is a child that gets shown and hidden.
public class PauseMenu : MonoBehaviour
{
    public const string MainMenuScene = "MainMenu";

    public static bool IsPaused { get; private set; }

    [SerializeField] GameObject overlay;
    [SerializeField] GameObject window;
    [SerializeField] SettingsPanel settings;
    [SerializeField] GameObject quitButton;

    private void Start()
    {
        overlay.SetActive(false);

        // closing the browser tab is the only way to quit a web build
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            quitButton.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // leaving the scene while paused must not leave the game frozen
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) == false) { return; }

        // one key, handled in one place: close whatever is on top first
        ShopPanel shop = GameManager.instance.shopPanel;

        if (settings.IsOpen)
        {
            settings.Close();
        }
        else if (IsPaused)
        {
            Resume();
        }
        else if (shop != null && shop.IsOpen)
        {
            shop.Close();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        overlay.SetActive(true);
        window.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        overlay.SetActive(false);
    }

    public void OpenSettings()
    {
        if (IsPaused == false)
        {
            Pause();
        }

        window.SetActive(false);
        settings.Open(() => window.SetActive(true));
    }

    public void SaveGame()
    {
        SaveManager.instance.Save();
        Notification.Show("Game saved");
    }

    public void ExitToMainMenu()
    {
        SaveManager.instance.Save();
        Resume();
        SceneManager.LoadScene(MainMenuScene);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
