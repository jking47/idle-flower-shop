using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One line item in an order: a flower type and how many are needed.
/// </summary>
[System.Serializable]
public class OrderRequirement
{
    public FlowerData flower;
    [Min(1)] public int count = 1;
}

/// <summary>
/// ScriptableObject template for a customer order.
/// Create via: Assets > Create > FlowerShop > Order Data
/// 
/// ShopManager picks from a pool of these at runtime to generate
/// active orders. Vary minShopLevel to gate complex orders behind progression.
/// </summary>
[CreateAssetMenu(fileName = "NewOrder", menuName = "FlowerShop/Order Data")]
public class OrderData : ScriptableObject
{
    [Header("Display")]
    public string displayName = "Bouquet Order";

    [Header("Requirements")]
    public List<OrderRequirement> requirements = new();

    [Header("Reward")]
    [Tooltip("Base coin payout. Scaled by UpgradeManager.GetSellValueMultiplier() at runtime.")]
    public double baseCoinReward = 50;

    [Header("Timing")]
    [Tooltip("Seconds the player has to fill this order before it expires.")]
    public float timeLimit = 90f;

    [Header("Availability")]
    [Tooltip("Minimum shop upgrade level before this order can appear. 0 = always available.")]
    public int minShopLevel = 0;
}
