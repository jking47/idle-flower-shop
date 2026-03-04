using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Draggable watering can tool. Player drags the can icon over growing plots
/// to speed up growth. Water depletes with use and refills over time.
/// Unlockable via upgrade. Attach to the draggable can icon UI element.
/// </summary>
public class WateringCan : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Watering Settings")]
    [Tooltip("Growth speed multiplier while being watered")]
    [SerializeField] float wateringSpeedMultiplier = 3f;

    [Tooltip("Water consumed per second while watering")]
    [SerializeField] float waterDrainRate = 10f;

    [Tooltip("Water refilled per second while not watering")]
    [SerializeField] float waterRefillRate = 5f;

    [Tooltip("Maximum water capacity")]
    [SerializeField] float maxWater = 100f;

    [Header("UI")]
    [SerializeField] Image canIcon;
    [SerializeField] Image waterFillBar;
    [SerializeField] TMP_Text waterText;

    [Header("Drag Settings")]
    [SerializeField] Canvas parentCanvas;

    float currentWater;
    bool isUnlocked;
    bool isDragging;
    RectTransform rectTransform;
    CanvasGroup canvasGroup;
    LayoutElement layoutElement;

    // Dock references for reparenting during drag
    Transform dockParent;
    int dockSiblingIndex;

    // Track which plots are being watered this frame
    readonly HashSet<FlowerBed> wateredPlots = new();

    public bool IsUnlocked => isUnlocked;
    public bool IsDragging => isDragging;
    public float WaterPercent => maxWater > 0 ? currentWater / maxWater : 0f;

    void Awake()
    {
        Services.Register(this);
        currentWater = maxWater;
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // LayoutElement lets us opt out of the dock's layout group during drag
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();

        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isUnlocked) return;

        if (isDragging && currentWater > 0 && wateredPlots.Count > 0)
        {
            currentWater -= waterDrainRate * Time.deltaTime;
            currentWater = Mathf.Max(0, currentWater);

            foreach (var plot in wateredPlots)
            {
                if (plot.State == PlotState.Growing)
                {
                    float bonusTime = (wateringSpeedMultiplier - 1f) * Time.deltaTime;
                    plot.ApplyOfflineTime(bonusTime);
                }
            }
        }
        else if (!isDragging && currentWater < maxWater)
        {
            currentWater += waterRefillRate * Time.deltaTime;
            currentWater = Mathf.Min(maxWater, currentWater);
        }

        UpdateUI();
    }

    public void Unlock()
    {
        isUnlocked = true;
        gameObject.SetActive(true);
    }

    // --- Drag Handlers ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isUnlocked || currentWater <= 0) return;

        isDragging = true;
        canvasGroup.blocksRaycasts = false;

        // Remember where we are in the dock
        dockParent = rectTransform.parent;
        dockSiblingIndex = rectTransform.GetSiblingIndex();

        // Reparent to canvas root so the dock layout group stops fighting us
        rectTransform.SetParent(parentCanvas != null ? parentCanvas.transform : dockParent.root, true);
        rectTransform.SetAsLastSibling(); // render on top of everything
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = eventData.position;
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPoint
            );
            rectTransform.position = worldPoint;
        }

        CheckPlotsUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        foreach (var plot in wateredPlots)
        {
            plot.SetWateringVisual(false);
        }
        wateredPlots.Clear();

        // Reparent back to dock — layout group handles positioning automatically
        rectTransform.SetParent(dockParent, false);
        rectTransform.SetSiblingIndex(dockSiblingIndex);
    }

    void CheckPlotsUnderPointer(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        var currentlyOver = new HashSet<FlowerBed>();

        foreach (var result in results)
        {
            var plot = result.gameObject.GetComponent<FlowerBed>();
            if (plot != null && plot.State == PlotState.Growing && currentWater > 0)
            {
                currentlyOver.Add(plot);

                if (!wateredPlots.Contains(plot))
                {
                    plot.SetWateringVisual(true);
                }
            }
        }

        var toRemove = new List<FlowerBed>();
        foreach (var plot in wateredPlots)
        {
            if (!currentlyOver.Contains(plot))
            {
                plot.SetWateringVisual(false);
                toRemove.Add(plot);
            }
        }
        foreach (var plot in toRemove)
        {
            wateredPlots.Remove(plot);
        }

        foreach (var plot in currentlyOver)
        {
            wateredPlots.Add(plot);
        }
    }

    void UpdateUI()
    {
        if (waterFillBar != null)
            waterFillBar.fillAmount = WaterPercent;

        if (waterText != null)
            waterText.text = $"{currentWater:F0}/{maxWater:F0}";

        if (canIcon != null)
        {
            Color c = canIcon.color;
            c.a = currentWater > 0 ? 1f : 0.4f;
            canIcon.color = c;
        }
    }
}