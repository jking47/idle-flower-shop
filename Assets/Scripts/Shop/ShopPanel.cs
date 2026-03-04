using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop UI panel. Implements IPanel for PanelManager (one panel open at a time).
/// Only shows slots up to ShopManager.MaxActiveOrders. Hidden slots become visible
/// when the Order Slots upgrade is purchased.
/// </summary>
public class ShopPanel : MonoBehaviour, IPanel
{
    [Header("Panel Controls")]
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;

    [Header("Order Slots")]
    [Tooltip("One ShopOrderUI per slot. Must match ShopManager.absoluteMaxSlots.")]
    [SerializeField] ShopOrderUI[] orderSlots;

    [Header("Phase Gate")]
    [SerializeField] GameObject lockedOverlay;

    ShopManager shop;

    void Awake()
    {
        Services.Register(this);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        gameObject.SetActive(false);
    }

    void Start()
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Register(this);

        shop = Services.Get<ShopManager>();

        bool unlocked = GameManager.Instance != null &&
                        GameManager.Instance.CurrentPhase >= GamePhase.Shop;
        lockedOverlay?.SetActive(!unlocked);
        RefreshAllSlots();
    }

    void OnEnable()
    {
        EventBus.Subscribe<OrderSpawnedEvent>(OnOrderSpawned);
        EventBus.Subscribe<OrderFilledEvent>(OnOrderFilled);
        EventBus.Subscribe<OrderExpiredEvent>(OnOrderExpired);
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<OrderSpawnedEvent>(OnOrderSpawned);
        EventBus.Unsubscribe<OrderFilledEvent>(OnOrderFilled);
        EventBus.Unsubscribe<OrderExpiredEvent>(OnOrderExpired);
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
    }

    // --- IPanel ---

    public void Open()
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Open(this);

        gameObject.SetActive(true);
        RefreshAllSlots();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // --- Event handlers ---

    void OnOrderSpawned(OrderSpawnedEvent e) => RefreshSlot(e.slotIndex);
    void OnOrderFilled(OrderFilledEvent e) => RefreshSlot(e.slotIndex);
    void OnOrderExpired(OrderExpiredEvent e) => RefreshSlot(e.slotIndex);

    void OnInventoryChanged(InventoryChangedEvent e)
    {
        if (shop == null) return;
        for (int i = 0; i < orderSlots.Length && i < shop.MaxActiveOrders; i++)
            orderSlots[i].RefreshFillButton();
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        if (evt.phase == GamePhase.Shop)
            lockedOverlay?.SetActive(false);
    }

    void OnUpgradePurchased(UpgradePurchasedEvent evt)
    {
        // Slot count may have changed — refresh visibility
        RefreshAllSlots();
    }

    // --- Helpers ---

    void RefreshSlot(int idx)
    {
        if (shop == null || idx < 0 || idx >= orderSlots.Length) return;
        orderSlots[idx].Bind(idx, shop.Slots[idx]);
    }

    void RefreshAllSlots()
    {
        if (shop == null) return;

        int activeCount = shop.MaxActiveOrders;

        for (int i = 0; i < orderSlots.Length; i++)
        {
            if (i < activeCount)
            {
                // Show this slot
                orderSlots[i].gameObject.SetActive(true);
                var order = i < shop.Slots.Count ? shop.Slots[i] : null;
                orderSlots[i].Bind(i, order);
            }
            else
            {
                // Hide slots beyond current max
                orderSlots[i].gameObject.SetActive(false);
            }
        }
    }
}