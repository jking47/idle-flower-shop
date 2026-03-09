using System;
using System.Collections.Generic;
using UnityEngine;

public enum DemandLevel { Low, Normal, High, Hot }

/// <summary>
/// Manages flower market prices with fluctuating supply and demand.
/// Prices shift periodically and react to player selling patterns.
/// Attach to GameManager object.
/// </summary>
public class MarketManager : MonoBehaviour
{
    [Header("Market Settings")]
    [Tooltip("Seconds between demand shifts")]
    [SerializeField] float demandShiftInterval = 45f;

    [Tooltip("How much selling a flower reduces its demand (per unit sold)")]
    [SerializeField] float sellPressure = 0.05f;

    [Tooltip("How fast demand naturally drifts back to normal per tick")]
    [SerializeField] float demandRecoveryRate = 0.02f;

    [Header("Price Multipliers")]
    [SerializeField] float lowDemandMult = 0.5f;
    [SerializeField] float normalDemandMult = 1.0f;
    [SerializeField] float highDemandMult = 1.5f;
    [SerializeField] float hotDemandMult = 2.5f;

    // Internal demand score per flower: 0 = low, 0.5 = normal, 1 = high, 1.5+ = hot
    readonly Dictionary<string, float> demandScores = new();

    float shiftTimer;
    System.Random rng;
    bool _loaded;

    public event Action OnMarketUpdated;

    void Awake()
    {
        Services.Register(this);
        rng = new System.Random(DateTime.UtcNow.Millisecond);
    }

    void Start()
    {
        InitializeDemand();
    }

    void Update()
    {
        shiftTimer += Time.deltaTime;
        if (shiftTimer >= demandShiftInterval)
        {
            shiftTimer = 0f;
            ShiftDemand();
        }
    }

    void InitializeDemand()
    {
        // If save data has already been applied, skip random init — load always wins
        if (_loaded) return;

        var garden = Services.Get<GardenManager>();
        if (garden == null) return;

        foreach (var flower in garden.AvailableFlowers)
        {
            // Start with random demand between 0.3 and 0.8
            demandScores[flower.name] = 0.3f + (float)rng.NextDouble() * 0.5f;
        }
        OnMarketUpdated?.Invoke();
        EventBus.Publish(new MarketUpdatedEvent());
    }

    void ShiftDemand()
    {
        var keys = new List<string>(demandScores.Keys);

        // Pick 1-2 flowers to boost, 1 to reduce
        if (keys.Count == 0) return;

        // Natural drift toward normal
        foreach (var key in keys)
        {
            float score = demandScores[key];
            if (score < 0.5f)
                score += demandRecoveryRate;
            else if (score > 0.5f)
                score -= demandRecoveryRate;

            demandScores[key] = score;
        }

        // Random boost: one flower gets a demand spike
        string boosted = keys[rng.Next(keys.Count)];
        demandScores[boosted] = Mathf.Min(demandScores[boosted] + 0.2f + (float)rng.NextDouble() * 0.3f, 1.8f);

        // Random reduction: one flower (different) gets demand drop
        if (keys.Count > 1)
        {
            string reduced;
            do { reduced = keys[rng.Next(keys.Count)]; } while (reduced == boosted);
            demandScores[reduced] = Mathf.Max(demandScores[reduced] - 0.15f, 0.1f);
        }

        OnMarketUpdated?.Invoke();
        EventBus.Publish(new MarketUpdatedEvent());
    }

    /// <summary>
    /// Apply sell pressure — selling a lot of one flower reduces its demand.
    /// </summary>
    public void ApplySellPressure(string flowerName, int amountSold)
    {
        if (!demandScores.ContainsKey(flowerName)) return;

        demandScores[flowerName] -= sellPressure * amountSold;
        demandScores[flowerName] = Mathf.Max(0.1f, demandScores[flowerName]);

        OnMarketUpdated?.Invoke();
        EventBus.Publish(new MarketUpdatedEvent());
    }

    /// <summary>
    /// Get the current demand level for a flower.
    /// </summary>
    public DemandLevel GetDemandLevel(string flowerName)
    {
        float score = GetDemandScore(flowerName);
        if (score >= 1.2f) return DemandLevel.Hot;
        if (score >= 0.7f) return DemandLevel.High;
        if (score >= 0.35f) return DemandLevel.Normal;
        return DemandLevel.Low;
    }

    public float GetDemandScore(string flowerName)
    {
        return demandScores.TryGetValue(flowerName, out float score) ? score : 0.5f;
    }

    /// <summary>
    /// Get the price multiplier for the current demand level.
    /// </summary>
    public float GetPriceMultiplier(string flowerName)
    {
        return GetDemandLevel(flowerName) switch
        {
            DemandLevel.Low => lowDemandMult,
            DemandLevel.Normal => normalDemandMult,
            DemandLevel.High => highDemandMult,
            DemandLevel.Hot => hotDemandMult,
            _ => normalDemandMult
        };
    }

    /// <summary>
    /// Calculate the current sell price for a flower.
    /// </summary>
    public double GetSellPrice(FlowerData flower)
    {
        double basePrice = flower.baseSellValue;

        // Apply upgrade multiplier
        if (Services.TryGet<UpgradeManager>(out var upgrades))
            basePrice *= upgrades.GetSellValueMultiplier();

        return basePrice * GetPriceMultiplier(flower.name);
    }

    /// <summary>
    /// Get demand display info for UI.
    /// </summary>
    public string GetDemandLabel(string flowerName)
    {
        return GetDemandLevel(flowerName) switch
        {
            DemandLevel.Low => "Low",
            DemandLevel.Normal => "Normal",
            DemandLevel.High => "High",
            DemandLevel.Hot => "HOT!",
            _ => "Normal"
        };
    }

    public Color GetDemandColor(string flowerName)
    {
        return GetDemandLevel(flowerName) switch
        {
            DemandLevel.Low => new Color(0.6f, 0.6f, 0.6f),
            DemandLevel.Normal => Color.white,
            DemandLevel.High => new Color(1f, 0.85f, 0.3f),
            DemandLevel.Hot => new Color(1f, 0.3f, 0.2f),
            _ => Color.white
        };
    }

    /// <summary>
    /// Time until next demand shift, for UI countdown.
    /// </summary>
    public float TimeUntilShift => Mathf.Max(0, demandShiftInterval - shiftTimer);

    // --- Save/Load ---

    public List<DemandSaveEntry> GetSaveData()
    {
        var data = new List<DemandSaveEntry>();
        foreach (var kvp in demandScores)
        {
            data.Add(new DemandSaveEntry { flowerName = kvp.Key, score = kvp.Value });
        }
        return data;
    }

    public void LoadSaveData(List<DemandSaveEntry> data)
    {
        if (data == null) return;
        foreach (var entry in data)
            demandScores[entry.flowerName] = entry.score;
        _loaded = true;
        OnMarketUpdated?.Invoke();
        EventBus.Publish(new MarketUpdatedEvent());
    }
}

[Serializable]
public class DemandSaveEntry
{
    public string flowerName;
    public float score;
}
