using System.Collections;
using UnityEngine;

/// <summary>
/// Handles background music playback with crossfade support.
/// Attach to GameManager alongside other managers.
/// 
/// Setup:
///   1. Add this component to GameManager
///   2. Assign AudioClips to the bgmTracks array in the inspector
///   3. Optionally set startTrackIndex to choose which track plays first
///
/// Usage from other scripts:
///   Services.Get<AudioManager>().PlayTrack(1);
///   Services.Get<AudioManager>().SetBGMVolume(0.5f);
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("BGM")]
    [Tooltip("Assign your music clips here. Index 0 plays by default.")]
    [SerializeField] AudioClip[] bgmTracks;

    [SerializeField] int startTrackIndex = -1;

    [SerializeField, Range(0f, 1f)] float bgmVolume = 0.4f;
    [SerializeField] float crossfadeDuration = 1.5f;

    AudioSource bgmSourceA;
    AudioSource bgmSourceB;
    int currentTrackIndex = -1;
    bool sourceAActive = true;
    bool isMuted;
    bool autoAdvance = true;
    Coroutine fadeRoutine;

    AudioSource ActiveSource => sourceAActive ? bgmSourceA : bgmSourceB;
    AudioSource InactiveSource => sourceAActive ? bgmSourceB : bgmSourceA;

    void Awake()
    {
        Services.Register(this);

        // Two sources for crossfading between tracks
        bgmSourceA = gameObject.AddComponent<AudioSource>();
        bgmSourceB = gameObject.AddComponent<AudioSource>();

        ConfigureSource(bgmSourceA);
        ConfigureSource(bgmSourceB);
    }

    void Start()
    {
        if (bgmTracks == null || bgmTracks.Length == 0) return;

        if (startTrackIndex >= 0)
            PlayTrack(startTrackIndex);
        else
            PlayTrack(Random.Range(0, bgmTracks.Length));
    }

    /// <summary>
    /// Play a BGM track by index. Crossfades from current track if one is playing.
    /// </summary>
    public void PlayTrack(int index)
    {
        if (bgmTracks == null || index < 0 || index >= bgmTracks.Length) return;
        if (bgmTracks[index] == null) return;
        if (index == currentTrackIndex && ActiveSource.isPlaying) return;

        currentTrackIndex = index;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(CrossfadeTo(bgmTracks[index]));
    }

    /// <summary>
    /// Play the next track in the array, wrapping around.
    /// </summary>
    public void PlayNextTrack()
    {
        if (bgmTracks == null || bgmTracks.Length == 0) return;
        PlayTrack((currentTrackIndex + 1) % bgmTracks.Length);
    }

    /// <summary>
    /// Set BGM volume (0-1). Stored even when muted.
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (ActiveSource.isPlaying)
            ActiveSource.volume = isMuted ? 0f : bgmVolume;
    }

    /// <summary>
    /// Mute or unmute BGM. Preserves volume setting for unmute.
    /// </summary>
    public void SetMuted(bool muted)
    {
        isMuted = muted;
        ActiveSource.volume = isMuted ? 0f : bgmVolume;
    }

    /// <summary>
    /// Pause/unpause BGM.
    /// </summary>
    public void SetBGMPaused(bool paused)
    {
        if (paused)
            ActiveSource.Pause();
        else
            ActiveSource.UnPause();
    }

    public float BGMVolume => bgmVolume;
    public bool IsMuted => isMuted;
    public int CurrentTrackIndex => currentTrackIndex;
    public int TrackCount => bgmTracks != null ? bgmTracks.Length : 0;

    IEnumerator CrossfadeTo(AudioClip newClip)
    {
        var fadeOut = ActiveSource;
        sourceAActive = !sourceAActive;
        var fadeIn = ActiveSource;

        fadeIn.clip = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        float elapsed = 0f;
        float startVolume = fadeOut.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossfadeDuration;

            fadeOut.volume = Mathf.Lerp(startVolume, 0f, t);
            fadeIn.volume = Mathf.Lerp(0f, isMuted ? 0f : bgmVolume, t);

            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;
        fadeIn.volume = isMuted ? 0f : bgmVolume;
        fadeRoutine = null;
    }

    void Update()
    {
        if (!autoAdvance || fadeRoutine != null) return;
        if (bgmTracks == null || bgmTracks.Length <= 1) return;
        if (currentTrackIndex < 0) return;
        if (!ActiveSource.isPlaying)
            PlayNextTrack();
    }

    void ConfigureSource(AudioSource source)
    {
        source.loop = false;
        source.playOnAwake = false;
        source.volume = 0f;
        source.spatialBlend = 0f; // 2D audio
    }
}