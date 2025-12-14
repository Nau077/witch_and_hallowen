using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class StageMusicController : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip baseMusic;
    public AudioClip battleMusic;
    public AudioClip stage5Music;

    [Header("Fade")]
    public float fadeDuration = 1.5f;
    public float targetVolume = 0.5f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;
    private int currentStage = -1;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;


    }

    private AudioClip GetClipForStage(int stage)
    {
        if (stage <= 0) return baseMusic;

        //if (stage >= 5 && stage5Music != null)
        //    return stage5Music;

        return battleMusic;
    }

    private void SwitchTo(AudioClip newClip)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeSwitch(newClip));
    }

    private IEnumerator FadeSwitch(AudioClip newClip)
    {
        // fade out
        float startVol = audioSource.volume;
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = 0f;

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // fade in
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }


    public void SetStage(int stage)
    {
        // 1) если stage не поменялся — ничего не делаем
        if (stage == currentStage) return;
        currentStage = stage;

        // 2) выбираем нужный клип
        AudioClip next = GetClipForStage(stage);
        if (next == null) return; // на случай если не назначил клип в инспекторе

        // 3) если уже играет нужный клип — не перезапускаем
        if (audioSource.clip == next && audioSource.isPlaying) return;

        // 4) переключаем с фейдом
        SwitchTo(next);
    }


}