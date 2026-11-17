using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] float timeToSwitch;

    [SerializeField] AudioClip playOnStart;

    private void Start()
    {
        GameSettings.onChanged += ApplyVolume;
        Play(playOnStart, true);
    }

    private void OnDestroy()
    {
        GameSettings.onChanged -= ApplyVolume;
    }

    void ApplyVolume()
    {
        audioSource.volume = GameSettings.MusicVolume;
    }

    private void Play(AudioClip musicToPlay, bool interrupt = false) 
    {
        if(musicToPlay == null) { return; }

        if (interrupt == true)
        {
            audioSource.volume = GameSettings.MusicVolume;
            audioSource.clip = musicToPlay;
            audioSource.Play();
        }
        else
        {
            switchTo = musicToPlay;
            StartCoroutine(SmoothSwitchMusic());
        }
    }

    AudioClip switchTo;
    float volume;

    IEnumerator SmoothSwitchMusic()
    {
        volume = 1f;

        while(volume > 0f)
        {
            volume -= Time.deltaTime / timeToSwitch;
            if (volume < 0f) { volume = 0f; }
            audioSource.volume = volume * GameSettings.MusicVolume;
            yield return new WaitForEndOfFrame();
        }

        Play(switchTo, true);
    }
}
