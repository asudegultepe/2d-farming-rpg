using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Player preferences that belong to this device rather than to a save file
public static class GameSettings
{
    const string MasterVolumeKey = "settings.masterVolume";
    const string MusicVolumeKey = "settings.musicVolume";

    public static Action onChanged;

    static float masterVolume = 1f;
    static float musicVolume = 0.7f;

    public static float MasterVolume
    {
        get { return masterVolume; }
        set
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            Apply();
        }
    }

    public static float MusicVolume
    {
        get { return musicVolume; }
        set
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            Apply();
        }
    }

    public static bool Fullscreen
    {
        get { return Screen.fullScreen; }
        set { Screen.fullScreen = value; }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
        Apply();
    }

    static void Apply()
    {
        AudioListener.volume = masterVolume;
        onChanged?.Invoke();
    }
}
