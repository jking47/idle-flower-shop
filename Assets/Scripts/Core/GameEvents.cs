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
    public int plotIndex;
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

/// <summary>Fired by FlowerSpriteInitializer once all procedural sprites are ready.</summary>
public struct SpritesInitializedEvent { }

// --- Pest Events ---

public struct PestEventStartedEvent
{
    public GamePhase phase;
    public int pestCount;
}

public struct PestEventEndedEvent
{
    public int repelled;    // pests chased off by the player
    public int reached;     // pests that made it to a plot
}

public struct PestReachedPlotEvent
{
    public int plotIndex;
    public float yieldPenaltyPercent;   // 0–1, e.g. 0.25 = 25% penalty
}

public struct PestRepeledEvent
{
    public int plotIndex;   // which plot the pest was targeting
}

// --- Shop / Market Events ---

/// <summary>Fired whenever demand scores shift (periodic or after sell pressure).</summary>
public struct MarketUpdatedEvent { }

/// <summary>Fired when a new order appears in a slot.</summary>
public struct OrderSpawnedEvent
{
    public int slotIndex;
}

/// <summary>Fired when the player successfully fills an order.</summary>
public struct OrderFilledEvent
{
    public int slotIndex;
    public double coinsEarned;
    public string orderName;
}

/// <summary>Fired when an order timer runs out before being filled.</summary>
public struct OrderExpiredEvent
{
    public int slotIndex;
}

/// <summary>Fired whenever a flower type's inventory count changes.</summary>
public struct InventoryChangedEvent
{
    public FlowerData flower;
    public int newCount;
}

// --- Garden / Plot Events ---

public struct PlotSelectedEvent
{
    public int plotIndex;
}

public struct FlowerTypeUnlockedEvent
{
    public string flowerName;
}