using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen phase unlock announcement with button highlight on dismiss.
/// Create as a panel under Canvas with a dark semi-transparent background.
/// Disables itself in Awake; shown automatically on PhaseUnlockedEvent.
/// </summary>
public class PhasePopup : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Button dismissButton;
    [SerializeField] float autoHideDelay = 8f;

    [Header("Button Highlight (assign from HUD)")]
    [Tooltip("Assign the Shop, Social, Upgrades buttons here to pulse them on unlock")]
    [SerializeField] GameObject shopButton;
    [SerializeField] GameObject socialButton;
    [SerializeField] GameObject upgradesButton;

    [Header("Highlight Settings")]
    [SerializeField] float highlightPulses = 4;
    [SerializeField] float highlightPulseDuration = 0.4f;
    [SerializeField] Color highlightColor = new Color(0.4f, 1f, 0.5f);

    GamePhase currentPhase;

    void Awake()
    {
        EventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);

        if (dismissButton != null)
            dismissButton.onClick.AddListener(OnDismissClicked);
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
        currentPhase = phase;
        gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = GetTitle(phase);
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = GetPhaseColor(phase);
        }

        if (descriptionText != null)
        {
            descriptionText.text = GetDescription(phase);
            descriptionText.fontSize = 22;
            descriptionText.color = new Color(0.85f, 0.88f, 0.9f);
        }

        if (Services.TryGet<GameJuice>(out var juice))
            juice.PlayUnlock();

        StopAllCoroutines();
        StartCoroutine(AutoHide(phase));
    }

    void OnDismissClicked()
    {
        HideAndHighlight(currentPhase);
    }

    void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    void HideAndHighlight(GamePhase phase)
    {
        GameObject target = GetHighlightTarget(phase);
        Hide();

        // Run pulse on GameJuice since this GameObject is now inactive
        if (target != null && Services.TryGet<GameJuice>(out var juice))
            juice.StartCoroutine(PulseButton(target));
    }

    IEnumerator AutoHide(GamePhase phase)
    {
        yield return new WaitForSeconds(autoHideDelay);
        HideAndHighlight(phase);
    }

    GameObject GetHighlightTarget(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Garden => upgradesButton,
            GamePhase.Shop => shopButton,
            GamePhase.Business => shopButton,
            _ => null
        };
    }

    IEnumerator PulseButton(GameObject target)
    {
        if (target == null) yield break;

        var image = target.GetComponent<Image>();
        if (image == null) yield break;

        Color original = image.color;
        Vector3 originalScale = target.transform.localScale;
        Vector3 bigScale = originalScale * 1.15f;

        for (int i = 0; i < highlightPulses; i++)
        {
            float half = highlightPulseDuration * 0.5f;

            // Pulse up — color + scale
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin((elapsed / half) * Mathf.PI * 0.5f);
                image.color = Color.Lerp(original, highlightColor, t);
                target.transform.localScale = Vector3.Lerp(originalScale, bigScale, t);
                yield return null;
            }

            // Pulse down
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin((elapsed / half) * Mathf.PI * 0.5f);
                image.color = Color.Lerp(highlightColor, original, t);
                target.transform.localScale = Vector3.Lerp(bigScale, originalScale, t);
                yield return null;
            }
        }

        image.color = original;
        target.transform.localScale = originalScale;
    }

    string GetTitle(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Garden   => "The Garden",
            GamePhase.Shop     => "The Flower Shop",
            GamePhase.Business => "The Business",
            _                  => "New Phase!"
        };
    }

    string GetDescription(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Garden   => "Your garden is growing!\n\n" +
                                  "New plots are available to unlock.\n" +
                                  "Check Upgrades for grow speed and yield boosts!",
            GamePhase.Shop     => "Your shop is open for business!\n\n" +
                                  "Tap the Shop button to see customer orders.\n" +
                                  "Fill orders to earn coins!",
            GamePhase.Business => "You've built a real business!\n\n" +
                                  "Staff management and bulk orders\n" +
                                  "are coming in a future update.",
            _                  => "New features unlocked!"
        };
    }

    Color GetPhaseColor(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Garden   => new Color(0.5f, 0.9f, 0.5f),   // green
            GamePhase.Shop     => new Color(0.9f, 0.8f, 0.4f),   // gold
            GamePhase.Business => new Color(0.6f, 0.7f, 1f),     // blue
            _                  => Color.white
        };
    }
}