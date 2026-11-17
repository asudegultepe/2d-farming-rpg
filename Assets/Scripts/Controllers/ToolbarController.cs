using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolbarController : MonoBehaviour
{
    [SerializeField] int toolbarSize = 9;
    int selectedTool;

    public Action<int> onChange;

    public Item GetItem
    {
        get {
            return GameManager.instance.inventoryContainer.slots[selectedTool].item;
        }
    }

    public ItemSlot GetSlot
    {
        get {
            return GameManager.instance.inventoryContainer.slots[selectedTool];
        }
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) { return; }

        // number keys 1-9 select a slot directly
        for (int i = 0; i < toolbarSize && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedTool = i;
                onChange?.Invoke(selectedTool);
                return;
            }
        }

        float delta = Input.mouseScrollDelta.y;

        if (delta != 0)
        {
            if(delta > 0)
            {
                selectedTool += 1;
                selectedTool = (selectedTool >= toolbarSize ? 0 : selectedTool);
            }
            else
            {
                selectedTool -= 1;
                selectedTool = (selectedTool < 0 ? toolbarSize - 1 : selectedTool);
            }
            onChange?.Invoke(selectedTool);
        }
    }

    internal void Set(int id)
    {
        selectedTool = id;
    }
}
