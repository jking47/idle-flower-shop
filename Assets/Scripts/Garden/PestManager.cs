using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages timed pest invasion events. Spawns Pest instances targeting
/// occupied garden plots and tracks resolution (repelled vs. reached).
///
/// Creates its own Screen Space Overlay canvas at runtime (sort order 100)
/// so pests render on top of all game UI without being clipped by panel masks.
///
/// Pest difficulty scales with game phase:
///   Patch  — slow bugs, light penalty
///   Garden — faster birds, moderate penalty
///   Shop   — quick blight spores, heavy penalty
///
/// Attach to GameManager object.
/// </summary>
public class PestManager : MonoBehaviour
{
    [Header("Feature Toggle")]
    [SerializeField] bool featureEnabled = true;

    [Header("Prefab")]
    [SerializeField] GameObject pestPrefab;

    [Header("Event Timing")]
    [SerializeField] float minEventInterval = 120f;
    [SerializeField] float maxEventInterval = 180f;
    [SerializeField] float initialGracePeriod = 60f;

    [Header("Patch Phase")]
    [SerializeField] int patchPestCount = 1;
    [SerializeField] float patchPestSpeed = 40f;
    [SerializeField] float patchYieldPenalty = 0.15f;

    [Header("Garden Phase")]
    [SerializeField] int gardenPestCount = 2;
    [SerializeField] float gardenPestSpeed = 60f;
    [SerializeField] float gardenYieldPenalty = 0.20f;

    [Header("Shop Phase")]
    [SerializeField] int shopPestCount = 3;
    [SerializeField] float shopPestSpeed = 80f;
    [SerializeField] float shopYieldPenalty = 0.25f;

    [Header("Difficulty Ramp")]
    [Tooltip("First N events are capped at 1 pest so new players can learn the mechanic")]
    [SerializeField] int rampUpEventCount = 3;

    // Runtime
    readonly List<Pest> activePests = new();
    int resolvedRepelled;
    int resolvedReached;
    int totalEventsStarted;
    bool eventInProgress;
    RectTransform overlayCanvasRt;

    public bool EventInProgress => eventInProgress;
    public IReadOnlyList<Pest> ActivePests => activePests;

    void Awake()
    {
        Services.Register(this);
        CreateOverlayCanvas();
    }

    void Start()
    {
        if (featureEnabled)
            StartCoroutine(PestEventLoop());
    }

    /// <summary>
    /// Creates a dedicated Screen Space Overlay canvas that renders above all game UI.
    /// Pests are children of this canvas so they are never clipped by garden panel masks.
    /// </summary>
    void CreateOverlayCanvas()
    {
        var go     = new GameObject("PestOverlayCanvas");
        go.transform.SetParent(transform); // child of GameManager — destroyed with it on reset
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        go.AddComponent<GraphicRaycaster>(); // required for WateringCan raycast detection

        overlayCanvasRt = go.GetComponent<RectTransform>();
    }

    IEnumerator PestEventLoop()
    {
        yield return new WaitForSeconds(initialGracePeriod);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minEventInterval, maxEventInterval));
            TrySpawnEvent();
        }
    }

    /// <summary>Immediately triggers a pest event, bypassing the timer. Used by the debug panel.</summary>
    public void TriggerPestEvent()
    {
        if (!featureEnabled)
        {
            Debug.LogWarning("[PestManager] Feature is disabled — enable via inspector toggle.");
            return;
        }
        TrySpawnEvent();
    }

    void TrySpawnEvent()
    {
        if (eventInProgress) return;
        if (pestPrefab == null || overlayCanvasRt == null) return;
        if (!Services.TryGet<GardenManager>(out var garden)) return;

        var targets = GetOccupiedPlots(garden);
        if (targets.Count == 0) return;

        if (!Services.TryGet<GameManager>(out var gm)) return;
        int count = PestCountForPhase(gm.CurrentPhase);

        // Cap to 1 pest for early events so new players can learn the mechanic
        if (totalEventsStarted < rampUpEventCount)
            count = 1;

        totalEventsStarted++;
        eventInProgress  = true;
        resolvedRepelled = 0;
        resolvedReached  = 0;

        EventBus.Publish(new PestEventStartedEvent { phase = gm.CurrentPhase, pestCount = count });
        Debug.Log($"[PestManager] Event {totalEventsStarted} started — phase: {gm.CurrentPhase}, pests: {count}");

        for (int i = 0; i < count; i++)
        {
            FlowerBed target = targets[Random.Range(0, targets.Count)];
            SpawnPest(target, PestSpeedForPhase(gm.CurrentPhase), PestPenaltyForPhase(gm.CurrentPhase));
        }
    }

    void SpawnPest(FlowerBed targetPlot, float speed, float yieldPenalty)
    {
        var obj  = Instantiate(pestPrefab, overlayCanvasRt);
        var pest = obj.GetComponent<Pest>();
        if (pest == null) { Destroy(obj); return; }

        pest.Initialize(targetPlot, RandomScreenEdgePosition(), speed, yieldPenalty, OnPestResolved);
        activePests.Add(pest);
    }

    void OnPestResolved(Pest pest, bool wasRepelled)
    {
        activePests.Remove(pest);
        if (wasRepelled) resolvedRepelled++;
        else             resolvedReached++;

        if (activePests.Count == 0)
        {
            eventInProgress = false;
            EventBus.Publish(new PestEventEndedEvent { repelled = resolvedRepelled, reached = resolvedReached });
        }
    }

    // --- Helpers ---

    List<FlowerBed> GetOccupiedPlots(GardenManager garden)
    {
        var result = new List<FlowerBed>();
        foreach (var plot in garden.Plots)
            if (!plot.IsLocked && (plot.State == PlotState.Growing || plot.State == PlotState.Bloomed))
                result.Add(plot);
        return result;
    }

    /// <summary>Returns a world/screen position just off the edge of the screen.</summary>
    Vector3 RandomScreenEdgePosition()
    {
        float margin = 60f;
        int edge = Random.Range(0, 4);
        return edge switch
        {
            0 => new Vector3(Random.Range(0f, Screen.width), Screen.height + margin, 0),
            1 => new Vector3(Random.Range(0f, Screen.width), -margin, 0),
            2 => new Vector3(-margin, Random.Range(0f, Screen.height), 0),
            _ => new Vector3(Screen.width + margin, Random.Range(0f, Screen.height), 0)
        };
    }

    int   PestCountForPhase(GamePhase p)   => p switch { GamePhase.Garden => gardenPestCount,   GamePhase.Shop => shopPestCount,   _ => patchPestCount };
    float PestSpeedForPhase(GamePhase p)   => p switch { GamePhase.Garden => gardenPestSpeed,   GamePhase.Shop => shopPestSpeed,   _ => patchPestSpeed };
    float PestPenaltyForPhase(GamePhase p) => p switch { GamePhase.Garden => gardenYieldPenalty, GamePhase.Shop => shopYieldPenalty, _ => patchYieldPenalty };
}
