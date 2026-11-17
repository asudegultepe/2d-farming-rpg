using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HUD energy bar made of segments that switch between a full and an empty sprite
public class EnergyDisplay : MonoBehaviour
{
    [SerializeField] List<Image> segments = new List<Image>();
    [SerializeField] Sprite full;
    [SerializeField] Sprite empty;

    private void Start()
    {
        Energy energy = GameManager.instance.energy;
        energy.onChanged += UpdateSegments;
        UpdateSegments(energy.Current, energy.Max);
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null && GameManager.instance.energy != null)
        {
            GameManager.instance.energy.onChanged -= UpdateSegments;
        }
    }

    void UpdateSegments(int current, int max)
    {
        // any energy left keeps at least one segment lit
        int lit = Mathf.CeilToInt((float)current / max * segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].sprite = i < lit ? full : empty;
        }
    }
}
