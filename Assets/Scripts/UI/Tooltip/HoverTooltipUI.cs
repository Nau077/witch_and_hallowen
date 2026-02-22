using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverTooltipUI : MonoBehaviour
{
    public static bool HasInstance => _instance != null;

    public static HoverTooltipUI Instance
    {
        get
        {
            if (_isQuitting) return null;

            if (_instance == null)
            {
                _instance = FindObjectOfType<HoverTooltipUI>();
                if (_instance == null)
                {
                    var go = new GameObject("HoverTooltipUI");
                    _instance = go.AddComponent<HoverTooltipUI>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private Vector2 mouseOffset = new Vector2(24f, -10f);
    [SerializeField] private float globalRightOffsetPx = 25f;
    [SerializeField] private float showDelayDefault = 0.35f;

    private static HoverTooltipUI _instance;
    private static bool _isQuitting;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _root;
    private TMP_Text _titleText;
    private TMP_Text _levelText;
    private TMP_Text _priceText;
    private TMP_Text _descText;
    private Coroutine _showRoutine;
    private HoverTooltipTrigger _activeTrigger;
    private bool _visible;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _isQuitting = false;

        if (transform.parent != null)
            transform.SetParent(null, false);

        DontDestroyOnLoad(gameObject);
        EnsureView();
        HideImmediate();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void Update()
    {
        if (!_visible || _root == null || _canvasRect == null) return;
        PlaceNearMouse();

        if (_activeTrigger == null) return;
        if (!_activeTrigger.isActiveAndEnabled) { HideFrom(_activeTrigger); return; }

        var data = _activeTrigger.BuildData();
        if (data.IsEmpty()) { HideFrom(_activeTrigger); return; }
        ApplyData(data);
    }

    public void ShowFrom(HoverTooltipTrigger trigger)
    {
        if (_isQuitting) return;
        if (trigger == null) return;
        EnsureView();

        if (_activeTrigger == trigger && (_showRoutine != null || _visible))
            return;

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _activeTrigger = trigger;
        float delay = trigger.ShowDelay > 0f ? trigger.ShowDelay : showDelayDefault;
        _showRoutine = StartCoroutine(ShowRoutine(trigger, delay));
    }

    public void HideFrom(HoverTooltipTrigger trigger)
    {
        if (trigger != null && trigger != _activeTrigger) return;

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        _activeTrigger = null;
        HideImmediate();
    }

    private IEnumerator ShowRoutine(HoverTooltipTrigger trigger, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (trigger == null || trigger != _activeTrigger || !trigger.isActiveAndEnabled)
        {
            _showRoutine = null;
            yield break;
        }

        var data = trigger.BuildData();
        if (data.IsEmpty())
        {
            _showRoutine = null;
            yield break;
        }

        ApplyData(data);
        _root.gameObject.SetActive(true);
        _visible = true;
        PlaceNearMouse();

        _showRoutine = null;
    }

    private void ApplyData(HoverTooltipData data)
    {
        if (_titleText != null) _titleText.text = data.title ?? "";
        if (_levelText != null) _levelText.text = data.levelLine ?? "";
        if (_priceText != null) _priceText.text = data.priceLine ?? "";
        if (_descText != null) _descText.text = data.description ?? "";
    }

    private void HideImmediate()
    {
        _visible = false;
        if (_root != null) _root.gameObject.SetActive(false);
    }

    private void EnsureView()
    {
        if (_root != null && _canvas != null) return;

        var canvasGo = new GameObject("TooltipCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = short.MaxValue;
        canvasGo.AddComponent<GraphicRaycaster>();
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _canvasRect = _canvas.GetComponent<RectTransform>();

        var rootGo = new GameObject("TooltipRoot");
        rootGo.transform.SetParent(_canvas.transform, false);
        _root = rootGo.AddComponent<RectTransform>();
        _root.pivot = new Vector2(0f, 1f);
        _root.anchorMin = new Vector2(0.5f, 0.5f);
        _root.anchorMax = new Vector2(0.5f, 0.5f);

        var bg = rootGo.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.17f, 0.96f);
        bg.raycastTarget = false;

        var outline = rootGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.8f, 0.55f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        var vlg = rootGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.spacing = 2f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        var fitter = rootGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _titleText = CreateLine("Title", 22, FontStyles.Bold, new Color(1f, 0.96f, 0.75f, 1f));
        _levelText = CreateLine("Level", 18, FontStyles.Normal, new Color(0.86f, 0.93f, 1f, 1f));
        _priceText = CreateLine("Price", 18, FontStyles.Normal, new Color(0.85f, 1f, 0.82f, 1f));
        _descText = CreateLine("Desc", 17, FontStyles.Normal, new Color(0.92f, 0.92f, 0.92f, 1f));
        _descText.enableWordWrapping = true;

        var maxWidth = _descText.gameObject.AddComponent<LayoutElement>();
        maxWidth.preferredWidth = 340f;
    }

    private TMP_Text CreateLine(string name, float size, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root, false);

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.raycastTarget = false;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.text = "";
        txt.alignment = TextAlignmentOptions.Left;
        txt.enableWordWrapping = false;
        return txt;
    }

    private void PlaceNearMouse()
    {
        if (_root == null || _canvasRect == null) return;

        Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, Input.mousePosition, cam, out var local))
            return;

        var size = _root.rect.size;
        var pos = local + mouseOffset;
        pos.x += globalRightOffsetPx;

        float minX = -_canvasRect.rect.width * 0.5f;
        float maxX = _canvasRect.rect.width * 0.5f - size.x;
        float minY = -_canvasRect.rect.height * 0.5f + size.y;
        float maxY = _canvasRect.rect.height * 0.5f;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        _root.anchoredPosition = pos;
    }
}

public class HoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public float ShowDelay => _showDelay;

    [SerializeField] private float _showDelay = 0.35f;
    private System.Func<HoverTooltipData> _provider;
    private bool _hovered;

    public void Bind(System.Func<HoverTooltipData> provider, float showDelay = 0.35f)
    {
        _provider = provider;
        _showDelay = Mathf.Max(0f, showDelay);
    }

    public HoverTooltipData BuildData()
    {
        return _provider != null ? _provider.Invoke() : default;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        HoverTooltipUI.Instance.ShowFrom(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        HoverTooltipUI.Instance.HideFrom(this);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!_hovered) return;
    }

    private void OnDisable()
    {
        if (HoverTooltipUI.HasInstance && HoverTooltipUI.Instance != null)
            HoverTooltipUI.Instance.HideFrom(this);
    }
}
