using System.Collections;
using UnityEngine;
// Если используешь 2D Light (URP), раскомментируй:
// using UnityEngine.Rendering.Universal;

public class FireAuraFader : MonoBehaviour
{
    [Header("What to fade")]
    [SerializeField] private SpriteRenderer[] renderers;   // можно оставить пустым – найдём сами
    // [SerializeField] private Light2D[] lights;          // опционально: если есть 2D-света

    [Header("Fade")]
    [SerializeField] private float maxAlpha = 1f;          // на какой прозрачности «горит» полностью
    [SerializeField] private bool deactivateWhenHidden = true;

    Coroutine fadeRoutine;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

        // lights ??= GetComponentsInChildren<Light2D>(true); // если используешь Light2D

        SetAlphaImmediate(0f);
        if (deactivateWhenHidden) gameObject.SetActive(false);
    }

    public void FadeIn(float duration = 0.2f)
    {
        if (deactivateWhenHidden) gameObject.SetActive(true);
        StartFade(maxAlpha, duration);
    }

    public void FadeOut(float duration = 0.2f)
    {
        StartFade(0f, duration, onComplete: () =>
        {
            if (deactivateWhenHidden) gameObject.SetActive(false);
        });
    }

    void StartFade(float target, float duration, System.Action onComplete = null)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(target, duration, onComplete));
    }

    IEnumerator FadeTo(float target, float duration, System.Action onComplete)
    {
        // текущее значение берём с первого рендера
        float start = (renderers != null && renderers.Length > 0) ? renderers[0].color.a : 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
            SetAlphaImmediate(a);
            yield return null;
        }
        SetAlphaImmediate(target);
        onComplete?.Invoke();
        fadeRoutine = null;
    }

    void SetAlphaImmediate(float a)
    {
        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var c = renderers[i].color;
                c.a = a;
                renderers[i].color = c;
            }
        }

        // Если используешь 2D Light — можно линейно менять интенсивность.
        // if (lights != null)
        // {
        //     foreach (var l in lights) l.intensity = a; // или a * maxIntensity
        // }
    }
}
