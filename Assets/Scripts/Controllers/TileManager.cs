using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class TileManager : MonoBehaviour
{
    public CropsManager cropsManager;
    [SerializeField] Tilemap tilemap;

    // The tilemap lives in the gameplay scene, so it's looked up again after every scene change.
    // While a scene is still loading there is none yet.
    void FindTilemap()
    {
        if (tilemap != null) { return; }

        GameObject go = GameObject.Find("BackgroundTilemap");
        if (go != null)
        {
            tilemap = go.GetComponent<Tilemap>();
        }
    }

    public Vector3Int GetGridPosition(Vector2 position, bool mousePosition)
    {
        FindTilemap();

        if (tilemap == null) { return Vector3Int.zero; }

        Vector3 worldPosition;

        if (mousePosition)
        {
            worldPosition = Camera.main.ScreenToWorldPoint(position);
        }
        else
        {
            worldPosition = position;
        }

        Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);

        return gridPosition;
    }

    public TileBase GetTileBase(Vector3Int gridPosition)
    {
        FindTilemap();

        if (tilemap == null) { return null; }

        TileBase tile = tilemap.GetTile(gridPosition);

        return tile;
    }
}
