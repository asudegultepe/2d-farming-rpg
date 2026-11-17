using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemSlot
{
    public Item item;
    public int count;

    public void Copy(ItemSlot slot)
    {
        item = slot.item;
        count = slot.count;
    }

    public void Set(Item item, int count)
    {
        this.item = item;
        this.count = count;
    }

    public void Clear()
    {
        item = null;
        count = 0;
    }
}

[CreateAssetMenu(menuName = "Data/Item Container")]
public class ItemContainer : ScriptableObject
{
    public List<ItemSlot> slots;

    // Raised whenever the contents change, so UI panels can redraw themselves
    [NonSerialized] public Action onChange;

    public void NotifyChanged()
    {
        onChange?.Invoke();
    }

    public void Add(Item item, int count = 1)
    {
        if (item.stackable == true)
        {
            ItemSlot itemSlot = slots.Find(x => x.item == item);

            if (itemSlot != null)
            {
                itemSlot.count += count;
            }
            else
            {
                itemSlot = slots.Find(x => x.item == null);
                if(itemSlot != null)
                {
                    itemSlot.item = item;
                    itemSlot.count = count;
                }
            }
        }
        else
        {
            ItemSlot itemSlot = slots.Find(x => x.item == null);

            if (itemSlot != null)
            {
                itemSlot.item = item;
            }
        }

        NotifyChanged();
    }

    public void Remove(Item itemToRemove, int count = 1)
    {
        if (itemToRemove.stackable)
        {
            ItemSlot itemSlot = slots.Find(x => x.item == itemToRemove);
            if (itemSlot == null) { return; }

            itemSlot.count -= count;
            if(itemSlot.count <= 0)
            {
                itemSlot.Clear();
            }
        }
        else
        {
            while (count > 0)
            {
                count -= 1;

                ItemSlot itemSlot = slots.Find(x => x.item == itemToRemove);
                if (itemSlot == null) { break; }
                itemSlot.Clear();
            }
        }

        NotifyChanged();
    }

    // True if the item fits, either on an existing stack or in an empty slot
    internal bool CanAdd(Item item)
    {
        if (item.stackable && slots.Exists(x => x.item == item)) { return true; }
        return CheckFreeSpace();
    }

    internal bool CheckFreeSpace()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if(slots[i].item == null)
            {
                return true;
            }
        }

        return false;
    }

    internal bool CheckItem(ItemSlot checkingItem)
    {
        ItemSlot itemSlot = slots.Find(x => x.item == checkingItem.item);

        if(itemSlot == null) { return false; }

        if(checkingItem.item.stackable) { return itemSlot.count >= checkingItem.count; }

        return true;
    }
}
