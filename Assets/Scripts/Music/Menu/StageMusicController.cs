using System.Collections;
using UnityEngine;

/// <summary>
/// StageMusicController (fixed):
/// - Warmup on startup to avoid first-play hitch
/// - Optional "force load" for clips
/// - Safe switching: no restart if same clip already playing
/// - Fade uses unscaledDeltaTime (independent of timeScale)
///
/// Attach to a GameObject (preferably root/manager) and assign clips in Inspector.
/// Requires: AudioSource (auto-created if missing).
/// </summary>
[DisallowMultipleComponent]
public class StageMusicController : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip baseMusic;
    public AudioClip battleMusic;
    public AudioClip stage5Music;

    [Header("Fade")]
    [Min(0f)] public float fadeDuration = 1.5f;
    [Range(0f, 1f)] public float targetVolume = 0.5f;

    [Header("Warmup / Hitch Fix")]
    [Tooltip("Play a tiny silent warmup at startup to initialize audio backend and decode pipeline.")]
    public bool warmupOnAwake = true;

    [Tooltip("How many frames to wait during warmup (2-3 is usually enough).")]
    [Range(1, 10)] public int warmupFrames = 3;

    [Tooltip("Force-load clips into memory at startup (may increase memory, but reduces hitches).")]
    public bool forceLoadAudioData = true;

    [Tooltip("If true, SetStage can be called before warmup completes; it will queue the last requested stage.")]
    public bool queueStageWhileWarming = true;

    private AudioSource _audio;
    private Coroutine _fadeRoutine;
    private int _currentStage = int.MinValue;

    private bool _warming;
    private bool _warmedUp;

    private int _queuedStage = int.MinValue;
    private bool _hasQueuedStage;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();

        _audio.loop = true;
        _audio.playOnAwake = false;
        _audio.volume = 0f;

        if (forceLoadAudioData)
            ForceLoadAssignedClips();

        if (warmupOnAwake)
            StartCoroutine(Warmup());
        else
            _warmedUp = true;
    }

    private void ForceLoadAssignedClips()
    {
        // These calls are safe; they request Unity to prepare audio data.
        // It may still decode on demand depending on platform/import settings,
        // but helps in many cases.
        if (baseMusic != null && !baseMusic.preloadAudioData) baseMusic.LoadAudioData();
        if (battleMusic != null && !battleMusic.preloadAudioData) battleMusic.LoadAudioData();
        if (stage5Music != null && !stage5Music.preloadAudioData) stage5Music.LoadAudioData();
    }

    private IEnumerator Warmup()
    {
        if (_warming || _warmedUp) yield break;
        _warming = true;

        // Wait a frame so audio device/backend is definitely initialized.
        yield return null;

        AudioClip clip = baseMusic != null ? baseMusic : (battleMusic != null ? battleMusic : stage5Music);
        if (clip != null)
        {
            // Silent, non-loop warmup "play" to force backend/decoder init now.
            bool prevLoop = _audio.loop;
            float prevVol = _audio.volume;
            AudioClip prevClip = _audio.clip;
            bool wasPlaying = _audio.isPlaying;

            _audio.loop = false;
            _audio.volume = 0f;
            _audio.clip = clip;

            // Use Play; PlayOneShot doesn't always force the same init path.
            _audio.Play();

            // Let the audio thread / decoder settle for a couple frames.
            for (int i = 0; i < Mathf.Max(1, warmupFrames); i++)
                yield return null;

            _audio.Stop();
            _audio.clip = prevClip;
            _audio.loop = prevLoop;
            _audio.volume = prevVol;

            if (wasPlaying && prevClip != null)
                _audio.Play();
        }

        _warming = false;
        _warmedUp = true;

        // If stage was requested during warmup, apply the last request now.
        if (_hasQueuedStage)
        {
            int s = _queuedStage;
            _hasQueuedStage = false;
            _queuedStage = int.MinValue;
            SetStage(s);
        }
    }

    private AudioClip GetClipForStage(int stage)
    {
        if (stage <= 0) return baseMusic;

        // Example rule: special track from stage 5
        if (stage >= 5 && stage5Music != null) return stage5Music;

        return battleMusic;
    }

    /// <summary>
    /// Call from RunLevelManager whenever the logical stage changes.
    /// </summary>
    public void SetStage(int stage)
    {
        if (stage == _currentStage) return;

        if (!_warmedUp && queueStageWhileWarming)
        {
            _queuedStage = stage;
            _hasQueuedStage = true;
            return;
        }

        _currentStage = stage;

        AudioClip next = GetClipForStage(stage);
        if (next == null) return;

        // If already playing this clip, do nothing.
        if (_audio.clip == next && _audio.isPlaying) return;

        SwitchTo(next);
    }

    private void SwitchTo(AudioClip newClip)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeSwitch(newClip));
    }

    private IEnumerator FadeSwitch(AudioClip newClip)
    {
        // Fade out current
        float startVol = _audio.volume;
        float d = Mathf.Max(0.001f, fadeDuration);

        for (float t = 0f; t < d; t += Time.unscaledDeltaTime)
        {
            float k = t / d;
            _audio.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }
        _audio.volume = 0f;

        // Switch clip
        _audio.Stop();
        _audio.clip = newClip;

        // Safety: if clip is not loaded yet, request load.
        if (forceLoadAudioData && newClip != null && newClip.loadState == AudioDataLoadState.Unloaded)
            newClip.LoadAudioData();

        _audio.Play();

        // Fade in
        for (float t = 0f; t < d; t += Time.unscaledDeltaTime)
        {
            float k = t / d;
            _audio.volume = Mathf.Lerp(0f, targetVolume, k);
            yield return null;
        }
        _audio.volume = targetVolume;

        _fadeRoutine = null;
    }

    // Optional helper if you want to hard-stop music (e.g., in menu)
    public void StopMusicImmediate()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
        _audio.volume = 0f;
        _audio.Stop();
        _audio.clip = null;
        _currentStage = int.MinValue;
    }

    // Optional helper if you want to set volume from settings
    public void SetTargetVolume(float v)
    {
        targetVolume = Mathf.Clamp01(v);
        if (_audio != null && _audio.isPlaying)
            _audio.volume = targetVolume;
    }
}
