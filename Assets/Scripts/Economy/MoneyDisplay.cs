using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Put on a TextMeshPro label to show the player's gold
[RequireComponent(typeof(TextMeshProUGUI))]
public class MoneyDisplay : MonoBehaviour
{
    TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        Wallet wallet = GameManager.instance.wallet;
        wallet.onMoneyChanged += UpdateText;
        UpdateText(wallet.Money);
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null && GameManager.instance.wallet != null)
        {
            GameManager.instance.wallet.onMoneyChanged -= UpdateText;
        }
    }

    void UpdateText(int money)
    {
        text.text = money + "g";
    }
}
