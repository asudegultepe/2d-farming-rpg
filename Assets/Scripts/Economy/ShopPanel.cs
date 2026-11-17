using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Same layout as the crafting panel: one button per item for sale, click to buy one.
// Closes by itself when the player walks away from the shopkeeper, or on Escape.
public class ShopPanel : ItemPanel
{
    [SerializeField] float closeDistance = 3f;

    List<Item> stock = new List<Item>();
    Transform shopkeeper;

    public void Open(List<Item> stock, Transform shopkeeper)
    {
        this.stock = stock;
        this.shopkeeper = shopkeeper;
        gameObject.SetActive(true);
        Show();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public bool IsOpen
    {
        get { return gameObject.activeSelf; }
    }

    // Escape is handled by PauseMenu, which closes the shop before it would pause
    private void Update()
    {
        if (shopkeeper == null) { return; }

        Vector3 playerPosition = GameManager.instance.player.transform.position;
        if (Vector2.Distance(playerPosition, shopkeeper.position) > closeDistance)
        {
            Close();
        }
    }

    public override void Show()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (i >= stock.Count || stock[i] == null)
            {
                buttons[i].Clean();
                continue;
            }

            Item item = stock[i];
            buttons[i].Set(new ItemSlot { item = item, count = 1 });
            // the count label shows the price instead
            buttons[i].text.gameObject.SetActive(true);
            buttons[i].text.text = item.buyPrice + "g";
        }
    }

    public override void OnClick(int id)
    {
        if (id >= stock.Count || stock[id] == null) { return; }

        Item item = stock[id];
        ItemContainer inventory = GameManager.instance.inventoryContainer;
        Wallet wallet = GameManager.instance.wallet;

        if (inventory.CanAdd(item) == false)
        {
            Notification.Show("Your inventory is full.");
            return;
        }

        if (wallet.TrySpend(item.buyPrice) == false)
        {
            Notification.Show("Not enough gold for " + item.itemName + ".");
            return;
        }

        inventory.Add(item);
        Notification.Show("Bought " + item.itemName + " for " + item.buyPrice + "g");
    }
}
