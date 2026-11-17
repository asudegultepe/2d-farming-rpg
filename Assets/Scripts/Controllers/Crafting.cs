using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crafting : MonoBehaviour
{
    [SerializeField] ItemContainer inventory;

    public void Craft(CraftingRecipe recipe)
    {
        if(inventory.CheckFreeSpace() == false)
        {
            Notification.Show("Not enough space to fit the item after crafting.");
            return;
        }

        for (int i = 0; i < recipe.elements.Count; i++)
        {
            if(inventory.CheckItem(recipe.elements[i]) == false)
            {
                Notification.Show("Missing ingredients for " + recipe.output.item.itemName + ".");
                return;
            }
        }

        for (int i = 0; i < recipe.elements.Count; i++)
        {
            inventory.Remove(recipe.elements[i].item, recipe.elements[i].count);
        }

        inventory.Add(recipe.output.item, recipe.output.count);
        Notification.Show("Crafted " + recipe.output.item.itemName);
    }
}
