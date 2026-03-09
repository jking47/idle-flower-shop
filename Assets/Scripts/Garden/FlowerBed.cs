using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlotState { Empty, Growing, Bloomed }

/// <summary>
/// Individual flower plot. Handles plant → grow → bloom → harvest cycle.
/// Supports locking with a currency cost, gem instant bloom, auto-replant via preferred flower.
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
    FlowerData preferredFlower; // what auto-plant will use next
    float growthTimer;
    float growthDuration;
    int plotIndex;
    Sprite lastStageSprite;

    CurrencyManager currency;

    // Pest damage — multiplier applied to yield at harvest (1.0 = no damage)
    float pestDamageMultiplier = 1f;

    // Lock state
    bool isLocked;
    double unlockCost;
    CurrencyType unlockCurrency;

    // Programmatic UI elements
    GameObject lockOverlay;
    TMP_Text lockCostText;
    GameObject gemIndicator;
    TMP_Text gemCostText;

    public PlotState State => state;
    public FlowerData CurrentFlower => currentFlower;
    public FlowerData PreferredFlower => preferredFlower;
    public float GrowthProgress => growthDuration > 0 ? growthTimer / growthDuration : 0f;
    public float GrowthDuration => growthDuration;
    public bool IsLocked => isLocked;
    public int PlotIndex => plotIndex;

    void Awake()
    {
        CreateLockOverlay();
        CreateGemIndicator();
    }

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

    // --- Lock System ---

    public void SetLocked(double cost, CurrencyType currencyType)
    {
        isLocked = true;
        unlockCost = cost;
        unlockCurrency = currencyType;
        UpdateLockVisuals();
    }

    public void ClearLock()
    {
        isLocked = false;
        UpdateLockVisuals();
    }

    bool TryUnlock()
    {
        if (!isLocked) return true;
        if (currency == null) return false;

        if (!currency.Spend(unlockCurrency, unlockCost))
        {
            if (lockCostText != null)
            {
                StopCoroutine(nameof(FlashLockText));
                StartCoroutine(FlashLockText());
            }
            if (Services.TryGet<GameJuice>(out var juice))
                juice.PlayError();
            return false;
        }

        isLocked = false;
        UpdateLockVisuals();

        if (Services.TryGet<GameJuice>(out var j))
            j.PlayUnlock();

        return true;
    }

    IEnumerator FlashLockText()
    {
        if (lockCostText == null) yield break;
        Color original = lockCostText.color;
        lockCostText.color = new Color(1f, 0.4f, 0.4f);
        yield return new WaitForSeconds(0.4f);
        lockCostText.color = original;
    }

    void UpdateLockVisuals()
    {
        if (lockOverlay == null) return;

        lockOverlay.SetActive(isLocked);

        if (isLocked && lockCostText != null)
        {
            string currencyName = unlockCurrency switch
            {
                CurrencyType.Petals => "Petals",
                CurrencyType.Coins => "Coins",
                CurrencyType.Gems => "Gems",
                _ => ""
            };
            lockCostText.text = $"{unlockCost:F0} {currencyName}";
        }
    }

    // --- Gem Cost Indicator ---

    void UpdateGemIndicator()
    {
        if (gemIndicator == null) return;

        bool showGem = state == PlotState.Growing && !isLocked;
        gemIndicator.SetActive(showGem);

        if (showGem && gemCostText != null)
        {
            if (Services.TryGet<BoostManager>(out var boost))
                gemCostText.text = $"Instant Bloom: {boost.InstantBloomCostGems} Gems";
            else
                gemCostText.text = "Instant Bloom: 5 Gems";
        }
    }

    // --- Programmatic UI Creation ---

    void CreateLockOverlay()
    {
        lockOverlay = new GameObject("LockOverlay");
        var rt = lockOverlay.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = lockOverlay.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);
        img.raycastTarget = false;

        // Lock icon text
        var iconGo = new GameObject("LockIcon");
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.SetParent(rt, false);
        iconRt.anchorMin = new Vector2(0.5f, 0.6f);
        iconRt.anchorMax = new Vector2(0.5f, 0.6f);
        iconRt.sizeDelta = new Vector2(120, 36);

        var iconText = iconGo.AddComponent<TextMeshProUGUI>();
        iconText.text = "LOCKED";
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.fontSize = 22;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = new Color(0.7f, 0.7f, 0.7f);
        iconText.enableWordWrapping = false;
        iconText.overflowMode = TextOverflowModes.Overflow;

        // Cost text
        var textGo = new GameObject("CostText");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = new Vector2(0.5f, 0.3f);
        textRt.anchorMax = new Vector2(0.5f, 0.3f);
        textRt.sizeDelta = new Vector2(160, 36);

        lockCostText = textGo.AddComponent<TextMeshProUGUI>();
        lockCostText.alignment = TextAlignmentOptions.Center;
        lockCostText.fontSize = 22;
        lockCostText.fontStyle = FontStyles.Bold;
        lockCostText.color = new Color(1f, 0.9f, 0.5f);
        lockCostText.enableWordWrapping = false;
        lockCostText.overflowMode = TextOverflowModes.Overflow;

        lockOverlay.SetActive(false);
    }

    void CreateGemIndicator()
    {
        gemIndicator = new GameObject("GemIndicator");
        var rt = gemIndicator.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 4f);
        rt.sizeDelta = new Vector2(0, 40);

        var bg = gemIndicator.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.12f, 0.25f, 0.85f);
        bg.raycastTarget = true;

        // Button for instant bloom
        var btn = gemIndicator.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(TryInstantBloom);

        // Cost text
        var textGo = new GameObject("GemText");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        gemCostText = textGo.AddComponent<TextMeshProUGUI>();
        gemCostText.alignment = TextAlignmentOptions.Center;
        gemCostText.fontSize = 20;
        gemCostText.color = new Color(0.6f, 0.8f, 1f);
        gemCostText.enableWordWrapping = false;
        gemCostText.overflowMode = TextOverflowModes.Overflow;
        gemCostText.raycastTarget = false;

        gemIndicator.SetActive(false);
    }

    // --- Core Flower Bed Logic ---

    public bool Plant(FlowerData flower)
    {
        if (state != PlotState.Empty || isLocked) return false;

        currentFlower = flower;
        preferredFlower = flower;
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

    /// <summary>
    /// Set what auto-plant will use next without interrupting current growth.
    /// Called when the player picks a new flower while something is already growing.
    /// </summary>
    public void SetPreferredFlower(FlowerData flower)
    {
        preferredFlower = flower;
    }

    void Update()
    {
        if (state != PlotState.Growing) return;

        growthTimer += Time.deltaTime;

        if (progressFill != null)
            progressFill.fillAmount = GrowthProgress;

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

        if (isLocked)
        {
            TryUnlock();
            return;
        }

        switch (state)
        {
            case PlotState.Bloomed:
                Harvest(false);
                break;
            case PlotState.Growing:
                // If auto-plant is unlocked, tap to change preferred flower
                // Otherwise, tap to instant bloom (gem badge also works for instant bloom)
                if (Services.TryGet<GardenManager>(out var garden) && garden.AutoPlantUnlocked)
                    EventBus.Publish(new PlotSelectedEvent { plotIndex = plotIndex });
                else
                    TryInstantBloom();
                break;
            case PlotState.Empty:
                EventBus.Publish(new PlotSelectedEvent { plotIndex = plotIndex });
                break;
        }
    }

    /// <summary>
    /// Instant bloom via gem indicator tap.
    /// </summary>
    public void TryInstantBloom()
    {
        if (state != PlotState.Growing) return;
        if (Services.TryGet<BoostManager>(out var boost))
            boost.InstantBloom(this);
    }

    void Harvest(bool isAuto)
    {
        if (state != PlotState.Bloomed || currentFlower == null) return;

        double yield = currentFlower.baseYield;
        if (Services.TryGet<UpgradeManager>(out var upgrades))
            yield *= upgrades.GetYieldMultiplier();
        if (Services.TryGet<BoostManager>(out var boost))
            yield *= boost.BoostMultiplier;

        yield *= pestDamageMultiplier;
        pestDamageMultiplier = 1f;
        if (flowerImage != null) flowerImage.color = Color.white; // clear damage tint

        if (!Services.TryGet<CurrencyManager>(out var currencyManager)) return;
        currencyManager.Add(CurrencyType.Petals, yield);

        EventBus.Publish(new FlowerHarvestedEvent
        {
            flowerData = currentFlower,
            yield = yield,
            plotIndex = plotIndex
        });

        currentFlower = null;
        state = PlotState.Empty;
        growthTimer = 0f;
        lastStageSprite = null;

        UpdateVisuals();

        // Auto-replant using preferred flower
        if (preferredFlower != null &&
            Services.TryGet<GardenManager>(out var garden) &&
            garden.AutoPlantUnlocked)
        {
            garden.PlantFlower(plotIndex, preferredFlower);
        }
    }

    public void AutoHarvest()
    {
        if (state != PlotState.Bloomed || currentFlower == null) return;
        Harvest(true);
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
        if (isLocked) return;

        currentFlower = flower;
        preferredFlower = flower;
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

    /// <summary>
    /// Called by a pest that reached this plot. Reduces yield at next harvest
    /// and applies a red tint to signal the damage.
    /// </summary>
    public void ApplyPestDamage(float penaltyPercent)
    {
        pestDamageMultiplier = Mathf.Clamp01(1f - penaltyPercent);

        // Red tint on the flower image to signal damage
        if (flowerImage != null)
            flowerImage.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f), penaltyPercent);
    }

    void UpdateVisuals()
    {
        UpdateLockVisuals();
        UpdateGemIndicator();

        if (flowerImage == null) return;

        if (isLocked)
        {
            flowerImage.enabled = false;
            if (progressFill != null) progressFill.fillAmount = 0f;
            return;
        }

        switch (state)
        {
            case PlotState.Empty:
                flowerImage.enabled = false;
                if (progressFill != null) progressFill.fillAmount = 0f;
                lastStageSprite = null;
                break;

            case PlotState.Growing:
                flowerImage.enabled = true;
                flowerImage.color = Color.white;
                UpdateGrowthSprite();
                break;

            case PlotState.Bloomed:
                flowerImage.enabled = true;
                flowerImage.color = Color.white;
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

