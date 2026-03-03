using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI panel for purchasing upgrades. Toggle open/closed via a HUD button.
/// Refreshes on every open and after each purchase.
/// </summary>
public class UpgradePanel : MonoBehaviour, IPanel
{
    [Header("References")]
    [SerializeField] Transform buttonContainer;
    [SerializeField] GameObject upgradeButtonPrefab;
    [SerializeField] Button closeButton;
    [SerializeField] Button openButton;

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
            if (gm != null && upgrade.requiredPhase > gm.CurrentPhase)
                continue;

            var obj = Instantiate(upgradeButtonPrefab, buttonContainer);
            spawnedButtons.Add(obj);

            var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var costText = obj.transform.Find("CostText")?.GetComponent<TMP_Text>();
            var levelText = obj.transform.Find("LevelText")?.GetComponent<TMP_Text>();
            var icon = obj.transform.Find("Icon")?.GetComponent<Image>();
            var button = obj.GetComponent<Button>();

            int level = upgradeManager.GetLevel(upgrade);
            bool maxed = upgradeManager.IsMaxed(upgrade);

            if (icon != null && upgrade.icon != null)
                icon.sprite = upgrade.icon;

            if (nameText != null)
                nameText.text = upgrade.displayName;

            if (levelText != null)
                levelText.text = maxed ? "MAX" : $"Lv.{level}";

            if (costText != null)
            {
                if (maxed)
                    costText.text = "—";
                else
                    costText.text = $"{upgrade.GetCost(level):F0} {upgrade.costCurrency}";
            }

            if (maxed)
            {
                button.interactable = false;
            }
            else
            {
                double cost = upgrade.GetCost(level);
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
    }
}