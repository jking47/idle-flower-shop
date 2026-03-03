using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight event-driven tutorial system.
/// Shows contextual hints on first-time actions, non-blocking.
/// Attach to a UI element under Canvas.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject hintPanel;
    [SerializeField] TMP_Text hintText;
    [SerializeField] Button dismissButton;

    [Header("Settings")]
    [SerializeField] float autoHideDuration = 6f;

    readonly HashSet<string> shownHints = new();
    readonly Queue<string> hintQueue = new();
    bool isShowingHint;
    int harvestCount;

    const string SAVE_KEY = "TutorialProgress";

    static readonly Dictionary<string, string> hints = new()
    {
        { "first_plot_tap",       "Tap an empty plot to plant your first flower!" },
        { "first_plant",          "Your flower is growing! It will bloom in a few seconds." },
        { "first_bloom",          "Your flower bloomed! Tap it to harvest petals." },
        { "first_harvest",        "Nice! Spend petals to plant more flowers or buy upgrades." },
        { "upgrades_available",   "You have enough petals for an upgrade! Check the Upgrades button." },
        { "social_intro",         "Visit the Social tab to see friends and collect gifts!" },
        { "shop_unlocked",        "Your flower shop is open! Fill customer orders for coins." },
        { "gems_intro",           "Gems let you speed up growth and unlock premium flowers." },
        { "instant_bloom",        "Tip: Tap a growing flower to instantly bloom it with gems!" }
    };

    void Awake()
    {
        Services.Register(this);

        if (hintPanel != null) hintPanel.SetActive(false);
        if (dismissButton != null) dismissButton.onClick.AddListener(DismissCurrentHint);

        LoadProgress();
    }

    void OnEnable()
    {
        EventBus.Subscribe<FlowerPlantedEvent>(OnFlowerPlanted);
        EventBus.Subscribe<FlowerBloomedEvent>(OnFlowerBloomed);
        EventBus.Subscribe<FlowerHarvestedEvent>(OnFlowerHarvested);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<FlowerPlantedEvent>(OnFlowerPlanted);
        EventBus.Unsubscribe<FlowerBloomedEvent>(OnFlowerBloomed);
        EventBus.Unsubscribe<FlowerHarvestedEvent>(OnFlowerHarvested);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void Start()
    {
        StartCoroutine(DelayedStartHint());
    }

    IEnumerator DelayedStartHint()
    {
        yield return new WaitForSeconds(1f);
        TryShowHint("first_plot_tap");
    }

    // --- Event Handlers ---

    void OnFlowerPlanted(FlowerPlantedEvent evt)
    {
        TryShowHint("first_plant");
    }

    void OnFlowerBloomed(FlowerBloomedEvent evt)
    {
        TryShowHint("first_bloom");
    }

    void OnFlowerHarvested(FlowerHarvestedEvent evt)
    {
        harvestCount++;

        if (harvestCount == 1)
            TryShowHint("first_harvest");

        if (harvestCount == 2)
            TryShowHint("instant_bloom");

        if (harvestCount == 3)
            TryShowHint("social_intro");
    }

    void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        // Check if player can afford any upgrade
        if (evt.currencyType == CurrencyType.Petals)
        {
            if (Services.TryGet<UpgradeManager>(out var upgrades))
            {
                foreach (var upgrade in upgrades.Upgrades)
                {
                    if (!upgrades.IsMaxed(upgrade))
                    {
                        double cost = upgrades.GetNextCost(upgrade);
                        if (evt.newAmount >= cost)
                        {
                            TryShowHint("upgrades_available");
                            break;
                        }
                    }
                }
            }
        }

        // Gems intro when player first receives gems
        if (evt.currencyType == CurrencyType.Gems && evt.previousAmount == 0 && evt.newAmount > 0)
        {
            TryShowHint("gems_intro");
        }
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        if (evt.phase == GamePhase.Shop)
            TryShowHint("shop_unlocked");
    }

    // --- Hint Display ---

    void TryShowHint(string hintId)
    {
        if (shownHints.Contains(hintId)) return;
        if (!hints.ContainsKey(hintId)) return;

        shownHints.Add(hintId);
        SaveProgress();

        hintQueue.Enqueue(hintId);
        if (!isShowingHint)
            ShowNextHint();
    }

    void ShowNextHint()
    {
        if (hintQueue.Count == 0)
        {
            isShowingHint = false;
            return;
        }

        isShowingHint = true;
        string hintId = hintQueue.Dequeue();

        if (hintPanel != null) hintPanel.SetActive(true);
        if (hintText != null) hintText.text = hints[hintId];

        StartCoroutine(AutoHideHint());
    }

    IEnumerator AutoHideHint()
    {
        yield return new WaitForSeconds(autoHideDuration);
        DismissCurrentHint();
    }

    void DismissCurrentHint()
    {
        StopCoroutine(nameof(AutoHideHint));
        if (hintPanel != null) hintPanel.SetActive(false);

        StartCoroutine(DelayedNextHint());
    }

    IEnumerator DelayedNextHint()
    {
        yield return new WaitForSeconds(0.5f);
        ShowNextHint();
    }

    // --- Persistence ---

    void SaveProgress()
    {
        string data = string.Join(",", shownHints);
        PlayerPrefs.SetString(SAVE_KEY, data);
    }

    void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        string data = PlayerPrefs.GetString(SAVE_KEY);
        foreach (string id in data.Split(','))
        {
            if (!string.IsNullOrEmpty(id))
                shownHints.Add(id);
        }
    }

    /// <summary>
    /// Reset all tutorial progress (used by debug panel).
    /// </summary>
    public void ResetTutorial()
    {
        shownHints.Clear();
        harvestCount = 0;
        PlayerPrefs.DeleteKey(SAVE_KEY);
        Debug.Log("[Tutorial] Progress reset.");
    }
}