using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all flower plots and available flower types.
/// Handles planting requests and auto-harvest ticking.
/// </summary>
public class GardenManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] List<FlowerBed> plots = new();
    [SerializeField] List<FlowerData> availableFlowers = new();

    [Header("Auto-Harvest")]
    [SerializeField] float autoHarvestInterval = 5f;

    bool autoHarvestUnlocked;
    float autoHarvestTimer;

    CurrencyManager currency;

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
        if (plots[plotIndex].State != PlotState.Empty) return false;

        if (flower.plantCost > 0 && !currency.Spend(CurrencyType.Petals, flower.plantCost))
            return false;

        return plots[plotIndex].Plant(flower);
    }

    /// <summary>
    /// Plant in the first available empty plot.
    /// </summary>
    public bool PlantFlowerInFirstEmpty(FlowerData flower)
    {
        for (int i = 0; i < plots.Count; i++)
        {
            if (plots[i].State == PlotState.Empty)
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
            if (plot.State == PlotState.Bloomed)
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
            plot.ApplyOfflineTime(seconds);
        }

        // If auto-harvest is unlocked, simulate harvest cycles
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

    FlowerData FindFlower(string assetName)
    {
        foreach (var flower in availableFlowers)
        {
            if (flower.name == assetName) return flower;
        }
        return null;
    }
}