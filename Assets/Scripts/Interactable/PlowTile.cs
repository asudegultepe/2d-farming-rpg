using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Data/Tool Action/Plow")]
public class PlowTile : ToolAction
{
    [SerializeField] List<TileBase> canPlow;

    public override bool OnApplyToTileMap(Vector3Int gridPosition, TileManager tileManager, Item item)
    {
        TileBase tileToPlow = tileManager.GetTileBase(gridPosition);

        if(canPlow.Contains(tileToPlow) == false)
        {
            return false;
        }

        tileManager.cropsManager.Plow(gridPosition);

        return true;
    }
}
