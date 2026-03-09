using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized visual/audio feedback for game events.
/// Attach to GameManager. Assign AudioClips in inspector (all optional).
/// Subscribes to EventBus and plays appropriate feedback.
/// Tracks active animations per-transform to prevent coroutine stacking.
/// </summary>
public class GameJuice : MonoBehaviour
{
    [Header("SFX Clips (all optional)")]
    [SerializeField] AudioClip harvestSfx;
    [SerializeField] AudioClip plantSfx;
    [SerializeField] AudioClip bloomSfx;
    [SerializeField] AudioClip orderFillSfx;
    [SerializeField] AudioClip unlockSfx;
    [SerializeField] AudioClip errorSfx;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] float sfxVolume = 0.5f;

    [Header("Punch Scale Settings")]
    [SerializeField] float punchScale = 1.35f;
    [SerializeField] float punchDuration = 0.2f;

    [Header("Bloom Pulse Settings")]
    [SerializeField] float bloomPulseScale = 1.2f;
    [SerializeField] float bloomPulseDuration = 0.3f;

    AudioSource sfxSource;

    const string SFX_MUTE_KEY = "SFX_Muted";
    bool sfxMuted;

    // Prevent coroutine stacking — one animation per transform at a time
    readonly Dictionary<Transform, Coroutine> activeAnims = new();
    readonly Dictionary<Transform, Vector3> baseScales = new();

    public bool SfxMuted => sfxMuted;

    void Awake()
    {
        Services.Register(this);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.loop = false;

        sfxMuted = PlayerPrefs.GetInt(SFX_MUTE_KEY, 0) == 1;
    }

    void OnEnable()
    {
        EventBus.Subscribe<FlowerHarvestedEvent>(OnHarvested);
        EventBus.Subscribe<FlowerPlantedEvent>(OnPlanted);
        EventBus.Subscribe<FlowerBloomedEvent>(OnBloomed);
        EventBus.Subscribe<OrderFilledEvent>(OnOrderFilled);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<FlowerHarvestedEvent>(OnHarvested);
        EventBus.Unsubscribe<FlowerPlantedEvent>(OnPlanted);
        EventBus.Unsubscribe<FlowerBloomedEvent>(OnBloomed);
        EventBus.Unsubscribe<OrderFilledEvent>(OnOrderFilled);
    }

    // --- Event Handlers ---

    void OnHarvested(FlowerHarvestedEvent evt)
    {
        PlayClip(harvestSfx);

        if (Services.TryGet<GardenManager>(out var garden))
        {
            if (evt.plotIndex >= 0 && evt.plotIndex < garden.Plots.Count)
            {
                var plot = garden.Plots[evt.plotIndex];
                PunchScale(plot.transform);
                UIBurst.Emit(plot.transform as RectTransform, new Color(0.95f, 0.85f, 0.2f, 1f), 8);
            }
        }
    }

    void OnPlanted(FlowerPlantedEvent evt)
    {
        PlayClip(plantSfx);

        if (Services.TryGet<GardenManager>(out var garden))
        {
            if (evt.plotIndex >= 0 && evt.plotIndex < garden.Plots.Count)
                PunchScale(garden.Plots[evt.plotIndex].transform);
        }
    }

    void OnBloomed(FlowerBloomedEvent evt)
    {
        PlayClip(bloomSfx);

        if (Services.TryGet<GardenManager>(out var garden))
        {
            if (evt.plotIndex >= 0 && evt.plotIndex < garden.Plots.Count)
            {
                var plot = garden.Plots[evt.plotIndex];
                StartBloomPulse(plot.transform);
                UIBurst.Emit(plot.transform as RectTransform, new Color(1f, 0.55f, 0.8f, 1f), 10);
            }
        }
    }

    void OnOrderFilled(OrderFilledEvent evt)
    {
        PlayClip(orderFillSfx);
        // Coin burst at screen center-bottom (shop panel area) — no specific transform available
        // Burst will be emitted from StorePanel's order UI instead via direct call if needed
    }

    // --- Public API for one-off feedback ---

    /// <summary>Play the unlock sound. Call from FlowerBed when a plot is unlocked.</summary>
    public void PlayUnlock()
    {
        PlayClip(unlockSfx);
    }

    /// <summary>Play the error/deny sound. Call when a purchase fails.</summary>
    public void PlayError()
    {
        PlayClip(errorSfx);
    }

    /// <summary>Punch scale on any transform (quick pop then return).</summary>
    public void PunchScale(Transform target)
    {
        if (target != null)
            StartAnim(target, PunchRoutine(target, punchScale, punchDuration));
    }

    /// <summary>Toggle SFX mute. Can be wired to MuteToggle or a separate button.</summary>
    public void SetSfxMuted(bool muted)
    {
        sfxMuted = muted;
        PlayerPrefs.SetInt(SFX_MUTE_KEY, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // --- Animation Management ---

    void StartBloomPulse(Transform target)
    {
        if (target != null)
            StartAnim(target, PulseRoutine(target, bloomPulseScale, bloomPulseDuration));
    }

    void StartAnim(Transform target, IEnumerator routine)
    {
        // Kill any running animation on this transform
        if (activeAnims.TryGetValue(target, out var running) && running != null)
            StopCoroutine(running);

        // Store base scale only on first animation (before any scaling)
        if (!baseScales.ContainsKey(target))
            baseScales[target] = target.localScale;

        // Reset to base before starting new animation
        target.localScale = baseScales[target];

        var co = StartCoroutine(routine);
        activeAnims[target] = co;
    }

    void ClearAnim(Transform target)
    {
        activeAnims.Remove(target);
        baseScales.Remove(target);
    }

    // --- Internal ---

    void PlayClip(AudioClip clip)
    {
        if (clip == null || sfxMuted) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    IEnumerator PunchRoutine(Transform target, float maxScale, float duration)
    {
        Vector3 original = baseScales[target];
        Vector3 punched = original * maxScale;
        float half = duration * 0.35f;

        // Scale up
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(original, punched, t * t);
            yield return null;
        }

        // Scale back down
        elapsed = 0f;
        float settle = duration - half;
        while (elapsed < settle)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settle);
            float eased = 1f - (1f - t) * (1f - t);
            target.localScale = Vector3.Lerp(punched, original, eased);
            yield return null;
        }

        target.localScale = original;
        ClearAnim(target);
    }

    IEnumerator PulseRoutine(Transform target, float maxScale, float duration)
    {
        Vector3 original = baseScales[target];
        Vector3 big = original * maxScale;
        float half = duration * 0.5f;

        // Smooth pulse up
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(original, big, Mathf.Sin(t * Mathf.PI * 0.5f));
            yield return null;
        }

        // Smooth pulse down
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(big, original, Mathf.Sin(t * Mathf.PI * 0.5f));
            yield return null;
        }

        target.localScale = original;
        ClearAnim(target);
    }
}