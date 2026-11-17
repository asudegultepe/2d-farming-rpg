using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class CropTile
{
    public int growTimer;
    public int growStage;
    public Crop crop;
    public SpriteRenderer renderer;
    public float damage;
    public Vector3Int position;

    public bool Complete
    {
        get
        {
            if (crop == null) { return false; }
            return growTimer >= crop.timeToGrow;
        }
    }

    // One hour passed. Returns true when the crop looks different afterwards.
    internal bool Tick()
    {
        if (crop == null) { return false; }

        // left in the ground too long, it withers
        damage += 0.02f;
        if (damage > 1f)
        {
            Harvested();
            return true;
        }

        if (Complete) { return false; }

        growTimer += 1;

        if (growStage < crop.growthStageTime.Count && growTimer >= crop.growthStageTime[growStage])
        {
            growStage += 1;
            return true;
        }

        return false;
    }

    internal void Harvested()
    {
        growTimer = 0;
        growStage = 0;
        crop = null;
        damage = 0;

        // there is no renderer while the field's scene isn't loaded
        if (renderer != null)
        {
            renderer.gameObject.SetActive(false);
        }
    }
}

// Lives on the Game Manager in Essentials, which is always loaded, so crops keep
// growing while the player is in another scene. The field's TilemapCropsManager
// only draws them and registers itself here while its scene is loaded.
public class CropsManager : MonoBehaviour
{
    public TilemapCropsManager cropsManager;

    List<CropsContainer> fields = new List<CropsContainer>();

    private void Start()
    {
        // every crop field in the project, collected by SaveDatabaseBuilder
        SaveDatabase database = Resources.Load<SaveDatabase>(SaveDatabase.ResourcePath);
        if (database != null)
        {
            fields = database.cropsContainers;
        }

        GameManager.instance.timeController.onHourPassed += GrowCrops;
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null && GameManager.instance.timeController != null)
        {
            GameManager.instance.timeController.onHourPassed -= GrowCrops;
        }
    }

    void GrowCrops()
    {
        foreach (CropsContainer field in fields)
        {
            field.Tick();
        }
    }

    public void PickUp(Vector3Int position)
    {
        if (cropsManager == null)
        {
            Debug.LogWarning("No tilemap crops manager are referenced in the crops manager");
            return;
        }

        cropsManager.PickUp(position);
    }

    public bool Check(Vector3Int position)
    {
        if (cropsManager == null)
        {
            Debug.LogWarning("No tilemap crops manager are referenced in the crops manager");
            return false;
        }

        return cropsManager.Check(position);
    }

    public void Seed(Vector3Int position, Crop toSeed)
    {
        if (cropsManager == null)
        {
            Debug.LogWarning("No tilemap crops manager are referenced in the crops manager");
            return;
        }

        cropsManager.Seed(position, toSeed);
    }

    public void Plow(Vector3Int position)
    {
        if (cropsManager == null)
        {
            Debug.LogWarning("No tilemap crops manager are referenced in the crops manager");
            return;
        }

        cropsManager.Plow(position);
    }
}
