using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializes game state to JSON via PlayerPrefs.
/// Structured so the backing store is easily swappable to a server.
/// Attach to GameManager object.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    const string SAVE_KEY = "FlowerShopSave";

    [Header("Settings")]
    [SerializeField] float autoSaveInterval = 30f;

    float autoSaveTimer;

    void Awake()
    {
        Services.Register(this);
    }

    void Update()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            Save();
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    void OnApplicationQuit()
    {
        Save();
    }

    public void Save()
    {
        var data = new SaveData();

        // Currencies
        var currency = Services.Get<CurrencyManager>();
        if (currency != null)
        {
            data.petals = currency.GetBalance(CurrencyType.Petals);
            data.coins  = currency.GetBalance(CurrencyType.Coins);
            data.renown = currency.GetBalance(CurrencyType.Renown);
            data.gems   = currency.GetBalance(CurrencyType.Gems);
        }

        // Game phase
        var gm = Services.Get<GameManager>();
        if (gm != null)
            data.phase = (int)gm.CurrentPhase;

        // Upgrades
        var upgrades = Services.Get<UpgradeManager>();
        if (upgrades != null)
        {
            data.upgrades = new List<UpgradeSaveEntry>();
            foreach (var kvp in upgrades.GetSaveData())
                data.upgrades.Add(new UpgradeSaveEntry { id = kvp.Key, level = kvp.Value });
        }

        // Plots
        var garden = Services.Get<GardenManager>();
        if (garden != null)
        {
            data.plots = new List<PlotSaveData>();
            foreach (var plot in garden.Plots)
            {
                data.plots.Add(new PlotSaveData
                {
                    state          = (int)plot.State,
                    flowerName     = plot.CurrentFlower != null ? plot.CurrentFlower.name : "",
                    growthProgress = plot.GrowthProgress
                });
            }

            data.unlockedPlotIndices = garden.GetUnlockedPlotIndices();
            data.unlockedFlowerNames = garden.GetUnlockedFlowerNamesList();
        }

        // Inventory
        var inventory = Services.Get<InventoryManager>();
        if (inventory != null)
            data.inventory = inventory.GetSaveData();

        // Market demand
        var market = Services.Get<MarketManager>();
        if (market != null)
            data.marketDemand = market.GetSaveData();

        // Boost
        var boost = Services.Get<BoostManager>();
        if (boost != null)
            data.boostTimeRemaining = boost.BoostTimeRemaining;

        // Achievements
        if (Services.TryGet<AchievementManager>(out var achievements))
        {
            data.completedAchievements  = achievements.GetSaveData();
            data.totalHarvests          = achievements.TotalHarvests;
            data.totalOrdersFilled      = achievements.TotalOrdersFilled;
            data.totalPetalsEarned      = achievements.TotalPetalsEarned;
        }

        data.lastSaveTime = DateTime.UtcNow.ToBinary();

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("[SaveSystem] Game saved.");
    }

    public bool Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return false;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        SaveData data;

        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to parse save: {e.Message}");
            return false;
        }

        // Currencies
        var currency = Services.Get<CurrencyManager>();
        if (currency != null)
        {
            currency.SetBalance(CurrencyType.Petals, data.petals);
            currency.SetBalance(CurrencyType.Coins,  data.coins);
            currency.SetBalance(CurrencyType.Renown, data.renown);
            currency.SetBalance(CurrencyType.Gems,   data.gems);
        }

        // Phase
        var gm = Services.Get<GameManager>();
        if (gm != null)
            gm.SetPhase((GamePhase)data.phase);

        // Upgrades
        var upgrades = Services.Get<UpgradeManager>();
        if (upgrades != null && data.upgrades != null)
        {
            var dict = new Dictionary<string, int>();
            foreach (var entry in data.upgrades)
                dict[entry.id] = entry.level;
            upgrades.LoadSaveData(dict);
        }

        // Plots
        var garden = Services.Get<GardenManager>();
        if (garden != null)
        {
            // Migrate legacy PlayerPrefs unlock key → JSON on first load
            if (data.unlockedPlotIndices == null || data.unlockedPlotIndices.Count == 0)
            {
                if (PlayerPrefs.HasKey(GardenManager.UNLOCK_SAVE_KEY_LEGACY))
                {
                    var migrated = new List<int>();
                    string raw = PlayerPrefs.GetString(GardenManager.UNLOCK_SAVE_KEY_LEGACY);
                    foreach (string idx in raw.Split(','))
                        if (int.TryParse(idx, out int i)) migrated.Add(i);
                    data.unlockedPlotIndices = migrated;
                    PlayerPrefs.DeleteKey(GardenManager.UNLOCK_SAVE_KEY_LEGACY);
                    Debug.Log("[SaveSystem] Migrated plot unlocks from PlayerPrefs to JSON.");
                }
            }

            garden.ApplyUnlockedPlotIndices(data.unlockedPlotIndices);
            garden.LoadUnlockedFlowers(data.unlockedFlowerNames);

            if (data.plots != null)
                garden.LoadPlotData(data.plots);
        }

        // Inventory
        var inventory = Services.Get<InventoryManager>();
        if (inventory != null)
            inventory.LoadSaveData(data.inventory);

        // Market demand
        var market = Services.Get<MarketManager>();
        if (market != null)
            market.LoadSaveData(data.marketDemand);

        // Boost
        var boost = Services.Get<BoostManager>();
        if (boost != null && data.boostTimeRemaining > 0)
            boost.LoadSaveData(data.boostTimeRemaining);

        // Achievements
        if (Services.TryGet<AchievementManager>(out var achievements))
            achievements.LoadSaveData(data.completedAchievements,
                data.totalHarvests, data.totalOrdersFilled, data.totalPetalsEarned);

        // Offline progress
        if (data.lastSaveTime != 0)
        {
            DateTime lastSave = DateTime.FromBinary(data.lastSaveTime);
            float elapsed = (float)(DateTime.UtcNow - lastSave).TotalSeconds;

            if (elapsed > 0 && garden != null)
            {
                double petalsBefore = currency != null ? currency.GetBalance(CurrencyType.Petals) : 0;

                int offlineHarvests = garden.ApplyOfflineTime(elapsed);

                double petalsAfter = currency != null ? currency.GetBalance(CurrencyType.Petals) : 0;
                double petalsEarned = petalsAfter - petalsBefore;

                if (Services.TryGet<AwayPopup>(out var popup))
                    popup.Show(elapsed, petalsEarned, offlineHarvests);

                Debug.Log($"[SaveSystem] Applied {elapsed:F0}s offline progress. +{petalsEarned:F0} petals, {offlineHarvests} harvests.");
            }
        }

        Debug.Log("[SaveSystem] Game loaded.");
        return true;
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        Debug.Log("[SaveSystem] Save deleted.");
    }

    public bool HasSave() => PlayerPrefs.HasKey(SAVE_KEY);
}

[Serializable]
public class SaveData
{
    public double petals;
    public double coins;
    public double renown;
    public double gems;
    public int phase;
    public long lastSaveTime;
    public List<PlotSaveData> plots;
    public List<int> unlockedPlotIndices;
    public List<string> unlockedFlowerNames;
    public List<int> completedAchievements;
    public int totalHarvests;
    public int totalOrdersFilled;
    public double totalPetalsEarned;
    public List<UpgradeSaveEntry> upgrades;
    public List<InventorySaveEntry> inventory;
    public List<DemandSaveEntry> marketDemand;
    public float boostTimeRemaining;
}

[Serializable]
public class PlotSaveData
{
    public int state;
    public string flowerName;
    public float growthProgress;
}

[Serializable]
public class UpgradeSaveEntry
{
    public string id;
    public int level;
}