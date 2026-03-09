using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Draggable spray bottle tool. Player drags it over pest icons to repel them.
/// Each repel costs a fixed amount of spray; spray refills over time.
///
/// Mirror of WateringCan's drag/dock pattern — attach to the spray bottle
/// UI icon and configure the dock parent the same way.
/// </summary>
public class SprayBottle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Spray Settings")]
    [Tooltip("Spray consumed each time a pest is repelled")]
    [SerializeField] float sprayPerRepel = 25f;

    [Tooltip("Spray refilled per second while not actively spraying")]
    [SerializeField] float sprayRefillRate = 5f;

    [Tooltip("Maximum spray capacity")]
    [SerializeField] float maxSpray = 100f;

    [Header("UI")]
    [SerializeField] Image bottleIcon;
    [SerializeField] Image sprayFillBar;
    [SerializeField] TMP_Text sprayText;

    [Header("Drag Settings")]
    [SerializeField] Canvas parentCanvas;

    [Header("Audio")]
    [Tooltip("Plays each time a pest is successfully repelled")]
    [SerializeField] AudioSource spraySound;

    [Tooltip("Plays when the player tries to drag with no spray remaining")]
    [SerializeField] AudioSource emptySound;

    float currentSpray;
    bool isDragging;
    RectTransform rectTransform;
    CanvasGroup canvasGroup;
    LayoutElement layoutElement;

    // Dock references for reparenting during drag
    Transform dockParent;
    int dockSiblingIndex;
    GameObject dockPlaceholder;

    // Cached raycast list — reused every drag frame to avoid GC allocs
    readonly List<RaycastResult> _raycastResults = new();

    public bool IsDragging => isDragging;
    public float SprayPercent => maxSpray > 0 ? currentSpray / maxSpray : 0f;

    void Awake()
    {
        Services.Register(this);
        currentSpray = maxSpray;
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    void Update()
    {
        // Refill when not dragging
        if (!isDragging && currentSpray < maxSpray)
        {
            currentSpray += sprayRefillRate * Time.deltaTime;
            currentSpray  = Mathf.Min(maxSpray, currentSpray);
        }

        UpdateUI();
    }

    // --- Drag Handlers ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSpray <= 0)
        {
            emptySound?.Play();
            return;
        }

        isDragging = true;
        canvasGroup.blocksRaycasts = false;

        dockParent = rectTransform.parent;
        dockSiblingIndex = rectTransform.GetSiblingIndex();

        dockPlaceholder = new GameObject("SprayPlaceholder");
        var phRt = dockPlaceholder.AddComponent<RectTransform>();
        phRt.SetParent(dockParent, false);
        phRt.SetSiblingIndex(dockSiblingIndex);
        var le = dockPlaceholder.AddComponent<LayoutElement>();
        le.preferredWidth  = rectTransform.rect.width;
        le.preferredHeight = rectTransform.rect.height;

        rectTransform.SetParent(parentCanvas != null ? parentCanvas.transform : dockParent.root, true);
        rectTransform.SetAsLastSibling();
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

        CheckPestsUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        if (dockPlaceholder != null)
        {
            int idx = dockPlaceholder.transform.GetSiblingIndex();
            Destroy(dockPlaceholder);
            dockPlaceholder = null;
            rectTransform.SetParent(dockParent, false);
            rectTransform.SetSiblingIndex(idx);
        }
        else
        {
            rectTransform.SetParent(dockParent, false);
            rectTransform.SetSiblingIndex(dockSiblingIndex);
        }
    }

    void CheckPestsUnderPointer(PointerEventData eventData)
    {
        if (currentSpray <= 0) return;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);
        var results = _raycastResults;

        foreach (var result in results)
        {
            var pest = result.gameObject.GetComponent<Pest>();
            if (pest == null) continue;

            // Only charge spray if the pest actually accepted the repel
            // (guards against multiple raycast hits on the same pest's child objects)
            if (pest.Repel())
            {
                spraySound?.Play();
                currentSpray -= sprayPerRepel;
                currentSpray  = Mathf.Max(0, currentSpray);
            }

            if (currentSpray <= 0) break;
        }
    }

    void UpdateUI()
    {
        if (sprayFillBar != null)
            sprayFillBar.fillAmount = SprayPercent;

        if (sprayText != null)
            sprayText.text = $"{currentSpray:F0}/{maxSpray:F0}";

        // Dim icon when empty
        if (bottleIcon != null)
        {
            Color c = bottleIcon.color;
            c.a = currentSpray > 0 ? 1f : 0.4f;
            bottleIcon.color = c;
        }

        if (sprayFillBar != null)
        {
            Color fc = sprayFillBar.color;
            fc.a = currentSpray > 0 ? 1f : 0.3f;
            sprayFillBar.color = fc;
        }
    }
}
