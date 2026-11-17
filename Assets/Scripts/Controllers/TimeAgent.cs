using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeAgent : MonoBehaviour
{
    public Action onTimeTick;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        GameManager.instance.timeController.Subscribe(this);
    }

    public void Invoke()
    {
        onTimeTick?.Invoke();
    }

    private void OnDestroy()
    {
        // when play mode ends the Game Manager may already be gone
        if (GameManager.instance == null || GameManager.instance.timeController == null) { return; }

        GameManager.instance.timeController.Unsubscribe(this);
    }
}
