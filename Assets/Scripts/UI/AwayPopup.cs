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
    /// offlineHarvests is total auto-harvest cycles that ran while away.
    /// </summary>
    public void Show(float secondsAway, double petalsEarned, int offlineHarvests)
    {
        if (secondsAway < minimumAwayTime) return;

        if (titleText != null)
            titleText.text = PickTitle(secondsAway, petalsEarned);

        if (timeAwayText != null)
            timeAwayText.text = $"You were away for {FormatDuration(secondsAway)}";

        if (petalsEarnedText != null)
        {
            if (petalsEarned > 0)
            {
                petalsEarnedText.text = $"+{FormatNumber(petalsEarned)} petals collected";
                petalsEarnedText.gameObject.SetActive(true);
            }
            else
            {
                petalsEarnedText.gameObject.SetActive(false);
            }
        }

        if (flowersBloomedText != null)
        {
            if (offlineHarvests > 0)
            {
                flowersBloomedText.text = offlineHarvests == 1
                    ? "1 flower was harvested for you"
                    : $"{offlineHarvests} flowers were harvested for you";
                flowersBloomedText.gameObject.SetActive(true);
            }
            else
            {
                flowersBloomedText.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(true);
    }

    string PickTitle(float secondsAway, double petalsEarned)
    {
        if (petalsEarned <= 0)
            return "Welcome back!";

        if (secondsAway >= 28800) // 8+ hours
            return "Your garden bloomed while you slept!";

        if (secondsAway >= 3600) // 1+ hour
            return "Your flowers worked hard while you were away!";

        if (secondsAway >= 300) // 5+ minutes
            return "Your garden kept growing!";

        return "Welcome back!";
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
