using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "While you were away" popup shown on load when offline progress is applied.
/// Displays a summary of what was earned. Attach to a panel under Canvas.
/// </summary>
public class AwayPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text timeAwayText;
    [SerializeField] TMP_Text petalsEarnedText;
    [SerializeField] TMP_Text flowersBloomedText;
    [SerializeField] Button collectButton;

    [Header("Settings")]
    [Tooltip("Minimum seconds away before showing the popup")]
    [SerializeField] float minimumAwayTime = 60f;

    void Awake()
    {
        Services.Register(this);

        if (collectButton != null)
            collectButton.onClick.AddListener(Close);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by SaveSystem after offline progress is calculated.
    /// </summary>
    public void Show(float secondsAway, double petalsEarned, int flowersBloomed)
    {
        if (secondsAway < minimumAwayTime) return;

        if (titleText != null)
            titleText.text = "While you were away...";

        if (timeAwayText != null)
            timeAwayText.text = FormatDuration(secondsAway);

        if (petalsEarnedText != null)
        {
            if (petalsEarned > 0)
            {
                petalsEarnedText.text = $"+{FormatNumber(petalsEarned)} petals earned";
                petalsEarnedText.gameObject.SetActive(true);
            }
            else
            {
                petalsEarnedText.gameObject.SetActive(false);
            }
        }

        if (flowersBloomedText != null)
        {
            if (flowersBloomed > 0)
            {
                flowersBloomedText.text = $"{flowersBloomed} flower{(flowersBloomed > 1 ? "s" : "")} bloomed";
                flowersBloomedText.gameObject.SetActive(true);
            }
            else
            {
                flowersBloomedText.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(true);
    }

    void Close()
    {
        gameObject.SetActive(false);
    }

    string FormatDuration(float seconds)
    {
        if (seconds < 60) return "Less than a minute";
        if (seconds < 3600) return $"{seconds / 60:F0} minutes";
        if (seconds < 86400) return $"{seconds / 3600:F1} hours";
        return $"{seconds / 86400:F1} days";
    }

    string FormatNumber(double value)
    {
        if (value < 1000) return value.ToString("F0");
        if (value < 1_000_000) return (value / 1000).ToString("F1") + "K";
        return (value / 1_000_000).ToString("F1") + "M";
    }
}
