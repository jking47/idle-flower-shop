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
    float autoHarvestTimer;

    CurrencyManager currency;

    const string UNLOCK_SAVE_KEY = "UnlockedPlots";

    public IReadOnlyList<FlowerBed> Plots => plots;
    public IReadOnlyList<FlowerData> AvailableFlowers => availableFlowers;
    public bool AutoHarvestUnlocked => autoHarvestUnlocked;

    void Awake()
    {
        Services.Register(this);
    }

    void Start()
    {
        currency = Services.Get<CurrencyManager>();

        for (int i = 0; i < plots.Count; i++)
        {
            plots[i].Initialize(i);
        }

        // Lock rows 2 and 3
        ApplyRowLocks();

        // Restore previously unlocked plots
        LoadUnlockState();
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
    /// Returns true if successful.
    /// </summary>
    public bool PlantFlower(int plotIndex, FlowerData flower)
    {
        if (plotIndex < 0 || plotIndex >= plots.Count) return false;
        if (plots[plotIndex].IsLocked) return false;
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
    /// Apply offline elapsed time to all growing plots.
    /// </summary>
    public void ApplyOfflineTime(float seconds)
    {
        foreach (var plot in plots)
        {
            if (!plot.IsLocked)
                plot.ApplyOfflineTime(seconds);
        }

        if (autoHarvestUnlocked && autoHarvestInterval > 0)
        {
            int cycles = Mathf.FloorToInt(seconds / autoHarvestInterval);
            for (int i = 0; i < cycles; i++)
            {
                AutoHarvestAll();
            }
        }
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

    // --- Unlock Persistence ---

    public void SaveUnlockState()
    {
        var unlocked = new List<string>();
        for (int i = 0; i < plots.Count; i++)
        {
            if (!plots[i].IsLocked)
                unlocked.Add(i.ToString());
        }
        PlayerPrefs.SetString(UNLOCK_SAVE_KEY, string.Join(",", unlocked));
        PlayerPrefs.Save();
    }

    void LoadUnlockState()
    {
        if (!PlayerPrefs.HasKey(UNLOCK_SAVE_KEY)) return;

        string data = PlayerPrefs.GetString(UNLOCK_SAVE_KEY);
        foreach (string idx in data.Split(','))
        {
            if (int.TryParse(idx, out int i) && i >= 0 && i < plots.Count)
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