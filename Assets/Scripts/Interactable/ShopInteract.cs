using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Put on a shopkeeper NPC. Right click opens the shop panel with this stock.
public class ShopInteract : Interactable
{
    [SerializeField] List<Item> stock;

    public override void Interact(Player player)
    {
        GameManager.instance.shopPanel.Open(stock, transform);
    }
}
