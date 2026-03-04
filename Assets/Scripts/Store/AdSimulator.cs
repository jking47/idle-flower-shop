using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simulates a fake mobile ad with a cheesy flower-themed pitch.
/// Call AdSimulator.Show() to display. Self-builds and self-destroys.
/// </summary>
public class AdSimulator : MonoBehaviour
{
    static readonly string[] adHeadlines = new[]
    {
        "FLOWER POWER DELUXE",
        "MEGA BLOOM GARDEN 3",
        "IDLE FLOWER TYCOON 2",
        "PETAL CRUSH SAGA",
        "GARDEN WARS: BLOOM BATTLE"
    };

    static readonly string[] adTaglines = new[]
    {
        "Can YOU reach level 100?!",
        "Mom vs Dad! Who grows better flowers?",
        "99% of players can't solve this garden!",
        "WARNING: Highly addictive!",
        "Doctors HATE this gardening trick!"
    };

    static readonly string[] adCtas = new[]
    {
        "DOWNLOAD FREE NOW!!!",
        "INSTALL - 100% FREE",
        "PLAY NOW - NO WIFI NEEDED",
        "TAP TO GROW TODAY!!!"
    };

    public static void Show(MonoBehaviour host, float duration, Action onComplete)
    {
        var go = new GameObject("FakeAdOverlay");
        var sim = go.AddComponent<AdSimulator>();
        sim.StartCoroutine(sim.RunAd(host, duration, onComplete));
    }

    IEnumerator RunAd(MonoBehaviour host, float duration, Action onComplete)
    {
        // Find canvas
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            onComplete?.Invoke();
            Destroy(gameObject);
            yield break;
        }

        // Full-screen overlay
        var overlay = new GameObject("AdOverlay");
        var overlayRt = overlay.AddComponent<RectTransform>();
        overlayRt.SetParent(canvas.transform, false);
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        overlayRt.SetAsLastSibling();

