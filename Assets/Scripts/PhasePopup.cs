using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen phase unlock announcement.
/// Create as a panel under Canvas with a dark semi-transparent background.
/// Disables itself in Awake; shown automatically on PhaseUnlockedEvent.
/// </summary>
public class PhasePopup : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Button dismissButton;
    [SerializeField] float autoHideDelay = 5f;

    void Awake()
    {
        // Subscribe in Awake, not OnEnable — popup starts inactive
        // but still needs to hear phase events
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);

        if (dismissButton != null)
            dismissButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    void OnPhaseUnlocked(PhaseUnlockedEvent evt)
    {
        Show(evt.phase);
    }

    void Show(GamePhase phase)
    {
        gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = GetTitle(phase);
        if (descriptionText != null)
            descriptionText.text = GetDescription(phase);

        StopAllCoroutines();
        StartCoroutine(AutoHide());
    }

    void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideDelay);
        Hide();
    }

    string GetTitle(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Garden   => "Garden Unlocked!",
            GamePhase.Shop     => "Shop Unlocked!",
            GamePhase.Business => "Business Unlocked!",
            _                  => "New Phase!"
        };
    }

    string GetDescription(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Garden   => "Auto-growing flower beds are now available.\nPlant flowers and watch them grow!",
            GamePhase.Shop     => "Your flower shop is open for business!\nFill customer orders to earn coins.",
            GamePhase.Business => "Time to expand! Hire staff and take bulk orders.",
            _                  => "New features unlocked!"
        };
    }
}
