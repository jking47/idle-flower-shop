using TMPro;
using UnityEngine;

/// <summary>
/// Binds currency values to HUD text elements.
/// Attach to the HUD parent object under Canvas.
/// </summary>
public class HUDController : MonoBehaviour
{
    [SerializeField] TMP_Text petalsText;
    [SerializeField] TMP_Text coinsText;
    [SerializeField] TMP_Text renownText;
    [SerializeField] TMP_Text gemsText;

    [Header("Phase Display")]
    [SerializeField] TMP_Text phaseText;

    void OnEnable()
    {
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void Start()
    {
        RefreshAll();
    }

    void RefreshAll()
    {
        if (!Services.TryGet<CurrencyManager>(out var currency)) return;

        UpdateCurrencyText(petalsText, "Petals", currency.GetBalance(CurrencyType.Petals));
        UpdateCurrencyText(coinsText, "Coins", currency.GetBalance(CurrencyType.Coins));
        UpdateCurrencyText(renownText, "Renown", currency.GetBalance(CurrencyType.Renown));
        UpdateCurrencyText(gemsText, "Gems", currency.GetBalance(CurrencyType.Gems));

        if (phaseText != null && Services.TryGet<GameManager>(out var gm))
        {
            phaseText.text = gm.CurrentPhase.ToString();
        }
    }

    void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        switch (evt.currencyType)
        {
            case CurrencyType.Petals:
                UpdateCurrencyText(petalsText, "Petals", evt.newAmount);
                break;
            case CurrencyType.Coins:
                UpdateCurrencyText(coinsText, "Coins", evt.newAmount);
                break;
            case CurrencyType.Renown:
                UpdateCurrencyText(renownText, "Renown", evt.newAmount);
                break;
            case CurrencyType.Gems:
                UpdateCurrencyText(gemsText, "Gems", evt.newAmount);
                break;
        }
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        if (phaseText != null)
            phaseText.text = evt.phase.ToString();
    }

    void UpdateCurrencyText(TMP_Text field, string label, double amount)
    {
        if (field == null) return;
        field.text = $"{label}: {FormatNumber(amount)}";
    }

    /// <summary>
    /// Formats large numbers for idle game display.
    /// </summary>
    string FormatNumber(double value)
    {
        if (value < 1000) return value.ToString("F0");
        if (value < 1_000_000) return (value / 1000).ToString("F1") + "K";
        if (value < 1_000_000_000) return (value / 1_000_000).ToString("F1") + "M";
        return (value / 1_000_000_000).ToString("F1") + "B";
    }
}