using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Volume and display options, shared by the main menu and the pause menu
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] Slider masterVolume;
    [SerializeField] Slider musicVolume;
    [SerializeField] Toggle fullscreen;

    Action onClosed;

    private void Awake()
    {
        masterVolume.onValueChanged.AddListener(value => GameSettings.MasterVolume = value);
        musicVolume.onValueChanged.AddListener(value => GameSettings.MusicVolume = value);
        fullscreen.onValueChanged.AddListener(value => GameSettings.Fullscreen = value);
    }

    public bool IsOpen
    {
        get { return gameObject.activeSelf; }
    }

    public void Open(Action onClosed)
    {
        this.onClosed = onClosed;

        masterVolume.SetValueWithoutNotify(GameSettings.MasterVolume);
        musicVolume.SetValueWithoutNotify(GameSettings.MusicVolume);
        fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen);

        gameObject.SetActive(true);
    }

    public void Close()
    {
        GameSettings.Save();
        gameObject.SetActive(false);
        onClosed?.Invoke();
    }
}
