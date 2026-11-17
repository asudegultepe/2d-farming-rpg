using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    const string FileName = "save.json";

    public static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, FileName); }
    }

    public static bool HasSave
    {
        get { return File.Exists(SavePath); }
    }

    public static void DeleteSave()
    {
        if (HasSave)
        {
            File.Delete(SavePath);
        }
    }

    // Gameplay scene stored in the save, empty for a new game.
    // GameSceneManager loads it when the game starts from the main menu.
    public string SavedScene { get; private set; }

    [SerializeField] KeyCode saveKey = KeyCode.F5;

    SaveDatabase database;
    HashSet<string> destroyedNodes = new HashSet<string>();

    // Added by GameManager.Awake, so inventory and crops are restored
    // before any Start reads them (UI panels, crop tilemaps, resource nodes).
    private void Awake()
    {
        instance = this;

        database = Resources.Load<SaveDatabase>(SaveDatabase.ResourcePath);
        if (database == null)
        {
            Debug.LogWarning("SaveDatabase not found in Resources, items and crops won't be saved.");
        }

        Debug.Log("Save system ready, press " + saveKey + " to save.");

        Load();
    }

    private void Start()
    {
        GameManager.instance.timeController.onNextDay += Save;
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null && GameManager.instance.timeController != null)
        {
            GameManager.instance.timeController.onNextDay -= Save;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(saveKey))
        {
            Save();
            Notification.Show("Game saved");
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public bool IsNodeDestroyed(string nodeId)
    {
        return destroyedNodes.Contains(nodeId);
    }

    public void RegisterDestroyedNode(string nodeId)
    {
        destroyedNodes.Add(nodeId);
    }

    public void Save()
    {
        if (database == null) { return; }

        SaveData data = new SaveData();

        Transform player = GameManager.instance.player.transform;
        // Essentials is always loaded, only remember which gameplay scene is on top of it
        GameSceneManager sceneManager = GameSceneManager.instance;
        if (sceneManager.CurrentScene != sceneManager.gameObject.scene.name)
        {
            data.sceneName = sceneManager.CurrentScene;
        }
        data.playerPosition = player.position;

        data.days = GameManager.instance.timeController.days;
        data.time = GameManager.instance.timeController.CurrentTime;
        data.money = GameManager.instance.wallet.Money;

        foreach (ItemSlot slot in GameManager.instance.inventoryContainer.slots)
        {
            SlotSaveData slotData = new SlotSaveData();
            slotData.itemId = slot.item != null ? slot.item.name : "";
            slotData.count = slot.count;
            data.inventory.Add(slotData);
        }

        foreach (CropsContainer container in database.cropsContainers)
        {
            CropsContainerSaveData containerData = new CropsContainerSaveData();
            containerData.containerId = container.name;

            foreach (CropTile tile in container.crops)
            {
                CropTileSaveData tileData = new CropTileSaveData();
                tileData.position = tile.position;
                tileData.cropId = tile.crop != null ? tile.crop.name : "";
                tileData.growTimer = tile.growTimer;
                tileData.growStage = tile.growStage;
                tileData.damage = tile.damage;
                containerData.tiles.Add(tileData);
            }

            data.cropsContainers.Add(containerData);
        }

        data.destroyedNodes = new List<string>(destroyedNodes);

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game saved to " + SavePath);
    }

    private void Load()
    {
        if (database == null) { return; }
        if (HasSave == false)
        {
            StartNewGame();
            return;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (System.Exception e)
        {
            Debug.LogError("Could not read save file: " + e.Message);
            return;
        }

        LoadInventory(data);
        LoadCrops(data);
        if (data.money >= 0)
        {
            GameManager.instance.wallet.Money = data.money;
        }
        destroyedNodes = new HashSet<string>(data.destroyedNodes);
        SavedScene = data.sceneName;

        StartCoroutine(LoadWorld(data));
    }

    // Resets the asset-backed state (inventory, crops) that would otherwise
    // carry over from an earlier game or from playing in the editor
    private void StartNewGame()
    {
        NewGameSettings settings = Resources.Load<NewGameSettings>(NewGameSettings.ResourcePath);
        if (settings == null)
        {
            Debug.LogWarning("NewGameSettings not found in Resources, starting with the current inventory.");
            return;
        }

        ItemContainer inventory = GameManager.instance.inventoryContainer;
        foreach (ItemSlot slot in inventory.slots)
        {
            slot.Clear();
        }
        foreach (ItemSlot slot in settings.startingItems)
        {
            if (slot.item != null)
            {
                inventory.Add(slot.item, slot.count);
            }
        }

        foreach (CropsContainer container in database.cropsContainers)
        {
            container.crops = new List<CropTile>();
        }

        GameManager.instance.wallet.Money = settings.startingMoney;
    }

    private void LoadInventory(SaveData data)
    {
        List<ItemSlot> slots = GameManager.instance.inventoryContainer.slots;

        for (int i = 0; i < slots.Count; i++)
        {
            if (i >= data.inventory.Count)
            {
                slots[i].Clear();
                continue;
            }

            Item item = database.GetItem(data.inventory[i].itemId);
            if (item == null)
            {
                slots[i].Clear();
                continue;
            }

            slots[i].Set(item, data.inventory[i].count);
        }
    }

    private void LoadCrops(SaveData data)
    {
        foreach (CropsContainerSaveData containerData in data.cropsContainers)
        {
            CropsContainer container = database.GetCropsContainer(containerData.containerId);
            if (container == null) { continue; }

            container.crops = new List<CropTile>();

            foreach (CropTileSaveData tileData in containerData.tiles)
            {
                CropTile tile = new CropTile();
                tile.position = tileData.position;
                tile.crop = database.GetCrop(tileData.cropId);
                tile.growTimer = tileData.growTimer;
                tile.growStage = tileData.growStage;
                tile.damage = tileData.damage;
                container.crops.Add(tile);
            }
        }
    }

    // Time and player position are applied after the first frame,
    // once DayTimeController and GameSceneManager have run their Start.
    IEnumerator LoadWorld(SaveData data)
    {
        yield return null;

        DayTimeController timeController = GameManager.instance.timeController;
        timeController.days = data.days;
        timeController.CurrentTime = data.time;

        GameSceneManager sceneManager = GameSceneManager.instance;

        if (string.IsNullOrEmpty(data.sceneName) == false && data.sceneName != sceneManager.CurrentScene)
        {
            sceneManager.InitSwitchScene(data.sceneName, data.playerPosition);
        }
        else
        {
            sceneManager.WarpPlayer(data.playerPosition);
        }

        Debug.Log("Game loaded from " + SavePath);
    }
}
