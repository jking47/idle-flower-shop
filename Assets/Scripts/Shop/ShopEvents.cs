/// Fired whenever demand scores shift (periodic or after sell pressure).
public struct MarketUpdatedEvent { }

/// Fired when a new order appears in a slot.
public struct OrderSpawnedEvent
{
    public int slotIndex;
}

/// Fired when the player successfully fills an order.
public struct OrderFilledEvent
{
    public int slotIndex;
    public double coinsEarned;
    public string orderName;
}

/// Fired when an order timer runs out before being filled.
public struct OrderExpiredEvent
{
    public int slotIndex;
}

/// Fired whenever a flower type's inventory count changes.
public struct InventoryChangedEvent
{
    public FlowerData flower;
    public int newCount;
}