using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI card for one active order slot.
/// Shows order details when active, next-spawn countdown when empty.
///
/// CompactCardLayout() runs in Awake to override the prefab's large font sizes and
/// spacings so the card fits a grid cell. It also enables childControlHeight on
/// ActiveState's VerticalLayoutGroup and sets explicit LayoutElement preferred
/// heights — without this, the VLG ignores font size and uses the prefab sizeDelta
/// values (~96px per section) which overflow any compact cell.
/// </summary>
public class ShopOrderUI : MonoBehaviour
{
    [Header("States")]
    [SerializeField] GameObject emptyState;
    [SerializeField] GameObject activeState;

    [Header("Order Info")]
    [SerializeField] TMP_Text orderNameText;
    [SerializeField] TMP_Text rewardText;

    [Header("Timer")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] Image timerFill;

    [Header("Requirements")]
    [SerializeField] Transform requirementsContainer;
    [SerializeField] ShopRequirementRow requirementRowPrefab;

    [Header("Actions")]
    [SerializeField] Button fillButton;
    [SerializeField] TMP_Text fillButtonText;

    [Header("Empty State Text")]
    [SerializeField] TMP_Text emptyText;

    int slotIndex;
    ActiveOrder trackedOrder;
    readonly List<ShopRequirementRow> rows = new();

    void Awake()
    {
        CompactCardLayout();
    }

    void OnEnable() => EventBus.Subscribe<MarketUpdatedEvent>(OnMarketUpdated);
    void OnDisable() => EventBus.Unsubscribe<MarketUpdatedEvent>(OnMarketUpdated);

    void OnMarketUpdated(MarketUpdatedEvent evt)
    {
        foreach (var row in rows) row.Refresh();
    }

    void Start()
    {
        fillButton.onClick.AddListener(OnFillClicked);
        ShowEmpty();
    }

    void Update()
    {
        if (trackedOrder == null)
        {
            UpdateEmptyText();
            return;
        }

        if (trackedOrder.IsExpired) return;

        float t = trackedOrder.timeRemaining;
        if (timerText)
        {
            timerText.text = FormatTime(t);
            timerText.color = t <= 10f ? Color.red : Color.white;
        }
        if (timerFill) timerFill.fillAmount = trackedOrder.TimerProgress;

        if (rewardText)
        {
            double reward = Services.Get<ShopManager>()?.GetCurrentReward(slotIndex) ?? 0;
            rewardText.text = $"+{reward:0} coins";
        }
    }

    public void Bind(int idx, ActiveOrder order)
    {
        slotIndex = idx;
        trackedOrder = order;

        if (order == null) { ShowEmpty(); return; }

        emptyState.SetActive(false);
        activeState.SetActive(true);

        if (orderNameText) orderNameText.text = order.data.displayName;

        BuildRequirementRows(order);
        RefreshFillButton();
    }

    public void RefreshFillButton()
    {
        if (fillButton == null || trackedOrder == null) return;

        var inv = Services.Get<InventoryManager>();
        bool canFill = inv != null && CanFillFromInventory(inv);

        fillButton.interactable = canFill;

        if (fillButtonText)
            fillButtonText.text = canFill ? "Fill Order" : "Need More Flowers";
    }

    // --- Layout compaction ---

    void CompactCardLayout()
    {
        // Root VLG — stretch children to fill card width
        var rootVlg = GetComponent<VerticalLayoutGroup>();
        if (rootVlg != null)
        {
            rootVlg.padding               = new RectOffset(8, 8, 8, 8);
            rootVlg.childControlWidth     = true;
            rootVlg.childForceExpandWidth = true;
        }

        // ActiveState VLG — use preferred heights, stretch all children to full width
        if (activeState != null)
        {
            var vlg = activeState.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight     = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth      = true;
                vlg.childForceExpandWidth  = true;
                vlg.spacing                = 5f;
            }

            // Any HLG rows inside ActiveState (e.g. reward row, timer row) also need
            // childControlWidth so they respect the width set by the parent VLG and
            // don't revert to their prefab sizeDelta, which causes text to clip left.
            foreach (var hlg in activeState.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                hlg.childControlWidth      = true;
                hlg.childForceExpandWidth  = false;
                hlg.childControlHeight     = true;
                hlg.childForceExpandHeight = false;
            }
        }

        // Order name
        if (orderNameText)
        {
            orderNameText.fontSize           = 24f;
            orderNameText.enableWordWrapping = false;
            orderNameText.overflowMode       = TextOverflowModes.Ellipsis;
            SetPreferredHeight(orderNameText.transform, 30f);
        }

