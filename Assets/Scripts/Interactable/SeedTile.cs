using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Tool Action/Seeded Tile")]
public class SeedTile : ToolAction
{
    public override bool OnApplyToTileMap(Vector3Int gridPosition, TileManager tileManager, Item item)
    {
        if(tileManager.cropsManager.Check(gridPosition) == false) { return false; }

        tileManager.cropsManager.Seed(gridPosition, item.crop);

        return true;
    }

    public override void OnItemUsed(Item usedItem, ItemContainer inventory)
    {
        inventory.Remove(usedItem);
    }
}
