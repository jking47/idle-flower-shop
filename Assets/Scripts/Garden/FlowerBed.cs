using UnityEngine;
using UnityEngine.UI;

public enum PlotState { Empty, Growing, Bloomed }

/// <summary>
/// Individual flower plot. Handles plant → grow → bloom → harvest cycle.
/// Attach to a UI button or world-space clickable object.
/// </summary>
public class FlowerBed : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image flowerImage;
    [SerializeField] Image progressFill;
    [SerializeField] Button interactButton;

    [Header("Watering Visual")]
    [SerializeField] GameObject wateringEffect;

    [Header("State (read-only in inspector)")]
    [SerializeField] PlotState state = PlotState.Empty;

    FlowerData currentFlower;
    float growthTimer;
    float growthDuration;
    int plotIndex;
    Sprite lastStageSprite; // tracks current growth sprite to avoid redundant swaps

    CurrencyManager currency;

    public PlotState State => state;
    public FlowerData CurrentFlower => currentFlower;
    public float GrowthProgress => growthDuration > 0 ? growthTimer / growthDuration : 0f;

    void Start()
    {
        currency = Services.Get<CurrencyManager>();
        interactButton.onClick.AddListener(OnClicked);
        UpdateVisuals();
    }

    public void Initialize(int index)
    {
        plotIndex = index;
    }

    public bool Plant(FlowerData flower)
    {
        if (state != PlotState.Empty) return false;

        currentFlower = flower;
        growthTimer = 0f;
        growthDuration = flower.growTime;
        if (Services.TryGet<UpgradeManager>(out var upgrades))
            growthDuration *= (float)upgrades.GetGrowSpeedMultiplier();
        state = PlotState.Growing;
        lastStageSprite = null;

        EventBus.Publish(new FlowerPlantedEvent
        {
            flowerData = flower,
            plotIndex = plotIndex
        });

        UpdateVisuals();
        return true;
    }

    void Update()
    {
        if (state != PlotState.Growing) return;

        growthTimer += Time.deltaTime;

        if (progressFill != null)
            progressFill.fillAmount = GrowthProgress;

        // Update growth stage sprite
        UpdateGrowthSprite();

        if (growthTimer >= growthDuration)
        {
            state = PlotState.Bloomed;

            EventBus.Publish(new FlowerBloomedEvent
            {
                flowerData = currentFlower,
                plotIndex = plotIndex
            });

            UpdateVisuals();
        }
    }

    /// <summary>
    /// Swap the flower image to match current growth stage.
    /// Only updates when the stage actually changes.
    /// </summary>
    void UpdateGrowthSprite()
    {
        if (currentFlower == null || flowerImage == null) return;

        var sprite = FlowerSpriteInitializer.GetStageSprite(currentFlower.name, GrowthProgress);
        if (sprite != null && sprite != lastStageSprite)
        {
            flowerImage.sprite = sprite;
            lastStageSprite = sprite;
        }
    }

    void OnClicked()
    {
        if (Services.TryGet<WateringCan>(out var can) && can.IsDragging)
            return;

        switch (state)
        {
            case PlotState.Bloomed:
                Harvest();
                break;
            case PlotState.Growing:
                if (Services.TryGet<BoostManager>(out var boost))
                    boost.InstantBloom(this);
                break;
            case PlotState.Empty:
                EventBus.Publish(new PlotSelectedEvent { plotIndex = plotIndex });
                break;
        }
    }

    void Harvest()
    {
        if (state != PlotState.Bloomed || currentFlower == null) return;

        double yield = currentFlower.baseYield;
        if (Services.TryGet<UpgradeManager>(out var upgrades))
            yield *= upgrades.GetYieldMultiplier();
        if (Services.TryGet<BoostManager>(out var boost))
            yield *= boost.BoostMultiplier;

        currency.Add(CurrencyType.Petals, yield);

        EventBus.Publish(new FlowerHarvestedEvent
        {
            flowerData = currentFlower,
            yield = yield
        });

        currentFlower = null;
        state = PlotState.Empty;
        growthTimer = 0f;
        lastStageSprite = null;

        UpdateVisuals();
    }

    public void AutoHarvest()
    {
        if (state != PlotState.Bloomed || currentFlower == null) return;
        Harvest();
    }

    public void ForceBloom()
    {
        if (state != PlotState.Growing) return;

        growthTimer = growthDuration;
        state = PlotState.Bloomed;

        EventBus.Publish(new FlowerBloomedEvent
        {
            flowerData = currentFlower,
            plotIndex = plotIndex
        });

        UpdateVisuals();
    }

    public void ApplyOfflineTime(float seconds)
    {
        if (state != PlotState.Growing) return;
        growthTimer += seconds;

        if (growthTimer >= growthDuration)
        {
            state = PlotState.Bloomed;

            EventBus.Publish(new FlowerBloomedEvent
            {
                flowerData = currentFlower,
                plotIndex = plotIndex
            });

            UpdateVisuals();
        }
    }

    public void LoadState(FlowerData flower, PlotState savedState, float progress)
    {
        currentFlower = flower;
        state = savedState;

        growthDuration = flower.growTime;
        if (Services.TryGet<UpgradeManager>(out var upgrades))
            growthDuration *= (float)upgrades.GetGrowSpeedMultiplier();

        growthTimer = progress * growthDuration;
        lastStageSprite = null;

        UpdateVisuals();
    }

    public void SetWateringVisual(bool watering)
    {
        if (wateringEffect != null)
            wateringEffect.SetActive(watering);
    }

    void UpdateVisuals()
    {
        if (flowerImage == null) return;

        switch (state)
        {
            case PlotState.Empty:
                flowerImage.enabled = false;
                if (progressFill != null) progressFill.fillAmount = 0f;
                lastStageSprite = null;
                break;

            case PlotState.Growing:
                flowerImage.enabled = true;
                flowerImage.color = Color.white; // full color now, stages handle the visual
                UpdateGrowthSprite();
                break;

            case PlotState.Bloomed:
                flowerImage.enabled = true;
                flowerImage.color = Color.white;
                // Use the bloomed stage sprite
                var bloomed = FlowerSpriteInitializer.GetStageSprite(
                    currentFlower?.name, 1f);
                if (bloomed != null)
                {
                    flowerImage.sprite = bloomed;
                    lastStageSprite = bloomed;
                }
                else if (currentFlower != null)
                {
                    flowerImage.sprite = currentFlower.icon;
                }
                if (progressFill != null) progressFill.fillAmount = 1f;
                break;
        }
    }
}

public struct PlotSelectedEvent
{
    public int plotIndex;
}