        // Block all input behind the ad
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0.05f, 0.08f, 0.15f, 1f);

        // "AD" label top-left
        var adLabel = MakeText(overlayRt, "AD", 14, TextAlignmentOptions.TopLeft,
            new Color(1f, 1f, 1f, 0.4f));
        var adLabelRt = adLabel.GetComponent<RectTransform>();
        adLabelRt.anchorMin = new Vector2(0, 1);
        adLabelRt.anchorMax = new Vector2(0, 1);
        adLabelRt.pivot = new Vector2(0, 1);
        adLabelRt.anchoredPosition = new Vector2(12, -12);
        adLabelRt.sizeDelta = new Vector2(40, 24);

        // Countdown top-right
        var timerText = MakeText(overlayRt, "", 16, TextAlignmentOptions.TopRight,
            new Color(1f, 1f, 0.5f, 0.8f));
        var timerRt = timerText.GetComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(1, 1);
        timerRt.anchorMax = new Vector2(1, 1);
        timerRt.pivot = new Vector2(1, 1);
        timerRt.anchoredPosition = new Vector2(-12, -12);
        timerRt.sizeDelta = new Vector2(100, 24);

        // Headline
        string headline = adHeadlines[UnityEngine.Random.Range(0, adHeadlines.Length)];
        var headlineText = MakeText(overlayRt, headline, 30, TextAlignmentOptions.Center,
            Color.yellow);
        var headlineRt = headlineText.GetComponent<RectTransform>();
        headlineRt.anchorMin = new Vector2(0.05f, 0.78f);
        headlineRt.anchorMax = new Vector2(0.95f, 0.88f);
        headlineRt.offsetMin = Vector2.zero;
        headlineRt.offsetMax = Vector2.zero;
        headlineText.fontStyle = FontStyles.Bold;

        // Growing flower area (center) — use a simple pulsing green square as "flower"
        var flowerContainer = new GameObject("FlowerArea");
        var flowerRt = flowerContainer.AddComponent<RectTransform>();
        flowerRt.SetParent(overlayRt, false);
        flowerRt.anchorMin = new Vector2(0.2f, 0.35f);
        flowerRt.anchorMax = new Vector2(0.8f, 0.72f);
        flowerRt.offsetMin = Vector2.zero;
        flowerRt.offsetMax = Vector2.zero;

        // "Soil" bar at bottom of flower area
        var soil = new GameObject("Soil");
        var soilRt = soil.AddComponent<RectTransform>();
        soilRt.SetParent(flowerRt, false);
        soilRt.anchorMin = new Vector2(0.1f, 0f);
        soilRt.anchorMax = new Vector2(0.9f, 0.08f);
        soilRt.offsetMin = Vector2.zero;
        soilRt.offsetMax = Vector2.zero;
        var soilImg = soil.AddComponent<Image>();
        soilImg.color = new Color(0.35f, 0.25f, 0.15f);

        // Stem (grows upward during ad)
        var stem = new GameObject("Stem");
        var stemRt = stem.AddComponent<RectTransform>();
        stemRt.SetParent(flowerRt, false);
        stemRt.anchorMin = new Vector2(0.48f, 0.08f);
        stemRt.anchorMax = new Vector2(0.52f, 0.08f);
        stemRt.pivot = new Vector2(0.5f, 0f);
        stemRt.offsetMin = Vector2.zero;
        stemRt.offsetMax = Vector2.zero;
        var stemImg = stem.AddComponent<Image>();
        stemImg.color = new Color(0.2f, 0.6f, 0.2f);

        // Flower head (appears partway through)
        var head = new GameObject("FlowerHead");
        var headRt = head.AddComponent<RectTransform>();
        headRt.SetParent(flowerRt, false);
        headRt.anchorMin = new Vector2(0.35f, 0.6f);
        headRt.anchorMax = new Vector2(0.65f, 0.85f);
        headRt.offsetMin = Vector2.zero;
        headRt.offsetMax = Vector2.zero;
        var headImg = head.AddComponent<Image>();
        headImg.color = new Color(1f, 0.4f, 0.6f);
        head.SetActive(false);

        // Center (pistil)
        var center = new GameObject("Center");
        var centerRt = center.AddComponent<RectTransform>();
        centerRt.SetParent(headRt, false);
        centerRt.anchorMin = new Vector2(0.3f, 0.3f);
        centerRt.anchorMax = new Vector2(0.7f, 0.7f);
        centerRt.offsetMin = Vector2.zero;
        centerRt.offsetMax = Vector2.zero;
        var centerImg = center.AddComponent<Image>();
        centerImg.color = new Color(1f, 0.85f, 0.2f);

        // Tagline below flower
        string tagline = adTaglines[UnityEngine.Random.Range(0, adTaglines.Length)];
        var taglineText = MakeText(overlayRt, tagline, 20, TextAlignmentOptions.Center,
            Color.white);
        var taglineRt = taglineText.GetComponent<RectTransform>();
        taglineRt.anchorMin = new Vector2(0.05f, 0.25f);
        taglineRt.anchorMax = new Vector2(0.95f, 0.33f);
        taglineRt.offsetMin = Vector2.zero;
        taglineRt.offsetMax = Vector2.zero;
        taglineText.fontStyle = FontStyles.Italic;

        // Fake CTA button at bottom
        var ctaGo = new GameObject("CTA");
        var ctaRt = ctaGo.AddComponent<RectTransform>();
        ctaRt.SetParent(overlayRt, false);
        ctaRt.anchorMin = new Vector2(0.1f, 0.08f);
        ctaRt.anchorMax = new Vector2(0.9f, 0.18f);
        ctaRt.offsetMin = Vector2.zero;
        ctaRt.offsetMax = Vector2.zero;
        var ctaImg = ctaGo.AddComponent<Image>();
        ctaImg.color = new Color(0.1f, 0.8f, 0.2f);

        string cta = adCtas[UnityEngine.Random.Range(0, adCtas.Length)];
        var ctaText = MakeText(ctaRt, cta, 22, TextAlignmentOptions.Center, Color.white);
        var ctaTextRt = ctaText.GetComponent<RectTransform>();
        ctaTextRt.anchorMin = Vector2.zero;
        ctaTextRt.anchorMax = Vector2.one;
        ctaTextRt.offsetMin = Vector2.zero;
        ctaTextRt.offsetMax = Vector2.zero;
        ctaText.fontStyle = FontStyles.Bold;

        // --- Animate ---
        float elapsed = 0f;
        bool flowerShown = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Grow stem
            float stemHeight = Mathf.Lerp(0f, 0.55f, Mathf.Min(t * 1.8f, 1f));
            stemRt.anchorMax = new Vector2(0.52f, 0.08f + stemHeight);

            // Show flower head at 50%
            if (t > 0.5f && !flowerShown)
            {
                head.SetActive(true);
                flowerShown = true;
            }

            // Pulse flower head
            if (flowerShown)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 4f) * 0.08f;
                headRt.localScale = Vector3.one * pulse;
            }

            // Pulse CTA button
            float ctaPulse = 1f + Mathf.Sin(elapsed * 3f) * 0.04f;
            ctaRt.localScale = Vector3.one * ctaPulse;

            // Countdown timer
            int remaining = Mathf.CeilToInt(duration - elapsed);
            timerText.text = remaining > 0 ? $"Skip in {remaining}s" : "Closing...";

            yield return null;
        }

        // Cleanup
        Destroy(overlay);
        Destroy(gameObject);
        onComplete?.Invoke();
    }

    TMP_Text MakeText(RectTransform parent, string text, float size,
        TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject("Text");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }
}
