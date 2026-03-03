using UnityEngine;

public enum FlowerRarity { Common, Uncommon, Rare, Exotic }

/// <summary>
/// Data definition for a flower type. Create instances via
/// Assets > Create > FlowerShop > Flower Data.
/// </summary>
[CreateAssetMenu(fileName = "NewFlower", menuName = "FlowerShop/Flower Data")]
public class FlowerData : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    [TextArea] public string description;
    public FlowerRarity rarity;
    public Sprite icon;

    [Header("Growth")]
    [Tooltip("Seconds from planting to bloom")]
    public float growTime = 10f;

    [Tooltip("Base petals per harvest before multipliers")]
    public double baseYield = 1;

    [Header("Cost")]
    [Tooltip("Petals to plant. 0 = free.")]
    public double plantCost;

    [Header("Shop Value")]
    [Tooltip("Base coin value when sold in arrangements")]
    public double baseSellValue = 1;

    [Header("Unlock")]
    [Tooltip("Minimum game phase required")]
    public GamePhase requiredPhase = GamePhase.Patch;

    [Tooltip("Petals spent to unlock this flower type (-1 = available from start)")]
    public double unlockCost = -1;

    [Tooltip("Only obtainable through friend gifts")]
    public bool socialExclusive;
}