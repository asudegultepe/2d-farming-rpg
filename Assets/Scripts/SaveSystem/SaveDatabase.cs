using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Maps assets to string ids so they can be written to a save file.
// Filled automatically in the editor by SaveDatabaseBuilder.
public class SaveDatabase : ScriptableObject
{
    public const string ResourcePath = "SaveDatabase";

    public List<Item> items = new List<Item>();
    public List<Crop> crops = new List<Crop>();
    public List<CropsContainer> cropsContainers = new List<CropsContainer>();

    public Item GetItem(string id)
    {
        if (string.IsNullOrEmpty(id)) { return null; }
        return items.Find(x => x != null && x.name == id);
    }

    public Crop GetCrop(string id)
    {
        if (string.IsNullOrEmpty(id)) { return null; }
        return crops.Find(x => x != null && x.name == id);
    }

    public CropsContainer GetCropsContainer(string id)
    {
        if (string.IsNullOrEmpty(id)) { return null; }
        return cropsContainers.Find(x => x != null && x.name == id);
    }
}
