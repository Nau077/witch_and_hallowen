using System.Collections;
using UnityEngine;
// Если используешь URP 2D Light, раскомментируй:
// using UnityEngine.Rendering.Universal;

public class FireballChargeFX : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Корень визуала ауры (объект FireAura). Будет включаться/выключаться.")]
    public GameObject fireAuraRoot;

    [Tooltip("Если не заполнено — найдём все SpriteRenderer-ы под fireAuraRoot.")]
    public SpriteRenderer[] auraRenderers;

    // [Tooltip("Опционально: 2D-света для ауры")]
    // public Light2D[] auraLights;

    [Header("Fade")]
    [Tooltip("Максимальная непрозрачность ауры на полном заряде")]
    public float maxAlpha = 1f;

    [Tooltip("Сколько секунд длится «быстрое» включение/выключение")]
    public float fadeDuration = 0.2f;

    [Tooltip("Выключать ли объект fireAuraRoot, когда альфа = 0")]
    public bool deactivateWhenHidden = true;

    Coroutine fadeRoutine;
    float currentAlpha = 0f;

    void Awake()
    {
        if (fireAuraRoot != null && (auraRenderers == null || auraRenderers.Length == 0))
            auraRenderers = fireAuraRoot.GetComponentsInChildren<SpriteRenderer>(true);

        // if (fireAuraRoot != null && (auraLights == null || auraLights.Length == 0))
        //     auraLights = fireAuraRoot.GetComponentsInChildren<Light2D>(true);

        SetAlphaImmediate(0f);
        if (deactivateWhenHidden && fireAuraRoot != null)
            fireAuraRoot.SetActive(false);
    }

    // ---- API, которое уже вызывает PlayerFireballShooter ----

    /// <summary>Начали заряд — мягко включаем ауру с альфы 0.</summary>
    public void BeginCharge()
    {
        if (fireAuraRoot != null && deactivateWhenHidden)
            fireAuraRoot.SetActive(true);

        // быстрый старт до небольшой видимости
        StartFadeTo(0.001f, 0.05f);
    }

    /// <summary>Плавное обновление яркости: t = 0..1 (от первой точки до максимума).</summary>
    public void UpdateCharge(float t)
    {
        float target = Mathf.Clamp01(t) * maxAlpha;
        // двигаем альфу «напрямую», без дерганий
        SetAlphaImmediate(target);
    }

    /// <summary>Выстрел: можно дать короткий флаш и потушить.</summary>
    public void Release()
    {
        // небольшой флэш (опционально)
        StartCoroutine(FlashThenFadeOut());
    }

    /// <summary>Отмена: просто потушить.</summary>
    public void Cancel()
    {
        StartFadeTo(0f, fadeDuration, () =>
        {
            if (fireAuraRoot != null && deactivateWhenHidden)
                fireAuraRoot.SetActive(false);
        });
    }

    // ---- внутренние штуки ----

    IEnumerator FlashThenFadeOut()
    {
        // вспышка до maxAlpha на короткое время
        SetAlphaImmediate(maxAlpha);
        yield return new WaitForSeconds(0.05f);

        // мягко погасить
        StartFadeTo(0f, fadeDuration, () =>
        {
            if (fireAuraRoot != null && deactivateWhenHidden)
                fireAuraRoot.SetActive(false);
        });
    }

    void StartFadeTo(float target, float duration, System.Action onComplete = null)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(target, duration, onComplete));
    }

    IEnumerator FadeTo(float target, float duration, System.Action onComplete)
    {
        float start = currentAlpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start, target, duration <= 0f ? 1f : t / duration);
            SetAlphaImmediate(a);
            yield return null;
        }
        SetAlphaImmediate(target);
        fadeRoutine = null;
        onComplete?.Invoke();
    }

    void SetAlphaImmediate(float a)
    {
        currentAlpha = a;

        if (auraRenderers != null)
        {
            for (int i = 0; i < auraRenderers.Length; i++)
            {
                if (!auraRenderers[i]) continue;
                var c = auraRenderers[i].color;
                c.a = a;
                auraRenderers[i].color = c;
            }
        }

        // Если используешь 2D Light:
        // if (auraLights != null)
        // {
        //     for (int i = 0; i < auraLights.Length; i++)
        //     {
        //         if (!auraLights[i]) continue;
        //         auraLights[i].intensity = a; // или a * maxIntensity
        //     }
        // }
    }
}
