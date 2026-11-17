using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The player's stamina. Tools cost energy, sleeping refills it.
// Added to the Game Manager automatically, like the Wallet.
public class Energy : MonoBehaviour
{
    [SerializeField] int max = 100;

    int current;

    // current, max
    public Action<int, int> onChanged;

    public int Max
    {
        get { return max; }
    }

    public int Current
    {
        get { return current; }
        set
        {
            current = Mathf.Clamp(value, 0, max);
            onChanged?.Invoke(current, max);
        }
    }

    private void Awake()
    {
        current = max;
    }

    public bool Has(int amount)
    {
        return current >= amount;
    }

    public void Use(int amount)
    {
        Current -= amount;
    }

    public void Refill()
    {
        Current = max;
    }
}
