using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Right click with an item selected on the toolbar to sell the whole stack
public class ShippingBin : Interactable
{
    public override void Interact(Player player)
    {
        ItemSlot slot = player.GetComponent<ToolbarController>().GetSlot;

        if (slot.item == null)
        {
            Notification.Show("Select an item on the toolbar to sell it.");
            return;
        }

        if (slot.item.sellPrice <= 0)
        {
            Notification.Show(slot.item.itemName + " can't be sold.");
            return;
        }

        Item item = slot.item;
        int count = item.stackable ? slot.count : 1;
        int total = item.sellPrice * count;

        GameManager.instance.inventoryContainer.Remove(item, count);
        GameManager.instance.wallet.Add(total);

        Notification.Show("Sold " + count + " " + item.itemName + " for " + total + "g");
    }
}