        // Reward text — target its container row if it has one
        if (rewardText)
        {
            rewardText.fontSize           = 20f;
            rewardText.enableWordWrapping = false;
            var rewardTarget = GetLayoutTarget(rewardText.transform);
            SetPreferredHeight(rewardTarget, 26f);
        }

        // Requirements container — cap height, ensure rows fill width
        if (requirementsContainer)
        {
            SetPreferredHeight(requirementsContainer, 140f);
            var reqVlg = requirementsContainer.GetComponent<VerticalLayoutGroup>();
            if (reqVlg != null)
            {
                reqVlg.childControlWidth     = true;
                reqVlg.childForceExpandWidth = true;
            }
        }

        // Timer row
        if (timerText != null)
        {
            var timerRow = timerText.transform.parent;
            if (timerRow != null)
            {
                foreach (var t in timerRow.GetComponentsInChildren<TMP_Text>())
                {
                    t.fontSize           = 20f;
                    t.enableWordWrapping = false;
                }
                SetPreferredHeight(timerRow, 26f);
            }
        }

        if (fillButton)     SetPreferredHeight(fillButton.transform, 44f);
        if (fillButtonText) fillButtonText.fontSize = 20f;

        if (emptyText)
        {
            emptyText.fontSize           = 22f;
            emptyText.enableWordWrapping = false;
            emptyText.overflowMode       = TextOverflowModes.Ellipsis;
        }
    }

    /// <summary>
    /// Returns the appropriate layout target for a TMP_Text: if the TMP is a child
    /// of a non-ActiveState container, return that container (the real layout element);
    /// otherwise return the TMP's own transform.
    /// </summary>
    Transform GetLayoutTarget(Transform tmp)
    {
        var parent = tmp.parent;
        if (parent != null && parent != activeState?.transform)
            return parent;
        return tmp;
    }

    /// <summary>Gets or adds a LayoutElement and sets its preferred height.</summary>
    void SetPreferredHeight(Transform t, float height)
    {
        var le = t.GetComponent<LayoutElement>();
        if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    // --- Internal ---

    bool CanFillFromInventory(InventoryManager inv)
    {
        foreach (var req in trackedOrder.data.requirements)
        {
            if (inv.GetCount(req.flower.name) < req.count)
                return false;
        }
        return true;
    }

    void BuildRequirementRows(ActiveOrder order)
    {
        foreach (var r in rows) Destroy(r.gameObject);
        rows.Clear();

        foreach (var req in order.data.requirements)
        {
            var row = Instantiate(requirementRowPrefab, requirementsContainer);
            row.Set(req.flower, req.count);

            // Row HLG: distribute width properly, don't force-expand any single element
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childControlWidth      = true;
                hlg.childForceExpandWidth  = false;
                hlg.childControlHeight     = true;
                hlg.childForceExpandHeight = false;
            }

            // Flower icon — fixed square, larger for readability
            foreach (var img in row.GetComponentsInChildren<Image>())
            {
                img.preserveAspect = true;
                var le = img.GetComponent<LayoutElement>() ?? img.gameObject.AddComponent<LayoutElement>();
                le.minWidth        = 28f;
                le.minHeight       = 28f;
                le.preferredWidth  = 28f;
                le.preferredHeight = 28f;
            }

            // Text labels — readable size, no mid-word wrapping
            foreach (var txt in row.GetComponentsInChildren<TMP_Text>())
            {
                txt.fontSize           = 18f;
                txt.enableWordWrapping = false;
                txt.overflowMode       = TextOverflowModes.Ellipsis;
            }

            // Cap row height so 4 requirements fit the 140px container budget
            SetPreferredHeight(row.transform, 32f);

            rows.Add(row);
        }
    }

    void OnFillClicked() => Services.Get<ShopManager>()?.TryFillOrder(slotIndex);

    void ShowEmpty()
    {
        trackedOrder = null;
        emptyState?.SetActive(true);
        activeState?.SetActive(false);
        UpdateEmptyText();
    }

    void UpdateEmptyText()
    {
        if (emptyText == null) return;

        Services.TryGet<ShopManager>(out var shop);
        float timeLeft = shop?.TimeUntilNextSpawn ?? -1f;

        emptyText.text = timeLeft >= 0f
            ? $"Next customer in {FormatTime(timeLeft)}"
            : "Awaiting customer...";
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return m > 0 ? $"{m}:{s:D2}" : $"{s}s";
    }
}
