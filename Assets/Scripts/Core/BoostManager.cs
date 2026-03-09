using UnityEngine;

/// <summary>
/// Manages timed boosts purchased with gems.
/// Attach to GameManager object.
/// </summary>
public class BoostManager : MonoBehaviour
{
    [Header("Sunshine Boost")]
    [SerializeField] double boostMultiplier = 2.0;
    [SerializeField] float boostDurationSeconds = 600f; // 10 minutes
    [SerializeField] int boostCostGems = 25;

    [Header("Instant Bloom")]
    [SerializeField] int instantBloomCostGems = 5;

    float boostTimeRemaining;

    public bool IsBoostActive => boostTimeRemaining > 0;
    public float BoostTimeRemaining => boostTimeRemaining;
    public double BoostMultiplier => IsBoostActive ? boostMultiplier : 1.0;
    public int BoostCostGems => boostCostGems;
    public int InstantBloomCostGems => instantBloomCostGems;

    void Awake()
    {
        Services.Register(this);
    }

    void Update()
    {
        if (boostTimeRemaining > 0)
        {
            boostTimeRemaining -= Time.deltaTime;
            if (boostTimeRemaining <= 0)
            {
                boostTimeRemaining = 0;
                Debug.Log("[Boost] Sunshine Boost expired.");
            }
        }
    }

    /// <summary>
    /// Activate the sunshine boost. Returns true if purchased successfully.
    /// </summary>
    public bool ActivateSunshineBoost()
    {
        var currency = Services.Get<CurrencyManager>();
        if (currency == null) return false;

        if (!currency.Spend(CurrencyType.Gems, boostCostGems))
            return false;

        boostTimeRemaining = boostDurationSeconds;
        Debug.Log($"[Boost] Sunshine Boost active for {boostDurationSeconds}s ({boostMultiplier}x harvest)");
        return true;
    }

    /// <summary>
    /// Restore boost timer from save data.
    /// </summary>
    public void LoadSaveData(float remainingSeconds)
    {
        boostTimeRemaining = Mathf.Max(0f, remainingSeconds);
    }

    /// <summary>
    /// Instantly bloom a growing flower. Returns true if purchased successfully.
    /// </summary>
    public bool InstantBloom(FlowerBed plot)
    {
        if (plot.State != PlotState.Growing) return false;

        var currency = Services.Get<CurrencyManager>();
        if (currency == null) return false;

        if (!currency.Spend(CurrencyType.Gems, instantBloomCostGems))
            return false;

        plot.ForceBloom();
        Debug.Log("[Boost] Instant bloom purchased.");
        return true;
    }
}
