using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop UI panel. Implements IPanel for PanelManager (one panel open at a time).
/// Only shows slots up to ShopManager.MaxActiveOrders. Hidden slots become visible
/// when the Order Slots upgrade is purchased.
///
/// Grid layout is configured at runtime from orderSlots[0].transform.parent — no
/// inspector assignment needed for the container. Cell size and column count are
/// tunable here without touching the scene or prefab.
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

    [Header("Grid Layout")]
    [Tooltip("Width and height of each order card in the grid. Adjust for your screen size.")]
    [SerializeField] Vector2 gridCellSize = new Vector2(400f, 340f);

    [Tooltip("Number of order card columns in the grid.")]
    [SerializeField] int gridColumns = 2;

    ShopManager shop;

    void Awake()
    {
        Services.Register(this);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        gameObject.AddComponent<PanelTransition>();
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
        ConfigureOrdersGrid();
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

        // Re-check on every open in case phase advanced while panel was closed
        bool unlocked = GameManager.Instance != null &&
                        GameManager.Instance.CurrentPhase >= GamePhase.Shop;
        lockedOverlay?.SetActive(!unlocked);

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

    // --- Grid setup ---

    /// <summary>
    /// Replaces the scene's VerticalLayoutGroup on the order container with a
    /// GridLayoutGroup. Called once in Start — container is inferred from the
    /// first slot's parent so no extra inspector assignment is needed.
    /// </summary>
    void ConfigureOrdersGrid()
    {
        if (orderSlots == null || orderSlots.Length == 0 || orderSlots[0] == null) return;

        var container = orderSlots[0].transform.parent;
        if (container == null) return;

        // Disable the existing VLG so it doesn't fight the GLG
        var vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;

        // Add (or reuse) a GridLayoutGroup
        var glg = container.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = container.gameObject.AddComponent<GridLayoutGroup>();

        glg.cellSize        = gridCellSize;
        glg.spacing         = new Vector2(8f, 8f);
        glg.padding         = new RectOffset(8, 8, 8, 8);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = gridColumns;
        glg.childAlignment  = TextAnchor.UpperLeft;
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
