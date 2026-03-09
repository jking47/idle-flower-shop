using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tints the FlowerBed background image based on plot state.
/// Add to the same GameObject as FlowerBed.
/// Uses the Image component already on the FlowerBed (the Button's target graphic).
/// Event-driven: subscribes to garden events instead of polling in Update().
/// </summary>
[RequireComponent(typeof(FlowerBed))]
public class FlowerBedTint : MonoBehaviour
{
    [Header("State Colors")]
    [SerializeField] Color emptyColor = new Color(0.30f, 0.22f, 0.16f); // dark soil
    [SerializeField] Color growingColor = new Color(0.28f, 0.35f, 0.20f); // earthy green
    [SerializeField] Color bloomedColor = new Color(0.35f, 0.45f, 0.25f); // brighter green

    [Header("Border")]
    [SerializeField] Color borderColor = new Color(0.22f, 0.16f, 0.10f); // dark frame

    Image bgImage;
    FlowerBed bed;
    Outline outline;

    void Awake()
    {
        bgImage = GetComponent<Image>();
        bed = GetComponent<FlowerBed>();

        // Add a subtle border
        outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2, -2);
    }

    void OnEnable()
    {
        EventBus.Subscribe<FlowerPlantedEvent>(OnPlotStateChanged);
        EventBus.Subscribe<FlowerBloomedEvent>(OnPlotStateChanged);
        EventBus.Subscribe<FlowerHarvestedEvent>(OnPlotStateChanged);

        // Re-apply tint on re-enable (e.g. after save/load)
        if (bed != null) ApplyTint(bed.State);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<FlowerPlantedEvent>(OnPlotStateChanged);
        EventBus.Unsubscribe<FlowerBloomedEvent>(OnPlotStateChanged);
        EventBus.Unsubscribe<FlowerHarvestedEvent>(OnPlotStateChanged);
    }

    void OnPlotStateChanged(FlowerPlantedEvent e)  => ApplyTint(bed.State);
    void OnPlotStateChanged(FlowerBloomedEvent e)   => ApplyTint(bed.State);
    void OnPlotStateChanged(FlowerHarvestedEvent e) => ApplyTint(bed.State);

    void ApplyTint(PlotState state)
    {
        if (bgImage == null) return;
        bgImage.color = state switch
        {
            PlotState.Empty   => emptyColor,
            PlotState.Growing => growingColor,
            PlotState.Bloomed => bloomedColor,
            _                 => emptyColor
        };
    }
}
