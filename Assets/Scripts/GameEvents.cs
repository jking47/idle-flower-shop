/// <summary>
/// All cross-system event definitions. Struct-based for zero allocation.
/// </summary>

public enum CurrencyType { Petals, Coins, Renown, Gems }

public struct CurrencyChangedEvent
{
    public CurrencyType currencyType;
    public double previousAmount;
    public double newAmount;
}

public struct FlowerPlantedEvent
{
    public FlowerData flowerData;
    public int plotIndex;
}

public struct FlowerHarvestedEvent
{
    public FlowerData flowerData;
    public double yield;
}

public struct FlowerBloomedEvent
{
    public FlowerData flowerData;
    public int plotIndex;
}

public struct UpgradePurchasedEvent
{
    public string upgradeId;
    public int newLevel;
}

public struct PhaseUnlockedEvent
{
    public GamePhase phase;
}

public enum GamePhase { Patch, Garden, Shop, Business }