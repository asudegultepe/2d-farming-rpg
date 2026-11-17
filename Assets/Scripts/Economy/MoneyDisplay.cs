using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Put on a TextMeshPro label to show the player's gold
[RequireComponent(typeof(TextMeshProUGUI))]
public class MoneyDisplay : MonoBehaviour
{
    [Tooltip("Leave empty when a coin icon is shown next to the label")]
    [SerializeField] string suffix = "g";

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
        // 1,250 instead of 1250
        text.text = money.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + suffix;
    }
}
