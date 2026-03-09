using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-blocking achievement notification banner.
/// Call AchievementToast.Show() to display a self-destroying popup.
/// </summary>
public class AchievementToast : MonoBehaviour
{
    public static void Show(string title, string detail, double renownAwarded)
    {
        var go = new GameObject("AchievementToast");
        var toast = go.AddComponent<AchievementToast>();
        toast.StartCoroutine(toast.Run(title, detail, renownAwarded));
    }

    IEnumerator Run(string title, string detail, double renown)
    {
        // Find or create a canvas to attach to
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) { Destroy(gameObject); yield break; }

        // Root panel
        var panelGo = new GameObject("ToastPanel");
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.SetParent(canvas.transform, false);

        // Anchor to top-center, slide in from above
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.sizeDelta = new Vector2(420f, 80f);
        panelRt.anchoredPosition = new Vector2(0f, 0f);
        panelRt.SetAsLastSibling();

        // Background
        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.18f, 0.26f, 0.95f);

        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.8f, 0.4f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var cg = panelGo.AddComponent<CanvasGroup>();

        // Left gold bar accent
        var accent = new GameObject("Accent");
        var accentRt = accent.AddComponent<RectTransform>();
        accentRt.SetParent(panelRt, false);
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.anchoredPosition = Vector2.zero;
        accentRt.sizeDelta = new Vector2(5f, 0f);
        accent.AddComponent<Image>().color = new Color(0.9f, 0.8f, 0.3f);

        // Trophy label
        MakeText(panelRt, "🏆", 28, new Vector2(0f, 0f), new Vector2(0.12f, 1f),
            new Color(0.9f, 0.8f, 0.3f), TextAlignmentOptions.Center, false);

        // Title
        MakeText(panelRt, title, 20, new Vector2(0.12f, 0.5f), new Vector2(0.75f, 1f),
            Color.white, TextAlignmentOptions.BottomLeft, true);

        // Detail
        MakeText(panelRt, detail, 15, new Vector2(0.12f, 0f), new Vector2(0.75f, 0.5f),
            new Color(0.7f, 0.85f, 0.7f), TextAlignmentOptions.TopLeft, true);

        // Renown badge
        if (renown > 0)
        {
            MakeText(panelRt, $"+{renown:F0} ✨", 16, new Vector2(0.75f, 0f), new Vector2(1f, 1f),
                new Color(0.8f, 0.9f, 1f), TextAlignmentOptions.Center, true);
        }

        // Animate: slide down from off-screen, hold, fade out
        float slideTime = 0.25f;
        float holdTime  = 3f;
        float fadeTime  = 0.4f;
        float panelH    = panelRt.sizeDelta.y;

        // Slide in
        float elapsed = 0f;
        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideTime);
            panelRt.anchoredPosition = new Vector2(0f, -panelH * t);
            yield return null;
        }
        panelRt.anchoredPosition = new Vector2(0f, -panelH);

        yield return new WaitForSeconds(holdTime);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - elapsed / fadeTime;
            yield return null;
        }

        Destroy(panelGo);
        Destroy(gameObject);
    }

    static TMP_Text MakeText(RectTransform parent, string text, float size,
        Vector2 anchorMin, Vector2 anchorMax, Color color,
        TextAlignmentOptions alignment, bool wordWrap)
    {
        var go = new GameObject("T");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(6f, 4f);
        rt.offsetMax = new Vector2(-6f, -4f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = wordWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }
}
