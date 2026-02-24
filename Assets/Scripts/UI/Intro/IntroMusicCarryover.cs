using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class IntroMusicCarryover : MonoBehaviour
{
    private static IntroMusicCarryover _instance;
    private AudioSource _audioSource;

    public static bool HasActiveCarryover
    {
        get
        {
            return _instance != null &&
                   _instance._audioSource != null &&
                   _instance._audioSource.isPlaying;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (_audioSource == null || !_audioSource.isPlaying)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public static void CreateFrom(AudioSource source)
    {
        if (source == null || source.clip == null || !source.isPlaying)
            return;

        if (_instance != null)
            Destroy(_instance.gameObject);

        var go = new GameObject("IntroMusicCarryover");
        var newSource = go.AddComponent<AudioSource>();

        newSource.clip = source.clip;
        newSource.outputAudioMixerGroup = source.outputAudioMixerGroup;
        newSource.mute = source.mute;
        newSource.bypassEffects = source.bypassEffects;
        newSource.bypassListenerEffects = source.bypassListenerEffects;
        newSource.bypassReverbZones = source.bypassReverbZones;
        newSource.priority = source.priority;
        newSource.volume = source.volume;
        newSource.pitch = source.pitch;
        newSource.panStereo = source.panStereo;
        newSource.spatialBlend = source.spatialBlend;
        newSource.reverbZoneMix = source.reverbZoneMix;
        newSource.loop = false;
        newSource.playOnAwake = false;

        float t = Mathf.Clamp(source.time, 0f, Mathf.Max(0f, source.clip.length - 0.01f));
        newSource.time = t;
        newSource.Play();

        go.AddComponent<IntroMusicCarryover>();
    }

    public static void StopAndDestroyActive()
    {
        if (_instance == null)
            return;

        if (_instance._audioSource != null)
            _instance._audioSource.Stop();

        Destroy(_instance.gameObject);
    }
}
