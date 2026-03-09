using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup panel for selecting which flower to plant.
/// Spawns a button per available flower type. Attach to a panel object under Canvas.
/// </summary>
public class FlowerSelectPanel : MonoBehaviour, IPanel
{
    [Header("References")]
    [SerializeField] Transform buttonContainer;
    [SerializeField] GameObject flowerButtonPrefab;
    [SerializeField] Button closeButton;

    int targetPlotIndex = -1;
    readonly List<GameObject> spawnedButtons = new();

    GardenManager garden;
    CurrencyManager currency;

    void Awake()
    {
        Services.Register(this);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        EventBus.Subscribe<PlotSelectedEvent>(OnPlotSelected);

        gameObject.SetActive(false);
    }

    void Start()
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Register(this);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<PlotSelectedEvent>(OnPlotSelected);
    }

    void OnPlotSelected(PlotSelectedEvent evt)
    {
        Open(evt.plotIndex);
    }

    public void Open(int plotIndex)
    {
        if (Services.TryGet<PanelManager>(out var pm))
            pm.Open(this);

        if (garden == null) garden = Services.Get<GardenManager>();
        if (currency == null) currency = Services.Get<CurrencyManager>();

        targetPlotIndex = plotIndex;
        gameObject.SetActive(true);
        BuildButtons();
    }

    public void Close()
    {
        foreach (var btn in spawnedButtons)
            Destroy(btn);
        spawnedButtons.Clear();

        gameObject.SetActive(false);
        targetPlotIndex = -1;
    }

    void BuildButtons()
    {
        foreach (var btn in spawnedButtons)
            Destroy(btn);
        spawnedButtons.Clear();

        Services.TryGet<GameManager>(out var gm);

        foreach (var flower in garden.AvailableFlowers)
        {
            if (gm != null && flower.requiredPhase > gm.CurrentPhase)
                continue;

            var obj = Instantiate(flowerButtonPrefab, buttonContainer);
            spawnedButtons.Add(obj);

            var icon = obj.transform.Find("Icon")?.GetComponent<Image>();
            var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var costText = obj.transform.Find("CostText")?.GetComponent<TMP_Text>();
            var button = obj.GetComponent<Button>();

            bool isUnlocked = garden.IsFlowerUnlocked(flower);

            if (icon != null && flower.icon != null)
                icon.sprite = flower.icon;

            if (nameText != null)
                nameText.text = flower.displayName;

            if (isUnlocked)
            {
                if (costText != null)
                {
                    string plantCostStr = flower.plantCost > 0
                        ? $"Cost: {flower.plantCost:F0} petals"
                        : "Free";
                    costText.text = $"{plantCostStr}\n<size=22><color=#8899AA>Yield: +{flower.baseYield:F0} | Grow: {flower.growTime:F0}s</color></size>";
                }

                bool canAfford = currency.CanAfford(CurrencyType.Petals, flower.plantCost) || flower.plantCost <= 0;
                if (!canAfford)
                {
                    var colors = button.colors;
                    colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    button.colors = colors;
                }

                var selectedFlower = flower;
                button.onClick.AddListener(() => OnFlowerChosen(selectedFlower));
            }
            else
            {
                // Locked — show unlock cost and handle unlock tap
                if (costText != null)
                {
                    bool canAffordUnlock = currency.CanAfford(CurrencyType.Petals, flower.unlockCost);
                    string affordColor = canAffordUnlock ? "#FFDD88" : "#FF6666";
                    costText.text = $"<color=#AABBCC>LOCKED</color>\n<size=22><color={affordColor}>Unlock: {flower.unlockCost:F0} petals</color></size>";
                }

                // Grey tint for locked state
                if (icon != null)
                    icon.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);

                var unlockFlower = flower;
                button.onClick.AddListener(() => OnUnlockChosen(unlockFlower));
            }
        }
    }

    void OnFlowerChosen(FlowerData flower)
    {
        if (targetPlotIndex < 0) return;

        if (garden.PlantFlower(targetPlotIndex, flower))
        {
            Close();
        }
    }

    void OnUnlockChosen(FlowerData flower)
    {
        if (garden.TryUnlockFlower(flower))
        {
            if (Services.TryGet<GameJuice>(out var juice))
                juice.PlayUnlock();
            BuildButtons(); // refresh to show unlocked state
        }
        else
        {
            if (Services.TryGet<GameJuice>(out var juice))
                juice.PlayError();
        }
    }
}