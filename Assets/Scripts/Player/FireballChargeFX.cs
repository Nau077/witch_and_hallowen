using System.Collections;
using UnityEngine;
#if USING_UNIVERSAL_RENDER_PIPELINE
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal; // для старых версий
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Volume, если нужно
#endif

public class FireballChargeFX : MonoBehaviour
{
    [Header("Sprite")]
    public SpriteRenderer[] bodySprites;         // спрайты персонажа (тело/одежда и т.п.)
    public bool useEmissionMaterial = false;
    public Material normalMat;
    public Material emissiveMat;                 // материал с эмиссией/аутлайном (URP Sprite Lit + Emission)

    [Header("Tint (если без шейдера)")]
    public Gradient tintGradient;                // от базового -> жёлто-оранжевый
    public float maxTintStrength = 0.65f;        // насколько сильно красим

#if USING_UNIVERSAL_RENDER_PIPELINE
    [Header("2D Light (URP)")]
    public UnityEngine.Experimental.Rendering.Universal.Light2D auraLight;
    public Gradient lightColor;
    public float minRadius = 1.0f;
    public float maxRadius = 3.5f;
    public float maxIntensity = 8f;

    [Header("Optional Bloom Volume")]
    public Volume localVolume;                   // локальный Volume на персонаже (с Bloom)
    public float maxVolumeWeight = 0.7f;
#endif

    [Header("Particles")]
    public ParticleSystem sparks;
    public ParticleSystem heat;
    public int sparksMaxRate = 40;
    public float heatMaxSize = 1.1f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip chargeLoop;
    public AudioClip releaseClip;
    public float maxPitch = 1.4f;
    public float maxVolume = 0.85f;

    [Header("Curves")]
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // как растёт свечение
    public AnimationCurve burstFadeCurve = AnimationCurve.EaseInOut(0, 1, 0.15f, 0);

    bool isCharging;
    float currentT;

    public void BeginCharge()
    {
        if (isCharging) return;
        isCharging = true;
        currentT = 0f;

        // Материал
        if (useEmissionMaterial && emissiveMat != null)
        {
            foreach (var sr in bodySprites) if (sr) sr.material = emissiveMat;
            SetEmission(0f);
        }

        // Tint
        ApplyTint(0f);

        // Particles
        if (sparks) { var e = sparks.emission; e.rateOverTime = 0f; sparks.Play(); }
        if (heat) { var m = heat.main; m.startSize = 0.2f; heat.Play(); }

#if USING_UNIVERSAL_RENDER_PIPELINE
        // Light
        if (auraLight)
        {
            auraLight.enabled = true;
            auraLight.intensity = 0f;
            auraLight.pointLightOuterRadius = minRadius;
            auraLight.color = lightColor.Evaluate(0f);
        }
        if (localVolume) localVolume.weight = 0f;
#endif

        // Audio
        if (audioSource && chargeLoop)
        {
            audioSource.clip = chargeLoop;
            audioSource.loop = true;
            audioSource.volume = 0.0f;
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }
    }

    // t = 0..1 — уровень заряда
    public void UpdateCharge(float t)
    {
        if (!isCharging) return;
        currentT = Mathf.Clamp01(t);
        float k = intensityCurve.Evaluate(currentT);

        // Tint / Emission
        ApplyTint(k);
        if (useEmissionMaterial) SetEmission(k);

        // Particles
        if (sparks)
        {
            var e = sparks.emission; e.rateOverTime = sparksMaxRate * k;
        }
        if (heat)
        {
            var m = heat.main; m.startSize = Mathf.Lerp(0.2f, heatMaxSize, k);
        }

#if USING_UNIVERSAL_RENDER_PIPELINE
        if (auraLight)
        {
            auraLight.intensity = maxIntensity * k;
            auraLight.pointLightOuterRadius = Mathf.Lerp(minRadius, maxRadius, k);
            auraLight.color = lightColor.Evaluate(k);
        }
        if (localVolume)
        {
            localVolume.weight = Mathf.Lerp(0f, maxVolumeWeight, k);
        }
#endif

        // Audio
        if (audioSource && audioSource.isPlaying)
        {
            audioSource.volume = Mathf.Lerp(0f, maxVolume, k);
            audioSource.pitch = Mathf.Lerp(1.0f, maxPitch, k);
        }
    }

    public void Release()
    {
        if (!isCharging) return;
        isCharging = false;

        // мгновенная вспышка + плавное затухание всего
        if (releaseClip && audioSource) audioSource.PlayOneShot(releaseClip, 0.9f);
        StartCoroutine(FadeOutFX());
    }

    public void Cancel()
    {
        if (!isCharging) return;
        isCharging = false;
        StartCoroutine(FadeOutFX());
    }

    IEnumerator FadeOutFX()
    {
        float t = 0f;
        float startK = intensityCurve.Evaluate(currentT);
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            float k = burstFadeCurve.Evaluate(t);
            float v = startK * k;

            ApplyTint(v);
            if (useEmissionMaterial) SetEmission(v);

#if USING_UNIVERSAL_RENDER_PIPELINE
            if (auraLight)
            {
                auraLight.intensity = maxIntensity * v;
                auraLight.pointLightOuterRadius = Mathf.Lerp(minRadius, maxRadius, v);
                auraLight.color = lightColor.Evaluate(v);
            }
            if (localVolume) localVolume.weight = Mathf.Lerp(0f, maxVolumeWeight, v);
#endif
            if (sparks) { var e = sparks.emission; e.rateOverTime = sparksMaxRate * v; }
            if (heat) { var m = heat.main; m.startSize = Mathf.Lerp(0.2f, heatMaxSize, v); }

            yield return null;
        }

        // Сброс к базовому
        ApplyTint(0f);
        if (useEmissionMaterial && normalMat != null)
            foreach (var sr in bodySprites) if (sr) sr.material = normalMat;

#if USING_UNIVERSAL_RENDER_PIPELINE
        if (auraLight) auraLight.enabled = false;
        if (localVolume) localVolume.weight = 0f;
#endif
        if (sparks) sparks.Stop();
        if (heat) heat.Stop();

        if (audioSource && audioSource.loop) audioSource.Stop();
    }

    void ApplyTint(float k)
    {
        if (bodySprites == null) return;
        var c = tintGradient.Evaluate(k);
        float strength = maxTintStrength * k;
        foreach (var sr in bodySprites)
        {
            if (!sr) continue;
            // смешиваем базовый цвет с огненным тоном
            Color baseC = Color.white;
            Color target = Color.Lerp(baseC, c, strength);
            sr.color = target;
        }
    }

    void SetEmission(float k)
    {
        if (!useEmissionMaterial || emissiveMat == null) return;
        // Часто параметр называется "_EmissionStrength" или "_Glow"
        float emission = Mathf.Lerp(0f, 1.5f, k);
        foreach (var sr in bodySprites)
        {
            if (!sr) continue;
            if (sr.material.HasProperty("_EmissionStrength"))
                sr.material.SetFloat("_EmissionStrength", emission);
            if (sr.material.HasProperty("_Glow"))
                sr.material.SetFloat("_Glow", emission);
            if (sr.material.HasProperty("_EmissionColor"))
                sr.material.SetColor("_EmissionColor", Color.Lerp(new Color(1f, 0.4f, 0.0f), Color.yellow, k));
        }
    }
}
