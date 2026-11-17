using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Title screen. Starting or continuing loads Essentials, which then loads
// the gameplay scene itself (see GameSceneManager).
public class MainMenu : MonoBehaviour
{
    public const string GameScene = "Essentials";

    [SerializeField] GameObject window;
    [SerializeField] Button continueButton;
    [SerializeField] GameObject quitButton;
    [SerializeField] GameObject confirmNewGame;
    [SerializeField] SettingsPanel settings;

    private void Start()
    {
        Time.timeScale = 1f;
        continueButton.interactable = SaveManager.HasSave;
        confirmNewGame.SetActive(false);

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            quitButton.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) == false) { return; }

        if (settings.IsOpen)
        {
            settings.Close();
        }
        else if (confirmNewGame.activeSelf)
        {
            CancelNewGame();
        }
    }

    public void Continue()
    {
        SceneManager.LoadScene(GameScene);
    }

    public void NewGame()
    {
        // ask before overwriting an existing save
        if (SaveManager.HasSave)
        {
            window.SetActive(false);
            confirmNewGame.SetActive(true);
            return;
        }

        StartNewGame();
    }

    public void StartNewGame()
    {
        SaveManager.DeleteSave();
        SceneManager.LoadScene(GameScene);
    }

    public void CancelNewGame()
    {
        confirmNewGame.SetActive(false);
        window.SetActive(true);
    }

    public void OpenSettings()
    {
        window.SetActive(false);
        settings.Open(() => window.SetActive(true));
    }

    public void Quit()
    {
        Application.Quit();
    }
}
