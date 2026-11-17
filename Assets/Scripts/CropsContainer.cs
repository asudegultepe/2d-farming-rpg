using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Crops Container")]
public class CropsContainer : ScriptableObject
{
    public List<CropTile> crops;

    // Raised when a tile's growth stage changes or it withers, so the field can redraw it
    [NonSerialized] public Action<CropTile> onTileChanged;

    public void Tick()
    {
        foreach (CropTile tile in crops)
        {
            if (tile.Tick())
            {
                onTileChanged?.Invoke(tile);
            }
        }
    }

    public CropTile Get(Vector3Int position)
    {
        return crops.Find(x => x.position == position);
    }

    public void Add(CropTile crop)
    {
        crops.Add(crop);
    }
}
