using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows current flower inventory on the HUD as compact icon + count pairs.
/// Attach to an empty container GameObject under HUD.
/// Creates its own HorizontalLayoutGroup and spawns entries programmatically.
/// </summary>
public class InventoryDisplay : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] float iconSize = 32f;
    [SerializeField] float spacing = 8f;
    [SerializeField] float entrySpacing = 14f;
    [SerializeField] int fontSize = 18;

    readonly Dictionary<string, InventoryEntry> entries = new();

    struct InventoryEntry
    {
        public GameObject root;
        public Image icon;
        public TMP_Text countText;
    }

    void Awake()
    {
        // Set up horizontal layout on this container
        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
            hlg = gameObject.AddComponent<HorizontalLayoutGroup>();

        hlg.spacing = entrySpacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Content size fitter so it wraps content
        var csf = GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void OnEnable()
    {
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        Refresh();
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    void OnInventoryChanged(InventoryChangedEvent evt) => Refresh();

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

            // Set icon sprite if not yet assigned
            if (entry.icon.sprite == null)
            {
                var sprite = FlowerSpriteInitializer.GetStageSprite(kvp.Key, 1f);
                if (sprite != null)
                    entry.icon.sprite = sprite;
            }
        }
    }

    void CreateEntry(string flowerName)
    {
        // Entry container
        var entryGo = new GameObject($"Inv_{flowerName}");
        var entryRt = entryGo.AddComponent<RectTransform>();
        entryRt.SetParent(transform, false);

        var entryHlg = entryGo.AddComponent<HorizontalLayoutGroup>();
        entryHlg.spacing = spacing;
        entryHlg.childAlignment = TextAnchor.MiddleLeft;
        entryHlg.childForceExpandWidth = false;
        entryHlg.childForceExpandHeight = false;
        entryHlg.childControlWidth = false;
        entryHlg.childControlHeight = false;

        // Flower icon
        var iconGo = new GameObject("Icon");
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.SetParent(entryRt, false);
        iconRt.sizeDelta = new Vector2(iconSize, iconSize);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        // Try to get sprite immediately
        var sprite = FlowerSpriteInitializer.GetStageSprite(flowerName, 1f);
        if (sprite != null)
            iconImg.sprite = sprite;

        // Count text
        var textGo = new GameObject("Count");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(entryRt, false);
        textRt.sizeDelta = new Vector2(40, iconSize);

        var countTmp = textGo.AddComponent<TextMeshProUGUI>();
        countTmp.fontSize = fontSize;
        countTmp.fontStyle = FontStyles.Bold;
        countTmp.color = new Color(0.9f, 0.92f, 0.85f);
        countTmp.alignment = TextAlignmentOptions.MidlineLeft;
        countTmp.enableWordWrapping = false;
        countTmp.overflowMode = TextOverflowModes.Overflow;

        entries[flowerName] = new InventoryEntry
        {
            root = entryGo,
            icon = iconImg,
            countText = countTmp
        };
    }

    string FormatCount(int count)
    {
        if (count < 1000) return count.ToString();
        return (count / 1000f).ToString("F1") + "K";
    }
}