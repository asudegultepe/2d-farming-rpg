using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Tool Action/Harvest")]
public class TilePickUpAction : ToolAction
{
    public override bool OnApplyToTileMap(Vector3Int gridPosition, TileManager tileManager, Item item)
    {
        tileManager.cropsManager.PickUp(gridPosition);
        return true;
    }
}
