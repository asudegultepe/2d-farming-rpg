using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        instance = this;
        player = FindObjectOfType<Player>();

        // the wallet has to exist before SaveManager loads the saved gold into it
        wallet = GetComponent<Wallet>();
        if (wallet == null)
        {
            wallet = gameObject.AddComponent<Wallet>();
        }

        if (SaveManager.instance == null)
        {
            gameObject.AddComponent<SaveManager>();
        }
    }

    public Player player;
    public ItemContainer inventoryContainer;
    public ItemManager dragAndDrop;
    public DayTimeController timeController;
    public DialogueSystem dialogueSystem;
    public ShopPanel shopPanel;
    [HideInInspector] public Wallet wallet;
}
