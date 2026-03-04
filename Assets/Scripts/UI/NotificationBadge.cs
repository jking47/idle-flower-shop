using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BadgeType
{
    Upgrades,   // count of affordable upgrades
    Shop,       // count of fillable orders
    Inventory   // shows when inventory has flowers (subtle reminder to sell)
}

/// <summary>
/// Self-building red notification badge. Attach to any HUD button.
/// Set badgeType in inspector to control what it tracks.
/// Builds its own red circle + count text programmatically.
/// </summary>
public class NotificationBadge : MonoBehaviour
{
    [SerializeField] BadgeType badgeType;

    [Header("Appearance")]
    [SerializeField] float badgeSize = 24f;
    [SerializeField] Vector2 offset = new Vector2(12f, 12f);
    [SerializeField] Color badgeColor = new Color(0.9f, 0.2f, 0.2f);

    GameObject badgeObj;
    TMP_Text countText;
    Image badgeImage;

    void Awake()
    {
        BuildBadge();
    }

    void OnEnable()
    {
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
        EventBus.Subscribe<OrderSpawnedEvent>(OnOrderSpawned);
        EventBus.Subscribe<OrderFilledEvent>(OnOrderFilled);
        EventBus.Subscribe<OrderExpiredEvent>(OnOrderExpired);
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
        EventBus.Unsubscribe<OrderSpawnedEvent>(OnOrderSpawned);
        EventBus.Unsubscribe<OrderFilledEvent>(OnOrderFilled);
        EventBus.Unsubscribe<OrderExpiredEvent>(OnOrderExpired);
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void Start()
    {
        Refresh();
    }

    // --- Event handlers — all just trigger a refresh ---

    void OnCurrencyChanged(CurrencyChangedEvent e) => Refresh();
    void OnUpgradePurchased(UpgradePurchasedEvent e) => Refresh();
    void OnOrderSpawned(OrderSpawnedEvent e) => Refresh();
    void OnOrderFilled(OrderFilledEvent e) => Refresh();
    void OnOrderExpired(OrderExpiredEvent e) => Refresh();
    void OnInventoryChanged(InventoryChangedEvent e) => Refresh();
    void OnPhaseUnlocked(PhaseUnlockedEvent e) => Refresh();

    // --- Badge Logic ---

    void Refresh()
    {
        int count = badgeType switch
        {
            BadgeType.Upgrades => CountAffordableUpgrades(),
            BadgeType.Shop => CountFillableOrders(),
            BadgeType.Inventory => CountInventoryFlowers(),
            _ => 0
        };

        bool show = count > 0;
        badgeObj.SetActive(show);

        if (show && countText != null)
            countText.text = count > 99 ? "99+" : count.ToString();
    }

    int CountAffordableUpgrades()
    {
        if (!Services.TryGet<UpgradeManager>(out var upgrades)) return 0;
        if (!Services.TryGet<CurrencyManager>(out var currency)) return 0;

        int count = 0;
        foreach (var upgrade in upgrades.Upgrades)
        {
            if (upgrades.IsMaxed(upgrade)) continue;
            if (!upgrades.PrerequisitesMet(upgrade)) continue;

            // Phase gate
            if (Services.TryGet<GameManager>(out var gm) &&
                upgrade.requiredPhase > gm.CurrentPhase) continue;

            double cost = upgrades.GetNextCost(upgrade);
            if (currency.CanAfford(upgrade.costCurrency, cost))
                count++;
        }
        return count;
    }

    int CountFillableOrders()
    {
        if (!Services.TryGet<ShopManager>(out var shop)) return 0;
        if (!Services.TryGet<InventoryManager>(out var inv)) return 0;

        int count = 0;
        foreach (var order in shop.Slots)
        {
            if (order == null || order.data == null) continue;

            bool canFill = true;
            foreach (var req in order.data.requirements)
            {
                if (req.flower == null || inv.GetCount(req.flower.name) < req.count)
                {
                    canFill = false;
                    break;
                }
            }
            if (canFill) count++;
        }
        return count;
    }

    int CountInventoryFlowers()
    {
        if (!Services.TryGet<InventoryManager>(out var inv)) return 0;

        int total = 0;
        foreach (var kvp in inv.GetAllStock())
            total += kvp.Value;
        return total > 0 ? 1 : 0; // just show dot, not count
    }

    // --- Build UI ---

    void BuildBadge()
    {
        badgeObj = new GameObject("Badge");
        var rt = badgeObj.AddComponent<RectTransform>();
        rt.SetParent(transform, false);

        // Position in top-right corner of the button
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(badgeSize, badgeSize);

        // Red circle background
        badgeImage = badgeObj.AddComponent<Image>();
        badgeImage.color = badgeColor;
        badgeImage.raycastTarget = false;

        var outline = badgeObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.1f, 0.1f);
        outline.effectDistance = new Vector2(1, -1);

        // Count text
        var textGo = new GameObject("Count");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        countText = textGo.AddComponent<TextMeshProUGUI>();
        countText.alignment = TextAlignmentOptions.Center;
        countText.fontSize = 14;
        countText.fontStyle = FontStyles.Bold;
        countText.color = Color.white;
        countText.enableWordWrapping = false;
        countText.raycastTarget = false;

        badgeObj.SetActive(false);
    }
}