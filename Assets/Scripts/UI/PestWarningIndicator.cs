using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD element that appears while a pest event is active.
/// Alerts players who aren't looking at the garden that something needs attention.
/// Pulses color to draw the eye. Attach to a GameObject in the HUD — starts
/// inactive, shown automatically on PestEventStartedEvent.
/// </summary>
public class PestWarningIndicator : MonoBehaviour
{
    [SerializeField] TMP_Text warningText;
    [SerializeField] Image background;
    [SerializeField] string warningMessage = "Pests attacking!";

    [Header("Pulse")]
    [SerializeField] Color colorA = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] Color colorB = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] float pulseSpeed = 2.5f;

    Coroutine pulseCoroutine;

    void Awake()
    {
        EventBus.Subscribe<PestEventStartedEvent>(OnPestEventStarted);
        EventBus.Subscribe<PestEventEndedEvent>(OnPestEventEnded);
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<PestEventStartedEvent>(OnPestEventStarted);
        EventBus.Unsubscribe<PestEventEndedEvent>(OnPestEventEnded);
    }

    void OnPestEventStarted(PestEventStartedEvent evt)
    {
        if (warningText != null) warningText.text = warningMessage;
        gameObject.SetActive(true);

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseBackground());
    }

    void OnPestEventEnded(PestEventEndedEvent evt)
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        gameObject.SetActive(false);
    }

    IEnumerator PulseBackground()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            if (background != null)
                background.color = Color.Lerp(colorA, colorB, t);
            yield return null;
        }
    }
}
