using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building mute toggle button for BGM.
/// Create an empty GameObject under HUD, position it, attach this script.
/// Builds its own button + label in Awake.
/// </summary>
public class MuteToggle : MonoBehaviour
{
    [Header("Size")]
    [SerializeField] float buttonWidth = 44f;
    [SerializeField] float buttonHeight = 32f;
    [SerializeField] int labelFontSize = 16;

    [Header("Colors")]
    [SerializeField] Color bgNormal = new Color(0.22f, 0.25f, 0.34f, 0.9f);
    [SerializeField] Color bgMuted = new Color(0.4f, 0.22f, 0.22f, 0.9f);

    Button button;
    Image bgImage;
    TMP_Text label;
    bool isMuted;

    const string MUTE_SAVE_KEY = "BGM_Muted";

    void Awake()
    {
        BuildUI();
        button.onClick.AddListener(Toggle);

        // Restore saved mute state
        isMuted = PlayerPrefs.GetInt(MUTE_SAVE_KEY, 0) == 1;
        UpdateVisuals();
    }

    void Start()
    {
        // Apply saved state to AudioManager once it's registered
        ApplyMuteState();
    }

    void Toggle()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt(MUTE_SAVE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMuteState();
        UpdateVisuals();
    }

    void ApplyMuteState()
    {
        if (Services.TryGet<AudioManager>(out var audio))
            audio.SetMuted(isMuted);
    }

    void UpdateVisuals()
    {
        if (label != null)
            label.text = isMuted ? "OFF" : "BGM";

        if (bgImage != null)
            bgImage.color = isMuted ? bgMuted : bgNormal;
    }

    void BuildUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);

        // Background image
        bgImage = GetComponent<Image>();
        if (bgImage == null)
            bgImage = gameObject.AddComponent<Image>();
        bgImage.color = bgNormal;

        // Button component
        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();
        button.targetGraphic = bgImage;

        // Subtle outline
        var outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.4f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        // Label
        var textGo = new GameObject("Label");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        label = textGo.AddComponent<TextMeshProUGUI>();
        label.text = "BGM";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = labelFontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.8f, 0.85f, 0.9f);
        label.enableWordWrapping = false;
        label.raycastTarget = false;
    }
}
