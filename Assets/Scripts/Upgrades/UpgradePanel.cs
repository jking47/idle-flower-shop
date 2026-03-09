using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI panel for purchasing upgrades with tech tree prerequisite display.
/// Shows locked upgrades dimmed with "Requires: X" text.
/// </summary>
public class UpgradePanel : MonoBehaviour, IPanel
{
    [Header("References")]
    [SerializeField] Transform buttonContainer;
    [SerializeField] GameObject upgradeButtonPrefab;
    [SerializeField] Button closeButton;
    [SerializeField] Button openButton;

    [Header("Tech Tree Colors")]
    [SerializeField] Color availableColor = new Color(0.31f, 0.23f, 0.29f);
    [SerializeField] Color lockedColor = new Color(0.18f, 0.18f, 0.2f);
    [SerializeField] Color maxedColor = new Color(0.15f, 0.25f, 0.18f);
    [SerializeField] Color lockedTextColor = new Color(0.9f, 0.5f, 0.4f);

    readonly List<GameObject> spawnedButtons = new();

    UpgradeManager upgradeManager;
    CurrencyManager currency;

    void Awake()
    {
        Services.Register(this);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (openButton != null)
            openButton.onClick.AddListener(Open);

        EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgradePurchased);

        gameObject.AddComponent<PanelTransition>();
        gameObject.SetActive(false);
    }

    void Start()
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Register(this);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
    }

    public void Open()
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Open(this);

        if (upgradeManager == null) upgradeManager = Services.Get<UpgradeManager>();
        if (currency == null) currency = Services.Get<CurrencyManager>();

        gameObject.SetActive(true);
        BuildButtons();
    }

    public void Close()
    {
        foreach (var btn in spawnedButtons)
            Destroy(btn);
        spawnedButtons.Clear();

        gameObject.SetActive(false);
    }

    void OnUpgradePurchased(UpgradePurchasedEvent evt)
    {
        if (gameObject.activeSelf)
            BuildButtons();
    }

    void BuildButtons()
    {
        foreach (var btn in spawnedButtons)
            Destroy(btn);
        spawnedButtons.Clear();

        var gm = Services.Get<GameManager>();

        foreach (var upgrade in upgradeManager.Upgrades)
        {
            // Phase gate — don't show at all if phase not reached
            if (gm != null && upgrade.requiredPhase > gm.CurrentPhase)
                continue;

            var obj = Instantiate(upgradeButtonPrefab, buttonContainer);
            spawnedButtons.Add(obj);

            var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var costText = obj.transform.Find("CostText")?.GetComponent<TMP_Text>();
            var levelText = obj.transform.Find("LevelText")?.GetComponent<TMP_Text>();
            var icon = obj.transform.Find("Icon")?.GetComponent<Image>();
            var button = obj.GetComponent<Button>();
            var bgImage = obj.GetComponent<Image>();

            int level = upgradeManager.GetLevel(upgrade);
            bool maxed = upgradeManager.IsMaxed(upgrade);
            bool prereqsMet = upgradeManager.PrerequisitesMet(upgrade);

            // Icon
            if (icon != null && upgrade.icon != null)
                icon.sprite = upgrade.icon;

            // Name — append description if available
            if (nameText != null)
            {
                nameText.text = upgrade.displayName;
                if (!string.IsNullOrEmpty(upgrade.description) && !maxed && prereqsMet)
                    nameText.text += $"\n<size=20><color=#8899AA>{upgrade.description}</color></size>";
            }

            // Level text
            if (levelText != null)
                levelText.text = maxed ? "MAX" : $"Lv.{level}";

            // Locked by prerequisite
            if (!prereqsMet)
            {
                string unmet = upgradeManager.GetFirstUnmetPrerequisite(upgrade);
                if (costText != null)
                    costText.text = $"Requires: {unmet}";
                if (costText != null)
                    costText.color = lockedTextColor;

                button.interactable = false;

                if (bgImage != null)
                    bgImage.color = lockedColor;

                // Dim the icon
                if (icon != null)
                    icon.color = new Color(0.4f, 0.4f, 0.4f);

                // Dim name
                if (nameText != null)
                    nameText.color = new Color(0.5f, 0.5f, 0.55f);

                if (levelText != null)
                    levelText.color = new Color(0.5f, 0.5f, 0.55f);

                // Add lock indicator
                AddPrereqArrow(obj, upgrade);

                continue;
            }

            // Maxed out
            if (maxed)
            {
                if (costText != null)
                    costText.text = "Complete";

                button.interactable = false;

                if (bgImage != null)
                    bgImage.color = maxedColor;

                continue;
            }

            // Available — show cost and allow purchase
            if (bgImage != null)
                bgImage.color = availableColor;

            double cost = upgrade.GetCost(level);
            if (costText != null)
                costText.text = $"{cost:F0} {upgrade.costCurrency}";

            bool canAfford = currency.CanAfford(upgrade.costCurrency, cost);
            if (!canAfford)
            {
                var colors = button.colors;
                colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                button.colors = colors;
            }

            var selectedUpgrade = upgrade;
            button.onClick.AddListener(() =>
            {
                upgradeManager.Purchase(selectedUpgrade);
            });
        }
    }

    /// <summary>
    /// Adds a small visual indicator showing what prerequisite is needed.
    /// </summary>
    void AddPrereqArrow(GameObject upgradeObj, UpgradeData upgrade)
    {
        if (upgrade.prerequisites == null || upgrade.prerequisites.Length == 0) return;

        var arrowGo = new GameObject("PrereqIndicator");
        var rt = arrowGo.AddComponent<RectTransform>();
        rt.SetParent(upgradeObj.transform, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(8f, 4f);
        rt.sizeDelta = new Vector2(20f, 16f);

        var tmp = arrowGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "\u25B2"; // up triangle — points to prerequisite above
        tmp.fontSize = 20;
        tmp.color = lockedTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }
}