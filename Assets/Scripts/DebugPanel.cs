using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug panel for testing and recruiter demos.
/// Toggle visibility with a small button in the corner.
/// Attach to a panel under Canvas.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] Button toggleButton;

    [Header("Panel Content")]
    [SerializeField] GameObject contentPanel;

    [Header("Currency Buttons")]
    [SerializeField] Button addPetalsButton;
    [SerializeField] Button addCoinsButton;
    [SerializeField] Button addRenownButton;
    [SerializeField] Button addGemsButton;

    [Header("Time")]
    [SerializeField] Button skip1MinButton;
    [SerializeField] Button skip1HourButton;
    [SerializeField] Button skip8HourButton;

    [Header("Phase")]
    [SerializeField] Button nextPhaseButton;
    [SerializeField] TMP_Text phaseText;

    [Header("Save")]
    [SerializeField] Button wipeSaveButton;
    [SerializeField] Button forceSaveButton;

    [Header("Settings")]
    [SerializeField] double currencyGrantAmount = 500;

    void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        if (addPetalsButton != null)
            addPetalsButton.onClick.AddListener(() => AddCurrency(CurrencyType.Petals));
        if (addCoinsButton != null)
            addCoinsButton.onClick.AddListener(() => AddCurrency(CurrencyType.Coins));
        if (addRenownButton != null)
            addRenownButton.onClick.AddListener(() => AddCurrency(CurrencyType.Renown));
        if (addGemsButton != null)
            addGemsButton.onClick.AddListener(() => AddCurrency(CurrencyType.Gems));

        if (skip1MinButton != null)
            skip1MinButton.onClick.AddListener(() => SkipTime(60f));
        if (skip1HourButton != null)
            skip1HourButton.onClick.AddListener(() => SkipTime(3600f));
        if (skip8HourButton != null)
            skip8HourButton.onClick.AddListener(() => SkipTime(28800f));

        if (nextPhaseButton != null)
            nextPhaseButton.onClick.AddListener(AdvancePhase);

        if (wipeSaveButton != null)
            wipeSaveButton.onClick.AddListener(WipeSave);
        if (forceSaveButton != null)
            forceSaveButton.onClick.AddListener(ForceSave);

        if (contentPanel != null)
            contentPanel.SetActive(false);
    }

    void TogglePanel()
    {
        if (contentPanel != null)
        {
            bool show = !contentPanel.activeSelf;
            contentPanel.SetActive(show);
            if (show) RefreshDisplay();
        }
    }

    void AddCurrency(CurrencyType type)
    {
        var currency = Services.Get<CurrencyManager>();
        if (currency != null)
        {
            currency.Add(type, currencyGrantAmount);
            Debug.Log($"[Debug] Added {currencyGrantAmount} {type}");
        }
    }

    void SkipTime(float seconds)
    {
        var garden = Services.Get<GardenManager>();
        if (garden != null)
        {
            garden.ApplyOfflineTime(seconds);
            Debug.Log($"[Debug] Skipped {seconds}s of time");
        }
    }

    void AdvancePhase()
    {
        var gm = Services.Get<GameManager>();
        if (gm == null) return;

        int next = (int)gm.CurrentPhase + 1;
        if (next <= (int)GamePhase.Business)
        {
            gm.SetPhase((GamePhase)next);
            EventBus.Publish(new PhaseUnlockedEvent { phase = (GamePhase)next });
            Debug.Log($"[Debug] Advanced to phase: {(GamePhase)next}");
        }

        RefreshDisplay();
    }

    void WipeSave()
    {
        var save = Services.Get<SaveSystem>();
        if (save != null)
        {
            save.DeleteSave();
            Debug.Log("[Debug] Save wiped. Reloading scene...");
            Services.Clear();
            EventBus.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void ForceSave()
    {
        var save = Services.Get<SaveSystem>();
        if (save != null)
        {
            save.Save();
            Debug.Log("[Debug] Forced save.");
        }
    }

    void RefreshDisplay()
    {
        var gm = Services.Get<GameManager>();
        if (phaseText != null && gm != null)
            phaseText.text = $"Phase: {gm.CurrentPhase}";
    }
}