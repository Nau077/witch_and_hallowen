using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-8500)]
public sealed class IrisScreenTransition : MonoBehaviour
{
    private const string RuntimeRootName = "IrisScreenTransition_Auto";
    private const string ShaderName = "UI/IrisTransitionHole";
    private const float HiddenRadius = 1.6f;

    private static IrisScreenTransition _instance;

    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private float feather = 0.02f;
    [SerializeField] private Vector2 center = new Vector2(0.5f, 0.5f);
    [SerializeField] private bool useUnscaledTime = true;

    private Canvas _canvas;
    private RawImage _image;
    private Material _material;
    private Coroutine _routine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null)
            return;

        var go = new GameObject(RuntimeRootName);
        _instance = go.AddComponent<IrisScreenTransition>();
        DontDestroyOnLoad(go);
    }

    public static IEnumerator PlayTransition(float closeDuration, float openDuration, float holdBlackDuration = 0f)
    {
        if (_instance == null)
            AutoCreate();

        if (_instance == null)
            yield break;

        yield return _instance.CloseRoutine(closeDuration);
        yield return _instance.HoldRoutine(holdBlackDuration);
        yield return _instance.OpenRoutine(openDuration);
    }

    public static IEnumerator Close(float closeDuration)
    {
        if (_instance == null)
            AutoCreate();

        if (_instance == null)
            yield break;

        yield return _instance.CloseRoutine(closeDuration);
    }

    public static IEnumerator Open(float openDuration)
    {
        if (_instance == null)
            AutoCreate();

        if (_instance == null)
            yield break;

        yield return _instance.OpenRoutine(openDuration);
    }

    public static IEnumerator HoldBlack(float holdBlackDuration)
    {
        if (_instance == null)
            AutoCreate();

        if (_instance == null)
            yield break;

        yield return _instance.HoldRoutine(holdBlackDuration);
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
        EnsureUi();
        SetRadius(HiddenRadius);
        if (_image != null)
            _image.enabled = false;
    }

    private IEnumerator CloseRoutine(float closeDuration)
    {
        EnsureUi();
        if (_image == null || _material == null)
            yield break;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _image.enabled = true;
        SetRadius(HiddenRadius);
        yield return AnimateRadius(HiddenRadius, 0f, Mathf.Max(0.01f, closeDuration));
        SetRadius(0f);
    }

    private IEnumerator OpenRoutine(float openDuration)
    {
        EnsureUi();
        if (_image == null || _material == null)
            yield break;

        _image.enabled = true;
        SetRadius(0f);
        yield return AnimateRadius(0f, HiddenRadius, Mathf.Max(0.01f, openDuration));
        SetRadius(HiddenRadius);
        _image.enabled = false;
    }

    private IEnumerator HoldRoutine(float holdBlackDuration)
    {
        float hold = Mathf.Max(0f, holdBlackDuration);
        if (hold <= 0f)
            yield break;

        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(hold);
        else
            yield return new WaitForSeconds(hold);
    }

    private IEnumerator AnimateRadius(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            SetRadius(Mathf.Lerp(from, to, k));
            yield return null;
        }

        SetRadius(to);
    }

    private void EnsureUi()
    {
        if (_canvas == null)
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null)
                _canvas = gameObject.AddComponent<Canvas>();

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;
        }

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        if (_image == null)
        {
            var child = transform.Find("IrisOverlay");
            if (child == null)
            {
                var overlay = new GameObject("IrisOverlay", typeof(RectTransform), typeof(RawImage));
                overlay.transform.SetParent(transform, false);
                child = overlay.transform;
            }

            var rect = child as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            _image = child.GetComponent<RawImage>();
            if (_image == null)
                _image = child.gameObject.AddComponent<RawImage>();

            _image.raycastTarget = false;
            _image.color = Color.white;
        }

        if (_material == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[IrisScreenTransition] Shader not found: " + ShaderName);
                return;
            }

            _material = new Material(shader) { hideFlags = HideFlags.DontSave };
            _image.material = _material;
        }

        _material.SetColor("_Color", overlayColor);
        _material.SetFloat("_Feather", Mathf.Max(0.0001f, feather));
        _material.SetVector("_Center", new Vector4(center.x, center.y, 0f, 0f));
    }

    private void SetRadius(float radius)
    {
        if (_material == null)
            return;

        _material.SetFloat("_Radius", Mathf.Max(0f, radius));
    }
}
