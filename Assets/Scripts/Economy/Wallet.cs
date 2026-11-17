using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Holds the player's gold. Added to the Game Manager automatically.
public class Wallet : MonoBehaviour
{
    [SerializeField] int startingMoney = 100;

    int money;

    public Action<int> onMoneyChanged;

    public int Money
    {
        get { return money; }
        set
        {
            money = Mathf.Max(0, value);
            onMoneyChanged?.Invoke(money);
        }
    }

    private void Awake()
    {
        money = startingMoney;
    }

    public void Add(int amount)
    {
        Money += amount;
    }

    public bool CanAfford(int amount)
    {
        return money >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (CanAfford(amount) == false) { return false; }

        Money -= amount;
        return true;
    }
}
