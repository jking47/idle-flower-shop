using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tints the FlowerBed background image based on plot state.
/// Add to the same GameObject as FlowerBed.
/// Uses the Image component already on the FlowerBed (the Button's target graphic).
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
    PlotState lastState = (PlotState)(-1);
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

    void Update()
    {
        if (bed == null || bgImage == null) return;
        if (bed.State == lastState) return;

        lastState = bed.State;
        bgImage.color = lastState switch
        {
            PlotState.Empty => emptyColor,
            PlotState.Growing => growingColor,
            PlotState.Bloomed => bloomedColor,
            _ => emptyColor
        };
    }
}
