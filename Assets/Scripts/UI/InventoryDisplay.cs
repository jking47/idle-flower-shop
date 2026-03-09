using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows current flower inventory on the HUD as compact icon + count pairs,
/// with a sell price and demand label below each entry that updates live with
/// market shifts.
///
/// Attach to an empty container GameObject under HUD.
/// Creates its own GridLayoutGroup and spawns entries programmatically.
/// </summary>
public class InventoryDisplay : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] float iconSize = 32f;
    [SerializeField] float spacing = 8f;
    [SerializeField] float entrySpacing = 14f;
    [SerializeField] int fontSize = 18;

    readonly Dictionary<string, InventoryEntry> entries = new();

    // Lazy-built lookup from flower name → FlowerData, used for price queries
    Dictionary<string, FlowerData> flowerLookup;

    struct InventoryEntry
    {
        public GameObject root;
        public Image icon;
        public TMP_Text countText;
        public TMP_Text priceText;
        public TMP_Text demandText;
    }

    void Awake()
    {
        // Grid layout — max 4 columns, wraps into additional rows automatically.
        // Cell height is increased to accommodate the price/demand row below the icon.
        var glg = GetComponent<GridLayoutGroup>();
        if (glg == null)
            glg = gameObject.AddComponent<GridLayoutGroup>();

        glg.cellSize        = new Vector2(iconSize + spacing + 40f, iconSize + 22f);
        glg.spacing         = new Vector2(entrySpacing, 6f);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;
        glg.childAlignment  = TextAnchor.UpperLeft;

        // Expand vertically to fit rows; width is fixed by the column count
        var csf = GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
    }

    void OnEnable()
    {
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        EventBus.Subscribe<SpritesInitializedEvent>(OnSpritesInitialized);
        EventBus.Subscribe<MarketUpdatedEvent>(OnMarketUpdated);
        Refresh();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        EventBus.Unsubscribe<SpritesInitializedEvent>(OnSpritesInitialized);
        EventBus.Unsubscribe<MarketUpdatedEvent>(OnMarketUpdated);
    }

    void OnInventoryChanged(InventoryChangedEvent evt) => Refresh();
    void OnSpritesInitialized(SpritesInitializedEvent evt) => Refresh();
    void OnMarketUpdated(MarketUpdatedEvent evt) => RefreshPrices();

    void Refresh()
    {
        if (!Services.TryGet<InventoryManager>(out var inv)) return;

        var stock = inv.GetAllStock();

        // Hide entries for flowers no longer in stock
        foreach (var kvp in entries)
        {
            bool inStock = stock.ContainsKey(kvp.Key) && stock[kvp.Key] > 0;
            kvp.Value.root.SetActive(inStock);
        }

        if (stock.Count == 0) return;

        foreach (var kvp in stock)
        {
            if (kvp.Value <= 0) continue;

            if (!entries.ContainsKey(kvp.Key))
                CreateEntry(kvp.Key);

            var entry = entries[kvp.Key];
            entry.root.SetActive(true);
            entry.countText.text = FormatCount(kvp.Value);

            // Always re-fetch sprite — may have been null on first Refresh before sprites were generated
            var sprite = FlowerSpriteInitializer.GetStageSprite(kvp.Key, 1f);
            if (sprite != null)
                entry.icon.sprite = sprite;
        }

        RefreshPrices();
    }

    /// <summary>
    /// Updates sell price and demand label on all visible entries.
    /// Called on inventory refresh and whenever the market shifts.
    /// </summary>
    void RefreshPrices()
    {
        if (!Services.TryGet<MarketManager>(out var market)) return;

        foreach (var kvp in entries)
        {
            if (!kvp.Value.root.activeSelf) continue;

            var flower = GetFlowerData(kvp.Key);
            if (flower == null) continue;

            double price = market.GetSellPrice(flower);
            kvp.Value.priceText.text = $"{price:0}c";

            kvp.Value.demandText.text  = market.GetDemandLabel(kvp.Key);
            kvp.Value.demandText.color = market.GetDemandColor(kvp.Key);
        }
    }

    void CreateEntry(string flowerName)
    {
        // Root: vertical stack — top row (icon + count) then market row (price + demand)
        var entryGo = new GameObject($"Inv_{flowerName}");
        var entryRt = entryGo.AddComponent<RectTransform>();
        entryRt.SetParent(transform, false);

        var vlg = entryGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 2f;
        vlg.childAlignment       = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = true;

        // --- Top row: icon + count ---
        var topRow = new GameObject("TopRow");
        var topRt = topRow.AddComponent<RectTransform>();
        topRt.SetParent(entryRt, false);

        var hlg = topRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = spacing;
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth    = false;
        hlg.childControlHeight   = false;

        // Fix top row height to the icon size
        var topLe = topRow.AddComponent<LayoutElement>();
        topLe.preferredHeight = iconSize;

        // Flower icon
        var iconGo = new GameObject("Icon");
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.SetParent(topRt, false);
        iconRt.sizeDelta = new Vector2(iconSize, iconSize);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        var sprite = FlowerSpriteInitializer.GetStageSprite(flowerName, 1f);
        if (sprite != null)
            iconImg.sprite = sprite;

        // Count text
        var countGo = new GameObject("Count");
        var countRt = countGo.AddComponent<RectTransform>();
        countRt.SetParent(topRt, false);
        countRt.sizeDelta = new Vector2(40, iconSize);

        var countTmp = countGo.AddComponent<TextMeshProUGUI>();
        countTmp.fontSize           = fontSize;
        countTmp.fontStyle          = FontStyles.Bold;
        countTmp.color              = new Color(0.9f, 0.92f, 0.85f);
        countTmp.alignment          = TextAlignmentOptions.MidlineLeft;
        countTmp.enableWordWrapping = false;
        countTmp.overflowMode       = TextOverflowModes.Overflow;

        // --- Market row: price + demand label ---
        var marketRow = new GameObject("MarketRow");
        var marketRt = marketRow.AddComponent<RectTransform>();
        marketRt.SetParent(entryRt, false);

        var mhlg = marketRow.AddComponent<HorizontalLayoutGroup>();
        mhlg.spacing              = 4f;
        mhlg.childAlignment       = TextAnchor.MiddleLeft;
        mhlg.childForceExpandWidth  = false;
        mhlg.childForceExpandHeight = false;
        mhlg.childControlWidth    = false;
        mhlg.childControlHeight   = false;

        int smallFont = Mathf.Max(fontSize - 5, 10);

        // Sell price (e.g. "3c")
        var priceGo = new GameObject("Price");
        var priceRt = priceGo.AddComponent<RectTransform>();
        priceRt.SetParent(marketRt, false);
        priceRt.sizeDelta = new Vector2(30, 16);

        var priceTmp = priceGo.AddComponent<TextMeshProUGUI>();
        priceTmp.fontSize           = smallFont;
        priceTmp.color              = new Color(0.85f, 0.85f, 0.7f);
        priceTmp.alignment          = TextAlignmentOptions.MidlineLeft;
        priceTmp.enableWordWrapping = false;
        priceTmp.overflowMode       = TextOverflowModes.Overflow;

        // Demand label (e.g. "High" in yellow)
        var demandGo = new GameObject("Demand");
        var demandRt = demandGo.AddComponent<RectTransform>();
        demandRt.SetParent(marketRt, false);
        demandRt.sizeDelta = new Vector2(44, 16);

        var demandTmp = demandGo.AddComponent<TextMeshProUGUI>();
        demandTmp.fontSize           = smallFont;
        demandTmp.fontStyle          = FontStyles.Bold;
        demandTmp.alignment          = TextAlignmentOptions.MidlineLeft;
        demandTmp.enableWordWrapping = false;
        demandTmp.overflowMode       = TextOverflowModes.Overflow;

        entries[flowerName] = new InventoryEntry
        {
            root       = entryGo,
            icon       = iconImg,
            countText  = countTmp,
            priceText  = priceTmp,
            demandText = demandTmp
        };
    }

    /// <summary>
    /// Lazy-init lookup from flower asset name to FlowerData.
    /// GardenManager is guaranteed available by the time any flower is in inventory.
    /// </summary>
    FlowerData GetFlowerData(string flowerName)
    {
        if (flowerLookup == null)
        {
            flowerLookup = new Dictionary<string, FlowerData>();
            if (Services.TryGet<GardenManager>(out var garden))
            {
                foreach (var f in garden.AvailableFlowers)
                    flowerLookup[f.name] = f;
            }
        }
        flowerLookup.TryGetValue(flowerName, out var result);
        return result;
    }

    string FormatCount(int count)
    {
        if (count < 1000) return count.ToString();
        return (count / 1000f).ToString("F1") + "K";
    }
}
