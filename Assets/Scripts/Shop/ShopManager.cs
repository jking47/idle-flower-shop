using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime state for one active customer order slot.
/// </summary>
public class ActiveOrder
{
    public OrderData data;
    public float timeRemaining;
    public int slotIndex;

    public bool IsExpired => timeRemaining <= 0;
    public float TimerProgress => data != null ? Mathf.Clamp01(timeRemaining / data.timeLimit) : 0f;
}

/// <summary>
/// Manages spawning, ticking, fulfillment, and expiry of shop orders.
/// Reward calculation integrates MarketManager so demand fluctuations
/// directly affect payout — selling Hot flowers in orders pays more.
/// 
/// Attach to GameManager object. Assign OrderData assets via inspector.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Order Pool")]
    [SerializeField] List<OrderData> orderPool = new();

    [Header("Settings")]
    [SerializeField] int baseMaxActiveOrders = 2;
    [SerializeField] float spawnIntervalSeconds = 30f;
    [SerializeField] int absoluteMaxSlots = 6;

    [Header("Reward Tuning")]
    [Tooltip("Multiplier on top of market value. Rewards bundling flowers into orders vs selling raw.")]
    [SerializeField] float orderBonusMultiplier = 1.25f;

    bool shopOpen;
    int maxActiveOrders;
    float spawnTimer;
    ActiveOrder[] slots;

    public IReadOnlyList<ActiveOrder> Slots => slots;
    public int MaxActiveOrders => maxActiveOrders;

    void Awake()
    {
        Services.Register(this);
        maxActiveOrders = baseMaxActiveOrders;
    }

    void OnEnable()
    {
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
    }

    void Start()
    {
        ApplyOrderSlotUpgrade();
        slots = new ActiveOrder[absoluteMaxSlots];

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentPhase >= GamePhase.Shop)
            OpenShop();
    }

    void Update()
    {
        if (!shopOpen || orderPool.Count == 0) return;
        TickOrders();
        TickSpawn();
    }

    // --- Core loop ---

    void TickOrders()
    {
        for (int i = 0; i < maxActiveOrders; i++)
        {
            if (slots[i] == null) continue;
            slots[i].timeRemaining -= Time.deltaTime;
            if (slots[i].timeRemaining <= 0)
                ExpireOrder(i);
        }
    }

    void TickSpawn()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnIntervalSeconds)
        {
            spawnTimer = 0f;
            TrySpawnOrder();
        }
    }

    // --- Public API ---

    /// <summary>
    /// Attempt to fill the order in the given slot.
    /// Validates inventory, consumes flowers, calculates market-adjusted reward.
    /// </summary>
    public bool TryFillOrder(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return false;
        var order = slots[slotIndex];
        if (order == null) return false;

        var inv = Services.Get<InventoryManager>();
        if (inv == null) return false;

        foreach (var req in order.data.requirements)
        {
            if (inv.GetCount(req.flower.name) < req.count)
            {
                Debug.Log($"[Shop] Can't fill '{order.data.displayName}': insufficient {req.flower.name}.");
                return false;
            }
        }

        var market = Services.Get<MarketManager>();
        foreach (var req in order.data.requirements)
        {
            inv.Remove(req.flower.name, req.count);
            market?.ApplySellPressure(req.flower.name, req.count);
        }

        double reward = CalculateReward(order, market);
        Services.Get<CurrencyManager>()?.Add(CurrencyType.Coins, reward);

        Debug.Log($"[Shop] Order '{order.data.displayName}' filled for {reward:0} coins.");

        EventBus.Publish(new OrderFilledEvent
        {
            slotIndex = slotIndex,
            coinsEarned = reward,
            orderName = order.data.displayName
        });

        slots[slotIndex] = null;
        TrySpawnOrder();
        return true;
    }

    /// <summary>
    /// Get the live market-adjusted reward for a slot. Used by UI to show current payout.
    /// </summary>
    public double GetCurrentReward(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length || slots[slotIndex] == null) return 0;
        return CalculateReward(slots[slotIndex], Services.Get<MarketManager>());
    }

    // --- Internal ---

    void ApplyOrderSlotUpgrade()
    {
        int bonus = 0;
        if (Services.TryGet<UpgradeManager>(out var upgrades))
            bonus = upgrades.GetBonusOrderSlots();
        maxActiveOrders = Mathf.Clamp(baseMaxActiveOrders + bonus, 1, absoluteMaxSlots);
    }

    void OpenShop()
    {
        shopOpen = true;
        TrySpawnOrder();
        Debug.Log("[Shop] Shop is open.");
    }

    void TrySpawnOrder()
    {
        int slot = FindEmptySlot();
        if (slot < 0) return;

        var data = PickOrder();
        if (data == null) return;

        slots[slot] = new ActiveOrder
        {
            data = data,
            timeRemaining = data.timeLimit,
            slotIndex = slot
        };

        EventBus.Publish(new OrderSpawnedEvent { slotIndex = slot });
    }

    void ExpireOrder(int slotIndex)
    {
        Debug.Log($"[Shop] Order '{slots[slotIndex]?.data.displayName}' expired in slot {slotIndex}.");
        slots[slotIndex] = null;
        EventBus.Publish(new OrderExpiredEvent { slotIndex = slotIndex });
    }

    double CalculateReward(ActiveOrder order, MarketManager market)
    {
        if (market == null) return order.data.baseCoinReward;

        double sum = 0;
        foreach (var req in order.data.requirements)
            sum += req.count * market.GetSellPrice(req.flower);

        return sum * orderBonusMultiplier;
    }

    int FindEmptySlot()
    {
        for (int i = 0; i < maxActiveOrders; i++)
        {
            if (slots[i] == null) return i;
        }
        return -1;
    }

    OrderData PickOrder()
    {
        var eligible = new List<OrderData>(orderPool.Count);
        foreach (var o in orderPool)
        {
            if (o != null && o.minShopLevel <= 0)
                eligible.Add(o);
        }
        return eligible.Count > 0 ? eligible[Random.Range(0, eligible.Count)] : null;
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        if (evt.phase >= GamePhase.Shop) OpenShop();
    }

    void OnUpgradePurchased(UpgradePurchasedEvent evt)
    {
        int oldMax = maxActiveOrders;
        ApplyOrderSlotUpgrade();

        // If we gained a slot, try to fill it immediately
        if (maxActiveOrders > oldMax && shopOpen)
            TrySpawnOrder();
    }
}