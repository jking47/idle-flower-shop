using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds a label and tooltip to the watering can dock area.
/// Attach to the WateringCanDock GameObject.
/// Builds label and drag hint programmatically.
/// </summary>
public class WateringCanDock : MonoBehaviour
{
    [Header("Label Settings")]
    [SerializeField] string dockLabel = "Water";
    [SerializeField] string dragHint = "Drag over flowers to speed growth!";
    [SerializeField] int labelFontSize = 16;
    [SerializeField] int hintFontSize = 12;

    [Header("Colors")]
    [SerializeField] Color labelColor = new Color(0.7f, 0.82f, 0.9f);
    [SerializeField] Color hintColor = new Color(0.55f, 0.6f, 0.7f);

    GameObject hintObject;
    bool hintShown;

    const string HINT_KEY = "WaterDockHintShown";

    void Awake()
    {
        BuildLabel();
        BuildDragHint();

        hintShown = PlayerPrefs.GetInt(HINT_KEY, 0) == 1;
    }

    void OnEnable()
    {
        // Keep hint visible permanently
        if (hintObject != null)
            hintObject.SetActive(true);
    }

    void BuildLabel()
    {
        var labelGo = new GameObject("DockLabel");
        var rt = labelGo.AddComponent<RectTransform>();
        rt.SetParent(transform, false);

        // Place label above the dock content
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 4f);
        rt.sizeDelta = new Vector2(0f, 24f);

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = dockLabel;
        tmp.fontSize = labelFontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = labelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    void BuildDragHint()
    {
        hintObject = new GameObject("DragHint");
        var rt = hintObject.AddComponent<RectTransform>();
        rt.SetParent(transform, false);

        // Place hint below the dock
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -4f);
        rt.sizeDelta = new Vector2(220f, 36f);

        // Background
        var bg = hintObject.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.18f, 0.25f, 0.9f);
        bg.raycastTarget = false;

        // Add outline
        var outline = hintObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.4f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        // Text
        var textGo = new GameObject("HintText");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(6f, 2f);
        textRt.offsetMax = new Vector2(-6f, -2f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = dragHint;
        tmp.fontSize = hintFontSize;
        tmp.color = hintColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        hintObject.SetActive(false);
    }
}