using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI card for one active order slot.
/// Assign to a prefab; ShopPanel spawns one per slot and calls Bind().
/// 
/// Inspector setup:
///   - emptyState: "Waiting for order..." placeholder GameObject
///   - activeState: root of the populated order card
///   - requirementsContainer: vertical layout group for ShopRequirementRow prefabs
///   - timerFill: Image with fillMethod Horizontal or Radial360
///   - rewardText: shows live market-adjusted payout
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

    int slotIndex;
    ActiveOrder trackedOrder;
    readonly List<ShopRequirementRow> rows = new();

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
        if (trackedOrder == null || trackedOrder.IsExpired) return;

        float t = trackedOrder.timeRemaining;
        if (timerText)
        {
            timerText.text = FormatTime(t);
            timerText.color = t <= 10f ? Color.red : Color.white;
        }
        if (timerFill) timerFill.fillAmount = trackedOrder.TimerProgress;

        // Refresh live reward display each frame (market prices shift over time)
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

    /// <summary>
    /// Refresh button state and requirement counts. Called on inventory change.
    /// </summary>
    public void RefreshFillButton()
    {
        if (fillButton == null || trackedOrder == null) return;

        var inv = Services.Get<InventoryManager>();
        bool canFill = inv != null && CanFillFromInventory(inv);

        fillButton.interactable = canFill;
        if (fillButtonText) fillButtonText.text = canFill ? "Fill Order" : "Need More";

        foreach (var row in rows) row.Refresh();
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
            rows.Add(row);
        }
    }

    void OnFillClicked() => Services.Get<ShopManager>()?.TryFillOrder(slotIndex);

    void ShowEmpty()
    {
        trackedOrder = null;
        emptyState?.SetActive(true);
        activeState?.SetActive(false);
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return m > 0 ? $"{m}:{s:D2}" : $"{s}s";
    }
}