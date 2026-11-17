using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class ToolsCharController : MonoBehaviour
{
    Movement player;
    Rigidbody2D rgbd2d;
    ToolbarController toolbarController;
    Animator animator;
    [SerializeField] float offsetDistance = 1f;
    [SerializeField] MarkerManager markerManager;
    [SerializeField] TileManager tileManager;
    [SerializeField] float maxDistance = 2f;
    [SerializeField] ToolAction onTilePickUp;

    Vector3Int selectedTilePosition;
    bool selectable;

    private void Awake()
    {
        player = GetComponent<Movement>();
        rgbd2d = GetComponent<Rigidbody2D>();
        toolbarController = GetComponent<ToolbarController>();
        animator = gameObject.GetComponent<Animator>();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) { return; }

        SelectTile();
        CanSelectCheck();
        Marker();
        if (Input.GetMouseButtonDown(0))
        {
            // a click on the toolbar or a menu shouldn't also swing the tool
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return; }

            if(UseToolWorld() == true){
                return;
            }
            UseToolGrid();
        }
    }

    private void SelectTile()
    {
        selectedTilePosition = tileManager.GetGridPosition(Input.mousePosition, true);
    }

    void CanSelectCheck()
    {
        Vector2 playerPosition = transform.position;
        Vector2 cameraPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        selectable = Vector2.Distance(playerPosition, cameraPosition) < maxDistance;
        markerManager.Show(selectable);
    }

    private void Marker()
    {
        markerManager.markedCellPosition = selectedTilePosition;
    }

    private bool UseToolWorld()
    {
        Vector2 position = rgbd2d.position + player.lastMotionVector * offsetDistance;

        Item item = toolbarController.GetItem;
        if(item == null) { return false; }
        if(item.onAction == null) { return false; }
        // handled, so the click doesn't fall through to the grid action either
        if (HasEnergyFor(item) == false) { return true; }

        animator.SetTrigger("act");

        bool complete = item.onAction.OnApply(position);

        if (complete == true)
        {
            if (item.onItemUsed != null)
            {
                item.onItemUsed.OnItemUsed(item, GameManager.instance.inventoryContainer);
            }
            GameManager.instance.energy.Use(item.energyCost);
        }

        return complete;
    }

    bool HasEnergyFor(Item item)
    {
        if (GameManager.instance.energy.Has(item.energyCost)) { return true; }

        Notification.Show("You're too tired. Get some sleep.");
        return false;
    }

    private void UseToolGrid()
    {
        if(selectable == true)
        {
            Item item = toolbarController.GetItem;
            if(item == null) 
            {
                PickUpTile();
                return; 
            }
            if(item.onTileMapAction == null) { return; }
            if (HasEnergyFor(item) == false) { return; }

            animator.SetTrigger("act");
            bool complete = item.onTileMapAction.OnApplyToTileMap(selectedTilePosition, tileManager, item);

            if(complete == true)
            {
                if(item.onItemUsed != null)
                {
                    item.onItemUsed.OnItemUsed(item, GameManager.instance.inventoryContainer);
                }
                GameManager.instance.energy.Use(item.energyCost);
            }
        }
    }

    private void PickUpTile()
    {
        if (onTilePickUp == null) { return; }
        onTilePickUp.OnApplyToTileMap(selectedTilePosition, tileManager, null);
    }
}
