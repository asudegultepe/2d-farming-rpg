using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class TileManager : MonoBehaviour
{
    [SerializeField] private Tilemap interactableMap;
    [SerializeField] private Tilemap tileMap;

    [SerializeField] private Tile hiddenInteractableMap;
    [SerializeField] private Tile interactedTile;

    void Start()
    {
        /*foreach(var position in interactableMap.cellBounds.allPositionsWithin)
        {
            TileBase tile = interactableMap.GetTile(position);

            if (tile != null && tile.name == "Interactable_Visible")
            {
                interactableMap.SetTile(position, hiddenInteractableMap);
            }
        }*/
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GetTileBase(Input.mousePosition);
        }
    }

    public bool IsInteractable(Vector3Int position)
    {
        TileBase tile = interactableMap.GetTile(position);

        if (tile != null)
        {
            if(tile.name == "Interactable")
            {
                return true;
            }
        }

        return false;
    }

    public void SetInteracted(Vector3Int position)
    {
        interactableMap.SetTile(position, interactedTile);
    }

    public TileBase GetTileBase(Vector2 mousePosition)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3Int gridPosition = tileMap.WorldToCell(worldPosition);

        TileBase tile = tileMap.GetTile(gridPosition);

        Debug.Log("Tile in position =" + gridPosition + "is" + tile);

        return null;
    }
}
