using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Two-mode tutorial system:
///
/// 1. Sequential steps (action-gated): core onboarding shown to new players.
///    Each step displays a hint and waits for the player to complete a specific
///    action before advancing. Dismiss collapses the panel but the step stays
///    active — useful when the hint is covering the thing the player needs to tap.
///    Step completion times are logged so you can spot where players stall.
///
/// 2. Contextual hints (fire-and-forget): late-game tips shown once when
///    relevant conditions are met. These only trigger after the sequential
///    tutorial is complete so they don't collide with onboarding.
///
/// Attach to GameManager (not the hint panel).
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject hintPanel;
    [SerializeField] TMP_Text hintText;
    [SerializeField] Button dismissButton;

    [Header("Settings")]
    [SerializeField] float contextualHintAutoHideDuration = 10f;
    [SerializeField] float stepReminderDelay = 20f;

    // -------------------------------------------------------------------------
    // Sequential Tutorial
    // -------------------------------------------------------------------------

    class TutorialStep
    {
        public string id;
        public string message;
        public Action<TutorialManager> Subscribe;
        public Action<TutorialManager> Unsubscribe;
        // Optional: if set, the step is skipped when this returns true at start time.
        // Use for conditions that may already be met before the tutorial reaches the step.
        public Func<bool> AlreadyComplete;
    }

    static readonly List<TutorialStep> SequentialSteps = new()
    {
        new TutorialStep
        {
            id              = "plant",
            message         = "Tap an empty plot to plant your first flower!",
            Subscribe       = tm => EventBus.Subscribe<FlowerPlantedEvent>(tm.OnStepEventPlanted),
            Unsubscribe     = tm => EventBus.Unsubscribe<FlowerPlantedEvent>(tm.OnStepEventPlanted),
            // The game auto-plants plot 0 on a fresh start — if any plot is already
            // growing or bloomed, the player has effectively completed this step.
            AlreadyComplete = () => Services.TryGet<GardenManager>(out var g) &&
                                    g.Plots.Count > 0 &&
                                    g.Plots[0].State != PlotState.Empty,
        },
        new TutorialStep
        {
            id          = "bloom",
            message     = "Your flower is growing! Wait for it to bloom, or tap it to speed things up.",
            Subscribe   = tm => EventBus.Subscribe<FlowerBloomedEvent>(tm.OnStepEventBloomed),
            Unsubscribe = tm => EventBus.Unsubscribe<FlowerBloomedEvent>(tm.OnStepEventBloomed),
            // Skip if a flower is already bloomed (e.g. player saved mid-step and the
            // event won't fire again on reload — advance directly to harvest).
            AlreadyComplete = () =>
            {
                if (!Services.TryGet<GardenManager>(out var g)) return false;
                foreach (var p in g.Plots) if (p.State == PlotState.Bloomed) return true;
                return false;
            },
        },
        new TutorialStep
        {
            id          = "harvest",
            message     = "Your flower bloomed! Tap it to harvest petals.",
            Subscribe   = tm => EventBus.Subscribe<FlowerHarvestedEvent>(tm.OnStepEventHarvested),
            Unsubscribe = tm => EventBus.Unsubscribe<FlowerHarvestedEvent>(tm.OnStepEventHarvested),
        },
    };

    int currentStepIndex;
    float stepStartTime;
    bool tutorialComplete;

    // -------------------------------------------------------------------------
    // Contextual Hints
    // -------------------------------------------------------------------------

    readonly HashSet<string> shownHints = new();
    readonly Queue<string> hintQueue = new();
    Coroutine autoHideCoroutine;
    Coroutine stepReminderCoroutine;
    bool isShowingContextualHint;
    int harvestCount;

    static readonly Dictionary<string, string> ContextualHints = new()
    {
        { "post_tutorial",      "Great start! Plant more flowers or check Upgrades to grow faster." },
        { "pest_intro",         "Pests are attacking your garden! Grab the spray bottle and drag it over them to chase them off before they damage your flowers!" },
        { "instant_bloom",      "Tip: Tap a growing flower to instantly bloom it with gems!" },
        { "upgrades_available", "You have enough petals for an upgrade! Check the Upgrades button." },
        { "shop_unlocked",      "Your flower shop is open! Fill customer orders for coins." },
        { "shop_orders_how",    "To fill an order, harvest the required flowers then tap the Deliver button inside the Shop." },
        { "social_intro",       "Visit the Social tab to see friends and collect gifts!" },
        { "gems_intro",         "Gems let you speed up growth and unlock premium flowers." },
        { "store_ad",           "Low on petals? Visit the Store to watch an ad for free petals!" },
        { "watering_can",       "Drag the watering can over a growing flower to make it grow 3× faster! It refills on its own over time." },
        { "plot_unlock",        "Tap a locked plot to unlock it with petals — more plots means more flowers growing at once!" },
    };

    // -------------------------------------------------------------------------
    // Save Keys
    // -------------------------------------------------------------------------

    const string SAVE_KEY_STEP    = "TutorialStep";
    const string SAVE_KEY_HINTS   = "TutorialHints";
    const string SAVE_KEY_HARVEST = "TutorialHarvestCount";

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    void Awake()
    {
        Services.Register(this);

        if (hintPanel != null)   hintPanel.SetActive(false);
        if (dismissButton != null) dismissButton.onClick.AddListener(OnDismissClicked);

        LoadProgress();
    }

    void OnEnable()
    {
        EventBus.Subscribe<FlowerHarvestedEvent>(OnFlowerHarvested);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
        EventBus.Subscribe<PestEventStartedEvent>(OnPestEventStarted);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<FlowerHarvestedEvent>(OnFlowerHarvested);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
        EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgradePurchased);
        EventBus.Unsubscribe<PestEventStartedEvent>(OnPestEventStarted);
    }

    void Start()
    {
        if (!tutorialComplete)
            StartCoroutine(BeginSequentialTutorial());
    }

    // =========================================================================
    // Sequential Tutorial
    // =========================================================================

    IEnumerator BeginSequentialTutorial()
    {
        yield return new WaitForSeconds(1.5f);
        StartStep(currentStepIndex);
    }

    void StartStep(int index)
    {
        if (index >= SequentialSteps.Count)
        {
            CompleteTutorial();
            return;
        }

        currentStepIndex = index;
        stepStartTime    = Time.time;
        SaveProgress();

        var step = SequentialSteps[index];

        // If the condition for this step is already satisfied (e.g. the game
        // auto-planted a flower before the tutorial reached the plant step),
        // skip it silently rather than showing a hint the player can't act on.
        if (step.AlreadyComplete != null && step.AlreadyComplete())
        {
            Debug.Log($"[Tutorial] Step {index} '{step.id}' skipped — already complete.");
            StartStep(index + 1);
            return;
        }

        step.Subscribe(this);
        ShowHint(step.message, autoHide: false);

        Debug.Log($"[Tutorial] Step {index} started: '{step.id}'");
    }

    void CompleteCurrentStep()
    {
        if (tutorialComplete) return;
        if (currentStepIndex < 0 || currentStepIndex >= SequentialSteps.Count) return;

        var step    = SequentialSteps[currentStepIndex];
        float elapsed = Time.time - stepStartTime;

        step.Unsubscribe(this);
        CancelStepReminder();
        Debug.Log($"[Tutorial] Step {currentStepIndex} '{step.id}' completed in {elapsed:F1}s");

        StartCoroutine(AdvanceStepAfterDelay(0.5f));
    }

    IEnumerator AdvanceStepAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideHint();
        yield return new WaitForSeconds(0.3f);
        StartStep(currentStepIndex + 1);
    }

    void CompleteTutorial()
    {
        tutorialComplete = true;
        HideHint();
        SaveProgress();
        Debug.Log("[Tutorial] Core tutorial complete.");

        TryShowContextualHint("post_tutorial");
    }

    // Step completion event handlers — each just signals the active step is done.
    void OnStepEventPlanted(FlowerPlantedEvent evt)     => CompleteCurrentStep();
    void OnStepEventBloomed(FlowerBloomedEvent evt)     => CompleteCurrentStep();
    void OnStepEventHarvested(FlowerHarvestedEvent evt) => CompleteCurrentStep();

    // =========================================================================
    // Contextual Hints
    // =========================================================================

    void OnFlowerHarvested(FlowerHarvestedEvent evt)
    {
        if (!tutorialComplete) return;

        harvestCount++;
        SaveProgress();

        if (harvestCount == 4) TryShowContextualHint("instant_bloom");
        else if (harvestCount == 8) TryShowSocialHint();
    }

    void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        if (!tutorialComplete) return;

        if (evt.currencyType == CurrencyType.Petals && Services.TryGet<UpgradeManager>(out var upgrades))
        {
            foreach (var upgrade in upgrades.Upgrades)
            {
                if (!upgrades.IsMaxed(upgrade) && evt.newAmount >= upgrades.GetNextCost(upgrade))
                {
                    TryShowContextualHint("upgrades_available");
                    break;
                }
            }
        }

        if (evt.currencyType == CurrencyType.Gems && evt.previousAmount == 0 && evt.newAmount > 0)
            TryShowContextualHint("gems_intro");
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        // Phase milestone hints fire regardless of tutorial state
        if (evt.phase == GamePhase.Garden)
            TryShowContextualHint("plot_unlock");

        if (evt.phase == GamePhase.Shop)
        {
            TryShowContextualHint("shop_unlocked");
            TryShowContextualHint("shop_orders_how");
        }

        if (evt.phase == GamePhase.Garden && harvestCount >= 8)
            TryShowContextualHint("social_intro");
    }

    void OnUpgradePurchased(UpgradePurchasedEvent evt)
    {
        if (!tutorialComplete) return;
        if (!Services.TryGet<CurrencyManager>(out var currency)) return;

        // Teach the watering can mechanic the first time it's unlocked
        if (evt.upgradeId == "WateringCan" && evt.newLevel == 1)
            TryShowContextualHint("watering_can");

        if (currency.GetBalance(CurrencyType.Petals) < 8)
            TryShowContextualHint("store_ad");
    }

    void OnPestEventStarted(PestEventStartedEvent evt)
    {
        TryShowContextualHint("pest_intro");
    }

    void TryShowSocialHint()
    {
        if (!Services.TryGet<GameManager>(out var gm)) return;
        if (gm.CurrentPhase < GamePhase.Garden) return;

        TryShowContextualHint("social_intro");
    }

    void TryShowContextualHint(string hintId)
    {
        if (shownHints.Contains(hintId)) return;
        if (!ContextualHints.ContainsKey(hintId)) return;

        shownHints.Add(hintId);
        SaveProgress();

        hintQueue.Enqueue(hintId);
        if (!isShowingContextualHint)
            ShowNextContextualHint();
    }

    void ShowNextContextualHint()
    {
        if (hintQueue.Count == 0)
        {
            isShowingContextualHint = false;
            return;
        }

        isShowingContextualHint = true;
        string hintId = hintQueue.Dequeue();
        ShowHint(ContextualHints[hintId], autoHide: true);
    }

    // =========================================================================
    // UI
    // =========================================================================

    void ShowHint(string message, bool autoHide)
    {
        // Cancel any pending contextual auto-hide before showing new content
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (hintPanel != null) hintPanel.SetActive(true);
        if (hintText != null)  hintText.text = message;

        if (autoHide)
            autoHideCoroutine = StartCoroutine(AutoHideContextualHint());
    }

    IEnumerator AutoHideContextualHint()
    {
        yield return new WaitForSeconds(contextualHintAutoHideDuration);
        autoHideCoroutine = null;
        HideHint();
        yield return new WaitForSeconds(0.8f);
        ShowNextContextualHint();
    }

    void HideHint()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (hintPanel != null) hintPanel.SetActive(false);
    }

    void OnDismissClicked()
    {
        if (!tutorialComplete)
        {
            // Collapse the panel so the player can see and tap what they need to.
            // Schedule a reminder to re-show the hint if the step isn't done soon.
            HideHint();
            CancelStepReminder();
            stepReminderCoroutine = StartCoroutine(StepReminderAfterDelay());
            return;
        }

        // Contextual mode: dismiss current hint and show next in queue.
        HideHint();
        StartCoroutine(DelayedNextContextualHint());
    }

    IEnumerator StepReminderAfterDelay()
    {
        yield return new WaitForSeconds(stepReminderDelay);
        stepReminderCoroutine = null;

        // Only re-show if still on the same step and the panel is hidden
        if (!tutorialComplete && hintPanel != null && !hintPanel.activeSelf)
            ShowHint(SequentialSteps[currentStepIndex].message, autoHide: false);
    }

    void CancelStepReminder()
    {
        if (stepReminderCoroutine != null)
        {
            StopCoroutine(stepReminderCoroutine);
            stepReminderCoroutine = null;
        }
    }

    IEnumerator DelayedNextContextualHint()
    {
        yield return new WaitForSeconds(0.8f);
        ShowNextContextualHint();
    }

    // =========================================================================
    // Persistence
    // =========================================================================

    void SaveProgress()
    {
        // Store Count when complete so the load check (savedStep >= Count) works correctly
        PlayerPrefs.SetInt(SAVE_KEY_STEP, tutorialComplete ? SequentialSteps.Count : currentStepIndex);
        PlayerPrefs.SetString(SAVE_KEY_HINTS, string.Join(",", shownHints));
        PlayerPrefs.SetInt(SAVE_KEY_HARVEST, harvestCount);
    }

    void LoadProgress()
    {
        int savedStep    = PlayerPrefs.GetInt(SAVE_KEY_STEP, 0);
        tutorialComplete = savedStep >= SequentialSteps.Count;
        if (!tutorialComplete)
            currentStepIndex = savedStep;

        string savedHints = PlayerPrefs.GetString(SAVE_KEY_HINTS, "");
        foreach (string id in savedHints.Split(','))
            if (!string.IsNullOrEmpty(id)) shownHints.Add(id);

        harvestCount = PlayerPrefs.GetInt(SAVE_KEY_HARVEST, 0);
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Reset all tutorial progress (used by debug panel).</summary>
    public void ResetTutorial()
    {
        // Unsubscribe the active step's event listener if mid-tutorial
        if (!tutorialComplete && currentStepIndex >= 0 && currentStepIndex < SequentialSteps.Count)
            SequentialSteps[currentStepIndex].Unsubscribe(this);

        StopAllCoroutines();

        shownHints.Clear();
        harvestCount            = 0;
        currentStepIndex        = 0;
        tutorialComplete        = false;
        isShowingContextualHint = false;
        autoHideCoroutine       = null;
        stepReminderCoroutine   = null;

        PlayerPrefs.DeleteKey(SAVE_KEY_STEP);
        PlayerPrefs.DeleteKey(SAVE_KEY_HINTS);
        PlayerPrefs.DeleteKey(SAVE_KEY_HARVEST);

        HideHint();
        Debug.Log("[Tutorial] Progress reset.");
    }
}
