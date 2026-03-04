using UnityEngine;
using TMPro;

public class PhaseHUDLabel : MonoBehaviour
{
    private TextMeshProUGUI label;
    private CurrencyManager currency;

    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseChanged);
        EventBus.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseChanged);
        EventBus.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
    }

    private void Start()
    {
        currency = Services.Get<CurrencyManager>();
        Refresh();
    }

    private void OnPhaseChanged(PhaseUnlockedEvent e) => Refresh();
    private void OnCurrencyChanged(CurrencyChangedEvent e) => Refresh();

    private void Refresh()
    {
        var phase = GameManager.Instance.CurrentPhase;
        string current = $"Phase: {PhaseName(phase)}";

        string next = phase switch
        {
            GamePhase.Patch => $"Next: The Garden ({(int)currency.GetBalance(CurrencyType.Petals)}/50 petals)",
            GamePhase.Garden => $"Next: The Shop ({(int)currency.GetBalance(CurrencyType.Petals)}/500 petals)",
            GamePhase.Shop => $"Next: The Business ({(int)currency.GetBalance(CurrencyType.Coins)}/1000 coins)",
            _ => "All phases unlocked!"
        };

        label.text = $"{current}\n{next}";
    }

    private string PhaseName(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Patch => "The Patch",
            GamePhase.Garden => "The Garden",
            GamePhase.Shop => "The Shop",
            GamePhase.Business => "The Business",
            _ => phase.ToString()
        };
    }
}