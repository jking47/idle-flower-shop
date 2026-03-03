using UnityEngine;

public enum UpgradeType
{
    YieldMultiplier,    // Increases petal harvest amount
    GrowSpeedMultiplier,// Reduces grow time
    AutoHarvest,        // Unlocks/improves auto-harvest
    PlotUnlock,         // Adds a new garden plot
    SellValueMultiplier,// Increases shop coin value (Phase 3)
    WateringCan         // Unlocks the watering can tool
}

/// <summary>
/// Data definition for a purchasable upgrade.
/// Each level has an escalating cost. Create via Assets > Create > FlowerShop > Upgrade Data.
/// </summary>
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "FlowerShop/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type & Effect")]
    public UpgradeType upgradeType;
    public CurrencyType costCurrency = CurrencyType.Petals;

    [Tooltip("Value applied per level (multiplier, flat bonus, etc.)")]
    public float effectPerLevel = 0.1f;

    [Header("Leveling")]
    public int maxLevel = 10;

    [Tooltip("Cost of level 1")]
    public double baseCost = 10;

    [Tooltip("Cost multiplier per level: cost = baseCost * (costScaling ^ currentLevel)")]
    public float costScaling = 1.5f;

    [Header("Unlock")]
    public GamePhase requiredPhase = GamePhase.Patch;

    /// <summary>
    /// Cost to purchase the next level from the given current level.
    /// </summary>
    public double GetCost(int currentLevel)
    {
        if (currentLevel >= maxLevel) return -1;
        return baseCost * System.Math.Pow(costScaling, currentLevel);
    }

    /// <summary>
    /// Total effect value at a given level.
    /// For multipliers, returns the bonus (e.g., 0.3 at level 3 with 0.1 per level).
    /// Caller applies as (1 + bonus) or however the type demands.
    /// </summary>
    public float GetEffect(int level)
    {
        return effectPerLevel * level;
    }
}