using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks upgrade levels and provides multiplier queries for other systems.
/// Attach to the GameManager object.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    [Header("Available Upgrades")]
    [SerializeField] List<UpgradeData> upgrades = new();

    // Runtime state: upgrade name -> current level
    readonly Dictionary<string, int> levels = new();

    public IReadOnlyList<UpgradeData> Upgrades => upgrades;

    void Awake()
    {
        Services.Register(this);

        foreach (var upgrade in upgrades)
        {
            levels[upgrade.name] = 0;
        }
    }

    public int GetLevel(UpgradeData upgrade) =>
        levels.TryGetValue(upgrade.name, out int level) ? level : 0;

    public double GetNextCost(UpgradeData upgrade) =>
        upgrade.GetCost(GetLevel(upgrade));

    public bool IsMaxed(UpgradeData upgrade) =>
        GetLevel(upgrade) >= upgrade.maxLevel;

    /// <summary>
    /// Check if all prerequisites for an upgrade are met (each at level >= 1).
    /// </summary>
    public bool PrerequisitesMet(UpgradeData upgrade)
    {
        if (upgrade.prerequisites == null || upgrade.prerequisites.Length == 0)
            return true;

        foreach (var prereq in upgrade.prerequisites)
        {
            if (prereq == null) continue;
            if (GetLevel(prereq) < 1)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Get the first unmet prerequisite name for display purposes.
    /// Returns null if all prerequisites are met.
    /// </summary>
    public string GetFirstUnmetPrerequisite(UpgradeData upgrade)
    {
        if (upgrade.prerequisites == null) return null;

        foreach (var prereq in upgrade.prerequisites)
        {
            if (prereq == null) continue;
            if (GetLevel(prereq) < 1)
                return prereq.displayName;
        }
        return null;
    }

    /// <summary>
    /// Attempt to purchase one level of an upgrade. Returns true if successful.
    /// </summary>
    public bool Purchase(UpgradeData upgrade)
    {
        if (!PrerequisitesMet(upgrade)) return false;

        int current = GetLevel(upgrade);
        if (current >= upgrade.maxLevel) return false;

        double cost = upgrade.GetCost(current);
        var currency = Services.Get<CurrencyManager>();

        if (!currency.Spend(upgrade.costCurrency, cost)) return false;

        levels[upgrade.name] = current + 1;

        ApplyImmediate(upgrade, current + 1);

        EventBus.Publish(new UpgradePurchasedEvent
        {
            upgradeId = upgrade.name,
            newLevel = current + 1
        });

        return true;
    }

    void ApplyImmediate(UpgradeData upgrade, int newLevel)
    {
        switch (upgrade.upgradeType)
        {
            case UpgradeType.AutoHarvest:
                if (newLevel == 1)
                    Services.Get<GardenManager>()?.UnlockAutoHarvest();
                break;

            case UpgradeType.WateringCan:
                if (newLevel == 1)
                    Services.Get<WateringCan>()?.Unlock();
                break;

            case UpgradeType.AutoPlant:
                if (newLevel == 1)
                    Services.Get<GardenManager>()?.UnlockAutoPlant();
                break;

            case UpgradeType.PlotUnlock:
                break;
        }
    }

    // --- Multiplier queries used by other systems ---

    public double GetYieldMultiplier()
    {
        return 1.0 + GetTotalEffect(UpgradeType.YieldMultiplier);
    }

    public double GetGrowSpeedMultiplier()
    {
        float bonus = GetTotalEffect(UpgradeType.GrowSpeedMultiplier);
        return 1.0 / (1.0 + bonus);
    }

    public double GetSellValueMultiplier()
    {
        return 1.0 + GetTotalEffect(UpgradeType.SellValueMultiplier);
    }

    public float GetWaterCapacityBonus()
    {
        return GetTotalEffect(UpgradeType.WaterCapacity);
    }

    public int GetBonusOrderSlots()
    {
        return Mathf.RoundToInt(GetTotalEffect(UpgradeType.OrderSlots));
    }

    float GetTotalEffect(UpgradeType type)
    {
        float total = 0f;
        foreach (var upgrade in upgrades)
        {
            if (upgrade.upgradeType == type)
                total += upgrade.GetEffect(GetLevel(upgrade));
        }
        return total;
    }

    // --- Save/Load support ---

    public Dictionary<string, int> GetSaveData()
    {
        return new Dictionary<string, int>(levels);
    }

    public void LoadSaveData(Dictionary<string, int> data)
    {
        foreach (var kvp in data)
        {
            if (levels.ContainsKey(kvp.Key))
            {
                levels[kvp.Key] = kvp.Value;
            }
        }

        foreach (var upgrade in upgrades)
        {
            int level = GetLevel(upgrade);
            if (level > 0)
                ApplyImmediate(upgrade, level);
        }
    }
}