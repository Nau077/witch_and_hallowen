using System.Collections;
using UnityEngine;

public class FireballChargeFX : MonoBehaviour
{
    [Header("Auras")]
    [Tooltip("Огненная аура – используется для обычных скиллов.")]
    public GameObject fireAuraRoot;

    [Tooltip("Ледяная аура – используется только для IceShard.")]
    public GameObject iceAuraRoot;

    [SerializeField] private SpriteRenderer[] fireRenderers;
    [SerializeField] private SpriteRenderer[] iceRenderers;

    [Header("Fade")]
    [Tooltip("Максимальная непрозрачность ауры на полном заряде.")]
    public float maxAlpha = 1f;

    [Tooltip("Сколько секунд длится плавное появление/исчезновение.")]
    public float fadeDuration = 0.2f;

    [Tooltip("Выключать ли объект ауры, когда альфа = 0.")]
    public bool deactivateWhenHidden = true;

    private Coroutine fadeRoutine;
    private float currentAlpha = 0f;

    private enum AuraType { None, Fire, Ice }
    private AuraType activeAura = AuraType.None;

    void Awake()
    {
        if (fireAuraRoot && (fireRenderers == null || fireRenderers.Length == 0))
            fireRenderers = fireAuraRoot.GetComponentsInChildren<SpriteRenderer>(true);

        if (iceAuraRoot && (iceRenderers == null || iceRenderers.Length == 0))
            iceRenderers = iceAuraRoot.GetComponentsInChildren<SpriteRenderer>(true);

        SetAllAurasAlphaImmediate(0f);

        if (deactivateWhenHidden)
        {
            if (fireAuraRoot) fireAuraRoot.SetActive(false);
            if (iceAuraRoot) iceAuraRoot.SetActive(false);
        }
    }

    // ===== API, вызываемое PlayerSkillShooter =====

    /// <summary>
    /// Начинаем заряд – отображаем нужную ауру.
    /// </summary>
    public void BeginCharge(bool useIceAura)
    {
        activeAura = useIceAura ? AuraType.Ice : AuraType.Fire;

        if (activeAura == AuraType.Fire)
        {
            if (fireAuraRoot && deactivateWhenHidden) fireAuraRoot.SetActive(true);
            if (iceAuraRoot && deactivateWhenHidden) iceAuraRoot.SetActive(false);
        }
        else
        {
            if (iceAuraRoot && deactivateWhenHidden) iceAuraRoot.SetActive(true);
            if (fireAuraRoot && deactivateWhenHidden) fireAuraRoot.SetActive(false);
        }

        StartFadeTo(0.001f, 0.05f); // лёгкое появление
    }

    /// <summary>
    /// Обновление яркости заряда (t = 0..1).
    /// </summary>
    public void UpdateCharge(float t)
    {
        float target = Mathf.Clamp01(t) * maxAlpha;
        SetAlphaImmediate(target);
    }

    /// <summary>
    /// Выстрел – вспышка и исчезновение.
    /// </summary>
    public void Release()
    {
        StartCoroutine(FlashThenFadeOut());
    }

    /// <summary>
    /// Отмена замаха.
    /// </summary>
    public void Cancel()
    {
        FadeOutAll();
    }

    // ===== внутреннее =====

    IEnumerator FlashThenFadeOut()
    {
        SetAlphaImmediate(maxAlpha);
        yield return new WaitForSeconds(0.05f);
        FadeOutAll();
    }

    void FadeOutAll()
    {
        StartFadeTo(0f, fadeDuration, () =>
        {
            if (deactivateWhenHidden)
            {
                if (fireAuraRoot) fireAuraRoot.SetActive(false);
                if (iceAuraRoot) iceAuraRoot.SetActive(false);
            }
            activeAura = AuraType.None;
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

        if (activeAura == AuraType.Fire)
            SetAlphaOn(fireRenderers, a);
        else if (activeAura == AuraType.Ice)
            SetAlphaOn(iceRenderers, a);
    }

    void SetAllAurasAlphaImmediate(float a)
    {
        SetAlphaOn(fireRenderers, a);
        SetAlphaOn(iceRenderers, a);
    }

    void SetAlphaOn(SpriteRenderer[] arr, float a)
    {
        if (arr == null) return;

        for (int i = 0; i < arr.Length; i++)
        {
            if (!arr[i]) continue;
            var c = arr[i].color;
            c.a = a;
            arr[i].color = c;
        }
    }
}
