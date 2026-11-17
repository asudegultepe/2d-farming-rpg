using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

public class DayTimeController : MonoBehaviour
{
    const float secondsInDay = 86400f;
    const float phaseLenght = 3600f; //15 min chunk of time

    public const int daysInSeason = 28;
    static readonly string[] weekDays = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    static readonly string[] seasonNames = { "Spring", "Summer", "Fall", "Winter" };

    [SerializeField] Color nightLightColor;
    [SerializeField] AnimationCurve nightTimeCurve;
    [SerializeField] Color dayLightColor = Color.white;

    float time;
    [SerializeField] float timeScale = 60f;
    [SerializeField] float startAtTime = 21600f;

    [SerializeField] TextMeshProUGUI text;
    [SerializeField] TextMeshProUGUI dayText;
    [SerializeField] Light2D globalLight;
    public int days = 0;

    [Header("Calendar HUD (optional)")]
    [SerializeField] TextMeshProUGUI seasonText;
    [SerializeField] Image seasonIcon;
    [Tooltip("Spring, Summer, Fall, Winter")]
    [SerializeField] Sprite[] seasonIcons;
    [SerializeField] RectTransform dayProgressFill;
    float dayProgressWidth;
    int shownDay = -1;

    // Days are counted from 0; the calendar starts on Spring 1, Year 1
    public int Season
    {
        get { return days / daysInSeason % 4; }
    }

    public int DayOfSeason
    {
        get { return days % daysInSeason + 1; }
    }

    public int Year
    {
        get { return days / (daysInSeason * 4) + 1; }
    }

    List<TimeAgent> agents;

    public Action onNextDay;
    // Once per in-game hour, for systems outside the loaded scenes (crop growth)
    public Action onHourPassed;

    public float CurrentTime
    {
        get { return time; }
        set { time = value; }
    }

    private void Awake()
    {
        agents = new List<TimeAgent>();

        // the fill is laid out at full width, shrink it from there
        if (dayProgressFill != null)
        {
            dayProgressWidth = dayProgressFill.sizeDelta.x;
        }
    }

    private void Start()
    {
        time = startAtTime;
    }

    public void Subscribe(TimeAgent timeAgent)
    {
        agents.Add(timeAgent);
    }

    public void Unsubscribe(TimeAgent timeAgent)
    {
        agents.Remove(timeAgent);
    }

    float Hours
    {
        get { return time / 3600f;  }
    }

    float Minutes
    {
        get { return time % 3600f / 60f; }
    }

    private void Update()
    {
        time += Time.deltaTime * timeScale;

        TimeValueCalculation();
        DayLight();
        DayProgress();

        // days can also be changed from outside (loading a save), so compare instead of using NextDay
        if (shownDay != days)
        {
            shownDay = days;
            UpdateCalendar();
        }

        if (time > secondsInDay)
        {
            NextDay();
        }

        TimeAgents();
    }

    private void TimeValueCalculation()
    {
        int hh = (int)Hours;
        int mm = (int)Minutes;
        text.text = hh.ToString("00") + ":" + mm.ToString("00");
    }

    private void UpdateCalendar()
    {
        dayText.text = weekDays[days % weekDays.Length] + " " + DayOfSeason;

        if (seasonText != null)
        {
            seasonText.text = seasonNames[Season] + " · Year " + Year;
        }

        if (seasonIcon != null && seasonIcons != null && Season < seasonIcons.Length)
        {
            seasonIcon.sprite = seasonIcons[Season];
        }
    }

    private void DayProgress()
    {
        if (dayProgressFill == null) { return; }

        Vector2 size = dayProgressFill.sizeDelta;
        size.x = dayProgressWidth * Mathf.Clamp01(time / secondsInDay);
        dayProgressFill.sizeDelta = size;
    }

    private void DayLight()
    {
        float v = nightTimeCurve.Evaluate(Hours);
        Color c = Color.Lerp(dayLightColor, nightLightColor, v);
        globalLight.color = c;
    }

    int oldPhase = 0;

    private void TimeAgents()
    {
        int currentPhase = (int)(time / phaseLenght);

        if (oldPhase != currentPhase)
        {
            oldPhase = currentPhase;
            TickAgents();
        }
    }

    private void TickAgents()
    {
        onHourPassed?.Invoke();

        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].Invoke();
        }
    }

    private void NextDay()
    {
        time = 0;
        days += 1;
        onNextDay?.Invoke();
    }

    // Sleeping jumps to the next morning. Every skipped hour still ticks the
    // time agents, so crops grow as if the night had passed normally.
    public void SkipToMorning()
    {
        // after midnight the day has already changed, so wake up the same day
        bool afterMidnight = time < startAtTime;
        float wakeUp = afterMidnight ? startAtTime : secondsInDay + startAtTime;
        int hoursSkipped = (int)(wakeUp / phaseLenght) - (int)(time / phaseLenght);

        for (int i = 0; i < hoursSkipped; i++)
        {
            TickAgents();
        }

        time = startAtTime;
        oldPhase = (int)(time / phaseLenght);

        if (afterMidnight == false)
        {
            days += 1;
            onNextDay?.Invoke();
        }
    }
}
