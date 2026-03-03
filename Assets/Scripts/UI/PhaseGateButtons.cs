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
        var phase = GameManager.Instance != null
            ? GameManager.Instance.CurrentPhase
            : GamePhase.Patch;

        // Shop button visible from Shop phase onward
        if (shopButton != null)
            shopButton.SetActive(phase >= GamePhase.Shop);

        // Social button visible from Garden phase onward
        if (socialButton != null)
            socialButton.SetActive(phase >= GamePhase.Garden);

        // Store button always visible (monetization should be accessible)
        // Change this if you want it gated too
        if (storeButton != null)
            storeButton.SetActive(phase >= GamePhase.Garden);
    }
}
