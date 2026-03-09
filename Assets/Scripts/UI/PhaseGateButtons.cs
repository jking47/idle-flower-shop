using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gates HUD nav buttons by game phase.
/// Attach to the HUD object. Assign buttons in inspector.
/// Buttons are hidden until their phase is reached.
/// </summary>
public class HUDPhaseGate : MonoBehaviour
{
    [Header("Phase-Gated Buttons")]
    [SerializeField] GameObject shopButton;
    [SerializeField] GameObject socialButton;
    [SerializeField] GameObject storeButton;

    void OnEnable()
    {
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void Start()
    {
        Refresh();
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        Refresh();
    }

    void Refresh()
    {
        var phase = Services.TryGet<GameManager>(out var gm)
            ? gm.CurrentPhase
            : GamePhase.Patch;

        if (shopButton != null)
            shopButton.SetActive(phase >= GamePhase.Shop);

        if (socialButton != null)
            socialButton.SetActive(phase >= GamePhase.Garden);

        // Store always visible — monetization accessible from the start
        if (storeButton != null)
            storeButton.SetActive(true);
    }
}