using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug panel for testing and recruiter demos.
/// Self-builds buttons programmatically. Only needs toggleButton wired in inspector.
/// Builds content as a sibling of DebugPanel, not a child, so it won't cover the button.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Header("Toggle Button (wire in inspector)")]
    [SerializeField] Button toggleButton;

    [Header("Settings")]
    [SerializeField] double smallGrant = 100;
    [SerializeField] double largeGrant = 1000;

    GameObject contentPanel;
    TMP_Text phaseText;

    void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        BuildPanel();
        contentPanel.SetActive(false);
    }

    void TogglePanel()
    {
        if (contentPanel != null)
        {
            bool show = !contentPanel.activeSelf;
            contentPanel.SetActive(show);
            if (show) RefreshDisplay();
        }
    }

    // --- Actions ---

    void AddCurrency(CurrencyType type, double amount)
    {
        var currency = Services.Get<CurrencyManager>();
        if (currency != null)
        {
            currency.Add(type, amount);
            Debug.Log($"[Debug] Added {amount} {type}");
            RefreshDisplay();
        }
    }

    void SkipTime(float seconds)
    {
        var garden = Services.Get<GardenManager>();
        if (garden != null)
        {
            garden.ApplyOfflineTime(seconds);
            Debug.Log($"[Debug] Skipped {seconds}s");
        }
    }

    void TriggerPest()
    {
        if (Services.TryGet<PestManager>(out var pests))
            pests.TriggerPestEvent();
        else
            Debug.LogWarning("[Debug] PestManager not found — is it on the GameManager object?");
    }

    void AdvancePhase()
    {
        var gm = Services.Get<GameManager>();
        if (gm == null) return;

        int next = (int)gm.CurrentPhase + 1;
        if (next <= (int)GamePhase.Business)
        {
            gm.SetPhase((GamePhase)next);
            EventBus.Publish(new PhaseUnlockedEvent { phase = (GamePhase)next });
            Debug.Log($"[Debug] Advanced to phase: {(GamePhase)next}");
        }

        RefreshDisplay();
    }

    void WipeSave()
    {
        var save = Services.Get<SaveSystem>();
        if (save != null)
            save.DeleteSave();

        // Clear data stored outside the main save JSON
        if (Services.TryGet<TutorialManager>(out var tutorial))
            tutorial.ResetTutorial();
        // Also remove any legacy plot-unlock PlayerPrefs key that may have survived migration
        PlayerPrefs.DeleteKey(GardenManager.UNLOCK_SAVE_KEY_LEGACY);

        Debug.Log("[Debug] Save wiped. Reloading...");

        var gm = GameManager.Instance;
        if (gm != null)
            gm.PrepareForReset();

        Services.Clear();
        EventBus.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ForceSave()
    {
        var save = Services.Get<SaveSystem>();
        if (save != null)
        {
            save.Save();
            Debug.Log("[Debug] Forced save.");
        }
    }

    void RefreshDisplay()
    {
        var gm = Services.Get<GameManager>();
        if (phaseText != null && gm != null)
            phaseText.text = $"Phase: {gm.CurrentPhase}";
    }

    // --- Build UI ---

    void BuildPanel()
    {
        // Build as sibling of DebugPanel under the same parent (Canvas)
        // so it doesn't cover the toggle button
        contentPanel = new GameObject("DebugContent");
        var panelRt = contentPanel.AddComponent<RectTransform>();
        panelRt.SetParent(transform.parent, false);

        // Position: left half of screen, above the debug button
        panelRt.anchorMin = new Vector2(0f, 0.05f);
        panelRt.anchorMax = new Vector2(0.55f, 0.85f);
        panelRt.offsetMin = new Vector2(10, 0);
        panelRt.offsetMax = new Vector2(-10, 0);

        var panelImg = contentPanel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        // Vertical layout directly on the panel
        var vlg = contentPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // --- Phase display ---
        phaseText = MakeLabel(panelRt, "Phase: --", 22);

        // --- Currency ---
        MakeLabel(panelRt, "— Currency —", 18);
        MakeButtonRow(panelRt, new (string, System.Action)[]
        {
            ($"+{smallGrant} Petals", () => AddCurrency(CurrencyType.Petals, smallGrant)),
            ($"+{largeGrant} Petals", () => AddCurrency(CurrencyType.Petals, largeGrant))
        });
        MakeButtonRow(panelRt, new (string, System.Action)[]
        {
            ($"+{smallGrant} Coins", () => AddCurrency(CurrencyType.Coins, smallGrant)),
            ($"+{largeGrant} Coins", () => AddCurrency(CurrencyType.Coins, largeGrant))
        });
        MakeButtonRow(panelRt, new (string, System.Action)[]
        {
            ($"+{smallGrant} Gems", () => AddCurrency(CurrencyType.Gems, smallGrant)),
            ($"+{smallGrant} Renown", () => AddCurrency(CurrencyType.Renown, smallGrant))
        });

        // --- Time ---
        MakeLabel(panelRt, "— Time Skip —", 18);
        MakeButtonRow(panelRt, new (string, System.Action)[]
        {
            ("1 Min", () => SkipTime(60f)),
            ("1 Hour", () => SkipTime(3600f)),
            ("8 Hours", () => SkipTime(28800f))
        });

        // --- Phase ---
        MakeButton(panelRt, "Next Phase", AdvancePhase);

        // --- Events ---
        MakeLabel(panelRt, "— Events —", 18);
        MakeButton(panelRt, "Trigger Pest", TriggerPest);

        // --- Save ---
        MakeLabel(panelRt, "— Save —", 18);
        MakeButtonRow(panelRt, new (string, System.Action)[]
        {
            ("Force Save", ForceSave),
            ("Wipe + Restart", WipeSave)
        });
    }

    TMP_Text MakeLabel(RectTransform parent, string text, float fontSize)
    {
        var go = new GameObject("Label");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 10;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.8f, 0.8f, 0.8f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    void MakeButton(RectTransform parent, string label, System.Action onClick)
    {
        var go = new GameObject(label);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 44;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.55f, 0.35f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());

        var textGo = new GameObject("Text");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    void MakeButtonRow(RectTransform parent, (string label, System.Action onClick)[] buttons)
    {
        var row = new GameObject("Row");
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 44;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        foreach (var (label, onClick) in buttons)
        {
            var go = new GameObject(label);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(rowRt, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.55f, 0.35f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var textGo = new GameObject("Text");
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.SetParent(rt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
        }
    }
}