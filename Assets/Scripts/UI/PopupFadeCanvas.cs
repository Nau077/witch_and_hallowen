using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PopupFadeCanvas : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private bool nearInstantMode = true;
    [SerializeField, Min(0f)] private float nearInstantDuration = 0.02f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool disableOnHidden = true;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        EnsureCanvasGroup();
    }

    public void ShowImmediate()
    {
        EnsureCanvasGroup();
        StopFade();
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void HideImmediate()
    {
        EnsureCanvasGroup();
        StopFade();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        if (disableOnHidden)
            gameObject.SetActive(false);
    }

    public void ShowSmooth()
    {
        EnsureCanvasGroup();
        StopFade();
        gameObject.SetActive(true);
        float duration = ResolveDuration(fadeInDuration);
        if (duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            return;
        }

        _fadeRoutine = StartCoroutine(FadeTo(1f, duration));
    }

    public void HideSmooth()
    {
        EnsureCanvasGroup();
        StopFade();
        float duration = ResolveDuration(fadeOutDuration);
        if (duration <= 0f)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (disableOnHidden)
                gameObject.SetActive(false);
            return;
        }

        _fadeRoutine = StartCoroutine(FadeTo(0f, duration));
    }

    private float ResolveDuration(float configured)
    {
        if (nearInstantMode)
            return 0.2f;
        return Mathf.Max(0f, configured);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float start = canvasGroup.alpha;
        float d = Mathf.Max(0.0001f, duration);
        float t = 0f;

        if (targetAlpha > start)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (t < d)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / d);
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        bool shown = targetAlpha >= 0.999f;
        canvasGroup.interactable = shown;
        canvasGroup.blocksRaycasts = shown;

        if (!shown && disableOnHidden)
            gameObject.SetActive(false);

        _fadeRoutine = null;
    }

    private void StopFade()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
}
