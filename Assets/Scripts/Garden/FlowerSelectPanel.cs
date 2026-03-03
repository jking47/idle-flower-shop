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

        foreach (var flower in garden.AvailableFlowers)
        {
            if (Services.TryGet<GameManager>(out var gm) && flower.requiredPhase > gm.CurrentPhase)
                continue;

            var obj = Instantiate(flowerButtonPrefab, buttonContainer);
            spawnedButtons.Add(obj);

            var icon = obj.transform.Find("Icon")?.GetComponent<Image>();
            var nameText = obj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var costText = obj.transform.Find("CostText")?.GetComponent<TMP_Text>();
            var button = obj.GetComponent<Button>();

            if (icon != null && flower.icon != null)
                icon.sprite = flower.icon;

            if (nameText != null)
                nameText.text = flower.displayName;

            if (costText != null)
                costText.text = flower.plantCost > 0 ? $"{flower.plantCost:F0} petals" : "Free";

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
    }

    void OnFlowerChosen(FlowerData flower)
    {
        if (targetPlotIndex < 0) return;

        if (garden.PlantFlower(targetPlotIndex, flower))
        {
            Close();
        }
    }
}