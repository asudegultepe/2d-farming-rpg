using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Data", menuName = "Data/Item")]
public class Item : ScriptableObject
{
    public string itemName = "Item Name";
    public bool stackable;
    public Sprite icon;
    public ToolAction onAction;
    public ToolAction onTileMapAction;
    public ToolAction onItemUsed;
    public Crop crop;

    [Header("Economy")]
    [Tooltip("Gold received for one of this item at the shipping bin. 0 means it can't be sold.")]
    [Min(0)] public int sellPrice;
    [Tooltip("Gold a shop charges for one of this item.")]
    [Min(0)] public int buyPrice;
}
