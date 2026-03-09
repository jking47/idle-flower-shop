using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Individual pest that moves toward a target FlowerBed plot using world/screen
/// positions (transform.position) so it works correctly regardless of canvas hierarchy.
///
/// Repelled by WateringCan drag. On reaching target, applies yield penalty.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class Pest : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Icons per phase: index 0 = Patch (bug), 1 = Garden (bird), 2 = Shop (blight)")]
    [SerializeField] Sprite[] phaseIcons;

    [Header("Audio")]
    [SerializeField] AudioClip repelSound;
    [SerializeField] AudioClip reachSound;
    [SerializeField] AudioSource audioSource;

    [Header("Arrival")]
    [Tooltip("Screen-pixel distance at which pest is considered to have reached the plot")]
    [SerializeField] float arrivalThreshold = 40f;

    [Header("Feeding")]
    [Tooltip("How long the pest sits on the plot before dealing damage (seconds)")]
    [SerializeField] float minFeedDuration = 10f;
    [SerializeField] float maxFeedDuration = 15f;

    [Header("Size")]
    [SerializeField] float displaySize = 60f;

    FlowerBed targetPlot;
    float speed;
    float yieldPenalty;
    Action<Pest, bool> onResolved;

    Image icon;
    Image timerRing;
    bool resolved;
    bool feeding;
    bool fleeing;
    Vector3 fleeTarget;
    float feedTimer;
    float feedDuration;
    float spawnTime;

    // Shared ring sprite — generated once, reused across all pest instances
    static Sprite _ringSprite;

    void Awake()
    {
        icon = GetComponent<Image>();

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(displaySize, displaySize);

        CreateTimerRing();
    }

    void CreateTimerRing()
    {
        var go = new GameObject("TimerRing");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(transform, false);

        // Slightly larger than the icon so it forms a halo around it
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(displaySize + 16f, displaySize + 16f);
        rt.anchoredPosition = Vector2.zero;

        timerRing = go.AddComponent<Image>();
        timerRing.sprite      = GetOrCreateRingSprite();
        timerRing.type        = Image.Type.Filled;
        timerRing.fillMethod  = Image.FillMethod.Radial360;
        timerRing.fillOrigin  = (int)Image.Origin360.Top;
        timerRing.fillClockwise = true;
        timerRing.fillAmount  = 1f;
        timerRing.color       = new Color(0.25f, 0.9f, 0.25f, 0.9f); // starts green
        timerRing.raycastTarget = false;
        timerRing.gameObject.SetActive(false);
    }

    static Sprite GetOrCreateRingSprite()
    {
        if (_ringSprite != null) return _ringSprite;

        const int size        = 64;
        const float outerR    = 30f;
        const float innerR    = 20f; // ring thickness
        float cx = size / 2f, cy = size / 2f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            tex.SetPixel(x, y, (d <= outerR && d >= innerR) ? Color.white : Color.clear);
        }
        tex.Apply();

        _ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _ringSprite;
    }

    /// <summary>Called by PestManager immediately after instantiation.</summary>
    public void Initialize(
        FlowerBed target,
        Vector3 spawnWorldPos,
        float moveSpeed,
        float penaltyPercent,
        Action<Pest, bool> resolvedCallback)
    {
        targetPlot   = target;
        speed        = moveSpeed;
        yieldPenalty = penaltyPercent;
        onResolved   = resolvedCallback;
        spawnTime    = Time.time;

        gameObject.SetActive(true); // ensure active regardless of prefab saved state
        icon.color = Color.white;   // guard against prefab alpha=0
        transform.position = spawnWorldPos;

        // Pick phase icon
        if (phaseIcons != null && phaseIcons.Length > 0 &&
            Services.TryGet<GameManager>(out var gm))
        {
            int idx = Mathf.Clamp((int)gm.CurrentPhase, 0, phaseIcons.Length - 1);
            if (phaseIcons[idx] != null) icon.sprite = phaseIcons[idx];
        }

        // Visible fallback if no sprite assigned
        if (icon.sprite == null)
            icon.color = new Color(0.8f, 0.2f, 0.9f);

        Debug.Log($"[Pest] Spawned at {spawnWorldPos}, targeting plot {target.PlotIndex} at {target.transform.position}");
    }

    void Update()
    {
        if (fleeing) { UpdateFlee(); return; }
        if (resolved) return;

        // Feeding phase — pest sits on the plot until timer expires
        if (feeding)
        {
            feedTimer += Time.deltaTime;

            // Update clock ring: green → yellow → red as time runs out
            if (timerRing != null)
            {
                float t = feedTimer / feedDuration;
                timerRing.fillAmount = 1f - t;
                timerRing.color = t < 0.5f
                    ? Color.Lerp(new Color(0.25f, 0.9f, 0.25f, 0.9f), new Color(1f, 0.85f, 0f, 0.9f), t * 2f)
                    : Color.Lerp(new Color(1f, 0.85f, 0f, 0.9f), new Color(1f, 0.15f, 0.1f, 0.9f), (t - 0.5f) * 2f);
            }

            if (feedTimer >= feedDuration)
                FinishFeeding();
            return;
        }

        if (targetPlot == null) return;

        Vector3 targetPos = targetPlot.transform.position;
        Vector3 direction = (targetPos - transform.position).normalized;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // Face direction of travel
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        // Arrival check — grace period prevents instant trigger on spawn frame
        if (Time.time - spawnTime > 0.5f &&
            Vector3.Distance(transform.position, targetPos) <= arrivalThreshold)
        {
            StartFeeding();
        }
    }

    void UpdateFlee()
    {
        transform.position = Vector3.MoveTowards(transform.position, fleeTarget, speed * 3f * Time.deltaTime);

        Vector3 dir = (fleeTarget - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        if (Vector3.Distance(transform.position, fleeTarget) < 5f)
            Destroy(gameObject);
    }

    void StartFeeding()
    {
        feeding = true;
        feedTimer = 0f;
        feedDuration = UnityEngine.Random.Range(minFeedDuration, maxFeedDuration);

        // Snap onto the plot and stop rotating
        transform.position = targetPlot.transform.position;
        transform.rotation = Quaternion.identity;

        if (timerRing != null)
        {
            timerRing.fillAmount = 1f;
            timerRing.color = new Color(0.25f, 0.9f, 0.25f, 0.9f);
            timerRing.gameObject.SetActive(true);
        }

        Debug.Log($"[Pest] Feeding on plot {targetPlot.PlotIndex} — will deal damage in {feedDuration:F1}s");
    }

    void FinishFeeding()
    {
        if (resolved) return;
        resolved = true;

        targetPlot?.ApplyPestDamage(yieldPenalty);

        EventBus.Publish(new PestReachedPlotEvent
        {
            plotIndex           = targetPlot != null ? targetPlot.PlotIndex : -1,
            yieldPenaltyPercent = yieldPenalty
        });

        PlaySound(reachSound);
        onResolved?.Invoke(this, false);
        Destroy(gameObject);
    }

    /// <summary>Called by SprayBottle when dragged over this pest. Returns true if the repel was accepted.</summary>
    public bool Repel()
    {
        if (resolved || fleeing) return false;
        resolved = true;
        feeding  = false;
        fleeing  = true;
        fleeTarget = RandomOffscreenPosition();

        if (timerRing != null)
            timerRing.gameObject.SetActive(false);

        EventBus.Publish(new PestRepeledEvent { plotIndex = targetPlot != null ? targetPlot.PlotIndex : -1 });

        PlaySound(repelSound);
        if (Services.TryGet<GameJuice>(out var juice))
            juice.PunchScale(transform);

        onResolved?.Invoke(this, true);
        return true;
        // GameObject destroyed by UpdateFlee once it exits the screen
    }

    /// <summary>Returns a position well off the nearest screen edge.</summary>
    Vector3 RandomOffscreenPosition()
    {
        float margin = 150f;
        Vector3 pos  = transform.position;

        // Pick the closest edge to flee toward
        float distLeft   = pos.x;
        float distRight  = Screen.width  - pos.x;
        float distBottom = pos.y;
        float distTop    = Screen.height - pos.y;

        float minDist = Mathf.Min(distLeft, distRight, distBottom, distTop);

        if      (minDist == distLeft)   return new Vector3(-margin,                      pos.y, 0);
        else if (minDist == distRight)  return new Vector3(Screen.width  + margin,       pos.y, 0);
        else if (minDist == distBottom) return new Vector3(pos.x,         -margin,             0);
        else                            return new Vector3(pos.x,          Screen.height + margin, 0);
    }


    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        // Spawn a free-floating temporary audio object so it survives past this pest's destruction
        var tempGo = new GameObject("PestSFX");
        var src = tempGo.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 0f;
        src.Play();
        Destroy(tempGo, clip.length + 0.1f);
    }
}
