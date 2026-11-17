using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// What a new game starts with. The inventory and crop containers are assets that keep
// whatever happened while playing in the editor, so a new game resets them from here.
[CreateAssetMenu(menuName = "Data/New Game Settings")]
public class NewGameSettings : ScriptableObject
{
    public const string ResourcePath = "NewGameSettings";

    public List<ItemSlot> startingItems = new List<ItemSlot>();
    [Min(0)] public int startingMoney = 100;
}
