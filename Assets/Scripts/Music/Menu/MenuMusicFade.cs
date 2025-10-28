using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicFade : MonoBehaviour
{
    public float fadeInDuration = 2f;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0f;
        audioSource.Play();
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 0.5f, t / fadeInDuration);
            yield return null;
        }
    }
}