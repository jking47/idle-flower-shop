using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all flower plots and available flower types.
/// Handles planting requests, auto-harvest ticking, and plot unlock state.
/// </summary>
public class GardenManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] List<FlowerBed> plots = new();
    [SerializeField] List<FlowerData> availableFlowers = new();

    [Header("Auto-Harvest")]
    [SerializeField] float autoHarvestInterval = 5f;

    [Header("Plot Unlock Costs")]
    [Tooltip("Row 1 (plots 0-2) is free. Row 2 costs petals, Row 3 costs coins.")]
    [SerializeField] int plotsPerRow = 3;
    [SerializeField] double row2Cost = 100;
    [SerializeField] double row3Cost = 50;

    bool autoHarvestUnlocked;
    bool autoPlantUnlocked;
    float autoHarvestTimer;

    CurrencyManager currency;

    // Tracks which flower asset names the player has purchased/unlocked.
    // Flowers with unlockCost < 0 are always unlocked.
    readonly HashSet<string> unlockedFlowerNames = new();

    // Legacy PlayerPrefs key — kept only for one-time migration in SaveSystem
    public const string UNLOCK_SAVE_KEY_LEGACY = "UnlockedPlots";

    public IReadOnlyList<FlowerBed> Plots => plots;
    public IReadOnlyList<FlowerData> AvailableFlowers => availableFlowers;
    public bool AutoHarvestUnlocked => autoHarvestUnlocked;
    public bool AutoPlantUnlocked => autoPlantUnlocked;

    void Awake()
    {
        Services.Register(this);
    }

    void Start()
    {
        currency = Services.Get<CurrencyManager>();

        // Flowers with no unlock cost are always available
        foreach (var f in availableFlowers)
            if (f.unlockCost < 0)
                unlockedFlowerNames.Add(f.name);

        for (int i = 0; i < plots.Count; i++)
        {
            plots[i].Initialize(i);
        }

        ApplyRowLocks();

        // Auto-plant first flower on fresh start so new players see immediate progress.
        // SaveSystem.Load() hasn't run yet — check whether any save exists.
        bool isFreshStart = !Services.TryGet<SaveSystem>(out var save) || !save.HasSave();
        // Also treat legacy-only saves (PlayerPrefs unlock key present) as non-fresh
        if (isFreshStart && PlayerPrefs.HasKey(UNLOCK_SAVE_KEY_LEGACY))
            isFreshStart = false;

        if (isFreshStart && availableFlowers.Count > 0)
        {
            if (plots.Count > 0 && !plots[0].IsLocked && plots[0].State == PlotState.Empty)
                plots[0].Plant(availableFlowers[0]);
        }
    }

    void ApplyRowLocks()
    {
        for (int i = 0; i < plots.Count; i++)
        {
            int row = i / plotsPerRow;

            switch (row)
            {
                case 0:
                    // Row 1 — free, no lock
                    break;
                case 1:
                    plots[i].SetLocked(row2Cost, CurrencyType.Petals);
                    break;
                default:
                    plots[i].SetLocked(row3Cost, CurrencyType.Coins);
                    break;
            }
        }
    }

    void Update()
    {
        if (!autoHarvestUnlocked) return;

        autoHarvestTimer += Time.deltaTime;
        if (autoHarvestTimer >= autoHarvestInterval)
        {
            autoHarvestTimer = 0f;
            AutoHarvestAll();
        }
    }

    /// <summary>
    /// Attempt to plant a flower in a specific plot.
    /// If the plot is growing and auto-plant is unlocked, updates the preferred flower instead.
    /// Returns true if successful.
    /// </summary>
    public bool PlantFlower(int plotIndex, FlowerData flower)
    {
        if (plotIndex < 0 || plotIndex >= plots.Count) return false;
        if (plots[plotIndex].IsLocked) return false;

        // If plot is growing, just update what it will auto-plant next
        if (plots[plotIndex].State == PlotState.Growing)
        {
            plots[plotIndex].SetPreferredFlower(flower);
            return true;
        }

        if (plots[plotIndex].State != PlotState.Empty) return false;

        if (flower.plantCost > 0 && !currency.Spend(CurrencyType.Petals, flower.plantCost))
            return false;

        return plots[plotIndex].Plant(flower);
    }

    /// <summary>
    /// Plant in the first available empty unlocked plot.
    /// </summary>
    public bool PlantFlowerInFirstEmpty(FlowerData flower)
    {
        for (int i = 0; i < plots.Count; i++)
        {
            if (!plots[i].IsLocked && plots[i].State == PlotState.Empty)
                return PlantFlower(i, flower);
        }
        return false;
    }

    public void UnlockAutoHarvest()
    {
        autoHarvestUnlocked = true;
    }

    public void UnlockAutoPlant()
    {
        autoPlantUnlocked = true;
    }

    // --- Flower Type Unlocks ---

    public bool IsFlowerUnlocked(FlowerData flower)
    {
        return flower.unlockCost < 0 || unlockedFlowerNames.Contains(flower.name);
    }

    /// <summary>
    /// Attempts to spend petals and unlock a flower type.
    /// Returns true on success, false if insufficient petals.
    /// </summary>
    public bool TryUnlockFlower(FlowerData flower)
    {
        if (IsFlowerUnlocked(flower)) return true;
        if (!currency.Spend(CurrencyType.Petals, flower.unlockCost)) return false;

        unlockedFlowerNames.Add(flower.name);
        EventBus.Publish(new FlowerTypeUnlockedEvent { flowerName = flower.name });
        Debug.Log($"[Garden] Unlocked flower: {flower.displayName}");
        return true;
    }

    public List<string> GetUnlockedFlowerNamesList() => new(unlockedFlowerNames);

    public void LoadUnlockedFlowers(List<string> names)
    {
        if (names == null) return;
        foreach (string n in names)
            unlockedFlowerNames.Add(n);
    }

    void AutoHarvestAll()
    {
        foreach (var plot in plots)
        {
            if (!plot.IsLocked && plot.State == PlotState.Bloomed)
            {
                plot.AutoHarvest();
            }
        }
    }

    /// <summary>
    /// Apply offline elapsed time to all growing plots, simulating multiple
    /// grow → harvest → replant cycles per plot. Returns total offline harvests.
    /// </summary>
    public int ApplyOfflineTime(float seconds)
    {
        int totalHarvests = 0;
        foreach (var plot in plots)
        {
            if (!plot.IsLocked)
                totalHarvests += SimulateOfflinePlot(plot, seconds);
        }
        return totalHarvests;
    }

    /// <summary>
    /// Simulates grow/harvest/replant cycles for a single plot over the offline period.
    /// Returns how many times the plot was harvested.
    /// </summary>
    int SimulateOfflinePlot(FlowerBed plot, float remainingTime)
    {
        int harvests   = 0;
        int maxCycles  = 500; // safety cap — prevents infinite loops on very short grow times
        int iterations = 0;

        while (remainingTime > 0f && iterations++ < maxCycles)
        {
            if (plot.State == PlotState.Growing)
            {
                float timeToBloom = (1f - plot.GrowthProgress) * plot.GrowthDuration;

                if (remainingTime < timeToBloom)
                {
                    // Not enough time to bloom — advance partial growth and stop
                    plot.ApplyOfflineTime(remainingTime);
                    break;
                }

                // Enough time to bloom this plant
                plot.ForceBloom();
                remainingTime -= timeToBloom;
            }
            else if (plot.State == PlotState.Bloomed)
            {
                if (!autoHarvestUnlocked) break;

                plot.AutoHarvest(); // earns petals; auto-replants if unlocked + affordable
                harvests++;

                // If auto-replant didn't happen the plot is empty — nothing more to simulate
                if (plot.State != PlotState.Growing) break;
            }
            else
            {
                break; // Empty with no auto-plant — done
            }
        }

        return harvests;
    }

    /// <summary>
    /// Restore plot state from save data.
    /// </summary>
    public void LoadPlotData(List<PlotSaveData> data)
    {
        for (int i = 0; i < data.Count && i < plots.Count; i++)
        {
            var plotData = data[i];
            if (string.IsNullOrEmpty(plotData.flowerName)) continue;

            FlowerData flower = FindFlower(plotData.flowerName);
            if (flower == null) continue;

            plots[i].LoadState(flower, (PlotState)plotData.state, plotData.growthProgress);
        }
    }

    // --- Unlock Persistence (JSON-based, via SaveSystem) ---

    /// <summary>Returns indices of all currently unlocked plots for serialization.</summary>
    public List<int> GetUnlockedPlotIndices()
    {
        var result = new List<int>();
        for (int i = 0; i < plots.Count; i++)
        {
            if (!plots[i].IsLocked)
                result.Add(i);
        }
        return result;
    }

    /// <summary>Restores unlock state from deserialized indices.</summary>
    public void ApplyUnlockedPlotIndices(List<int> indices)
    {
        if (indices == null) return;
        foreach (int i in indices)
        {
            if (i >= 0 && i < plots.Count)
                plots[i].ClearLock();
        }
    }

    FlowerData FindFlower(string assetName)
    {
        foreach (var flower in availableFlowers)
        {
            if (flower.name == assetName) return flower;
        }
        return null;
    }
}