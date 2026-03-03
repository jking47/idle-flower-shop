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
    /// Attempt to purchase one level of an upgrade. Returns true if successful.
    /// </summary>
    public bool Purchase(UpgradeData upgrade)
    {
        int current = GetLevel(upgrade);
        if (current >= upgrade.maxLevel) return false;

        double cost = upgrade.GetCost(current);
        var currency = Services.Get<CurrencyManager>();

        if (!currency.Spend(upgrade.costCurrency, cost)) return false;

        levels[upgrade.name] = current + 1;

        // Apply any immediate one-time effects
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

            case UpgradeType.PlotUnlock:
                // Handled by listening to UpgradePurchasedEvent in GardenManager
                break;
        }
    }

    // --- Multiplier queries used by other systems ---

    /// <summary>
    /// Total harvest yield multiplier. Apply as: baseYield * GetYieldMultiplier()
    /// </summary>
    public double GetYieldMultiplier()
    {
        return 1.0 + GetTotalEffect(UpgradeType.YieldMultiplier);
    }

    /// <summary>
    /// Grow time multiplier. Apply as: baseGrowTime * GetGrowSpeedMultiplier()
    /// Values less than 1 = faster growth.
    /// </summary>
    public double GetGrowSpeedMultiplier()
    {
        float bonus = GetTotalEffect(UpgradeType.GrowSpeedMultiplier);
        // Convert additive bonus to a speed divisor: 0.3 bonus = 1/1.3 = ~0.77x grow time
        return 1.0 / (1.0 + bonus);
    }

    /// <summary>
    /// Shop sell value multiplier.
    /// </summary>
    public double GetSellValueMultiplier()
    {
        return 1.0 + GetTotalEffect(UpgradeType.SellValueMultiplier);
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

        // Re-apply any immediate effects
        foreach (var upgrade in upgrades)
        {
            int level = GetLevel(upgrade);
            if (level > 0)
                ApplyImmediate(upgrade, level);
        }
    }
}