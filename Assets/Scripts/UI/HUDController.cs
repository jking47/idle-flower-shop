using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Binds currency values to HUD text elements.
/// Currency values tween smoothly from their previous display value to the new one.
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

    [Header("Tween")]
    [SerializeField] float tweenDuration = 0.4f;

    // Current displayed values (what the text shows right now)
    readonly Dictionary<CurrencyType, double>    displayedValues = new();
    // Running tween coroutines
    readonly Dictionary<CurrencyType, Coroutine> tweens          = new();

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

        SetCurrencyImmediate(CurrencyType.Petals, currency.GetBalance(CurrencyType.Petals));
        SetCurrencyImmediate(CurrencyType.Coins,  currency.GetBalance(CurrencyType.Coins));
        SetCurrencyImmediate(CurrencyType.Gems,   currency.GetBalance(CurrencyType.Gems));

        // Renown hidden until earned
        double renown = currency.GetBalance(CurrencyType.Renown);
        if (renownText != null) renownText.gameObject.SetActive(renown > 0);
        if (renown > 0) SetCurrencyImmediate(CurrencyType.Renown, renown);

        if (phaseText != null && Services.TryGet<GameManager>(out var gm))
            phaseText.text = gm.CurrentPhase.ToString();
    }

    void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        if (evt.currencyType == CurrencyType.Renown)
        {
            if (renownText != null) renownText.gameObject.SetActive(evt.newAmount > 0);
            if (evt.newAmount <= 0) return;
        }

        // Cancel existing tween for this slot
        if (tweens.TryGetValue(evt.currencyType, out var running) && running != null)
            StopCoroutine(running);

        double from = displayedValues.TryGetValue(evt.currencyType, out var prev) ? prev : evt.previousAmount;
        tweens[evt.currencyType] = StartCoroutine(TweenCurrency(evt.currencyType, from, evt.newAmount));
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        if (phaseText != null)
            phaseText.text = evt.phase.ToString();
    }

    IEnumerator TweenCurrency(CurrencyType type, double from, double to)
    {
        float elapsed = 0f;
        while (elapsed < tweenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tweenDuration);
            // Ease-out quad
            float ease = 1f - (1f - t) * (1f - t);
            double current = from + (to - from) * ease;
            displayedValues[type] = current;
            WriteCurrencyText(type, current);
            yield return null;
        }

        displayedValues[type] = to;
        WriteCurrencyText(type, to);
        tweens.Remove(type);
    }

    void SetCurrencyImmediate(CurrencyType type, double value)
    {
        displayedValues[type] = value;
        WriteCurrencyText(type, value);
    }

    void WriteCurrencyText(CurrencyType type, double value)
    {
        switch (type)
        {
            case CurrencyType.Petals: UpdateCurrencyText(petalsText, "Petals", value); break;
            case CurrencyType.Coins:  UpdateCurrencyText(coinsText,  "Coins",  value); break;
            case CurrencyType.Renown: UpdateCurrencyText(renownText, "Renown", value); break;
            case CurrencyType.Gems:   UpdateCurrencyText(gemsText,   "Gems",   value); break;
        }
    }

    void UpdateCurrencyText(TMP_Text field, string label, double amount)
    {
        if (field == null) return;
        field.text = $"{label}: {FormatNumber(amount)}";
    }

    string FormatNumber(double value)
    {
        if (value < 1000)        return value.ToString("F0");
        if (value < 1_000_000)   return (value / 1000).ToString("F1") + "K";
        if (value < 1_000_000_000) return (value / 1_000_000).ToString("F1") + "M";
        return (value / 1_000_000_000).ToString("F1") + "B";
    }
}
