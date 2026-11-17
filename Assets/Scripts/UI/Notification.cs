using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Short on-screen messages ("Sold 5 Turnip for 175g", "Not enough gold").
// Put on a TextMeshPro label with a CanvasGroup; call Notification.Show from anywhere.
[RequireComponent(typeof(CanvasGroup))]
public class Notification : MonoBehaviour
{
    static Notification instance;

    [SerializeField] TextMeshProUGUI text;
    [SerializeField] float visibleTime = 2f;
    [SerializeField] float fadeTime = 0.5f;

    CanvasGroup canvasGroup;
    float timer;

    private void Awake()
    {
        instance = this;
        if (text == null) { text = GetComponentInChildren<TextMeshProUGUI>(); }
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public static void Show(string message)
    {
        if (instance == null)
        {
            Debug.Log(message);
            return;
        }

        instance.text.text = message;
        instance.timer = instance.visibleTime + instance.fadeTime;
    }

    private void Update()
    {
        if (timer <= 0f) { return; }

        timer -= Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Clamp01(timer / fadeTime);
    }
}
