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
}
