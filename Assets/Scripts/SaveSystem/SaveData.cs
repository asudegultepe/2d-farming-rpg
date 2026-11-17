using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string sceneName;
    public Vector3 playerPosition;

    public int days;
    public float time;

    // -1 = missing (save written before money existed), keep the starting gold
    public int money = -1;

    public List<SlotSaveData> inventory = new List<SlotSaveData>();
    public List<CropsContainerSaveData> cropsContainers = new List<CropsContainerSaveData>();
    public List<string> destroyedNodes = new List<string>();
}

[Serializable]
public class SlotSaveData
{
    public string itemId;
    public int count;
}

[Serializable]
public class CropsContainerSaveData
{
    public string containerId;
    public List<CropTileSaveData> tiles = new List<CropTileSaveData>();
}

[Serializable]
public class CropTileSaveData
{
    public Vector3Int position;
    public string cropId;
    public int growTimer;
    public int growStage;
    public float damage;
}
