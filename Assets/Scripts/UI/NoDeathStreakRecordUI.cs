using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-9000)]
public sealed class NoDeathStreakRecordUI : MonoBehaviour
{
    private const string RuntimeRootName = "NoDeathStreakRecordUI_Auto";
    private const string RuntimeCanvasName = "NoDeathStreakRecordCanvas";
    private const float FixedAlphaMultiplier = 0.3f;

    private const string EnglishFontName = "CinzelDecorative-Black SDF";
    private const string RussianFontName = "LiberationSans SDF";

#if UNITY_EDITOR
    private const string EnglishFontAssetPath = "Assets/TextMesh Pro/Fonts/Cinzel/CinzelDecorative-Black SDF.asset";
    private const string RussianFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
#endif

    private static NoDeathStreakRecordUI _instance;

    private RectTransform _panelRoot;
    private TMP_Text _titleText;
    private TMP_Text _valueText;
    private TMP_Text _descriptionText;
    private Image _panelImage;

    private Canvas _runtimeCanvas;
    private RectTransform _runtimeCanvasRect;
    private Canvas _interLevelCanvas;

    private Color _basePanelColor = new Color(0f, 0f, 0f, 0.38f);
    private Color _baseTextColor = new Color(1f, 1f, 1f, 0.67f);

    private TMP_FontAsset _englishFont;
    private TMP_FontAsset _russianFont;
    private Material _englishMaterial;
    private Material _russianMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null)
            return;

        var existing = FindObjectOfType<NoDeathStreakRecordUI>();
        if (existing != null)
        {
            _instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        var go = new GameObject(RuntimeRootName);
        _instance = go.AddComponent<NoDeathStreakRecordUI>();
        DontDestroyOnLoad(go);
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

        BuildUi();
        ApplyPreferredFont();
        Refresh();
    }

    private void OnEnable()
    {
        NoDeathStreakRecord.OnChanged += Refresh;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        NoDeathStreakRecord.OnChanged -= Refresh;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReattachToRuntimeCanvas();
        Refresh();
    }

    private void BuildUi()
    {
        ReattachToRuntimeCanvas();
    }

    private void ReattachToRuntimeCanvas()
    {
        EnsureRuntimeCanvas();
        if (_runtimeCanvasRect == null)
            return;

        if (_panelRoot == null)
        {
            CreatePanel(_runtimeCanvasRect);
        }
        else if (_panelRoot.parent != _runtimeCanvasRect)
        {
            _panelRoot.SetParent(_runtimeCanvasRect, false);
        }

        ResolveInterLevelCanvas();
        UpdateCanvasSorting();
        PlaceAndSortPanel();
    }

    private void EnsureRuntimeCanvas()
    {
        if (_runtimeCanvas == null)
        {
            _runtimeCanvas = GetComponentInChildren<Canvas>(true);
            if (_runtimeCanvas == null)
            {
                var canvasGo = new GameObject(RuntimeCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                _runtimeCanvas = canvasGo.GetComponent<Canvas>();
            }
        }

        _runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _runtimeCanvas.pixelPerfect = false;
        _runtimeCanvas.overrideSorting = true;
        _runtimeCanvas.sortingOrder = 1;

        var scaler = _runtimeCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        var raycaster = _runtimeCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = false;

        _runtimeCanvasRect = _runtimeCanvas.transform as RectTransform;
    }

    private void ResolveInterLevelCanvas()
    {
        _interLevelCanvas = null;
        var all = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t.name != "InterLevelPanel")
                continue;

            _interLevelCanvas = t.GetComponentInParent<Canvas>(true);
            if (_interLevelCanvas != null)
                return;
        }
    }

    private void UpdateCanvasSorting()
    {
        if (_runtimeCanvas == null)
            return;

        if (_interLevelCanvas != null)
        {
            _runtimeCanvas.sortingLayerID = _interLevelCanvas.sortingLayerID;
            _runtimeCanvas.sortingOrder = _interLevelCanvas.sortingOrder - 20;
            return;
        }

        _runtimeCanvas.sortingLayerID = 0;
        _runtimeCanvas.sortingOrder = 1;
    }

    private void PlaceAndSortPanel()
    {
        if (_panelRoot == null)
            return;

        _panelRoot.anchorMin = new Vector2(1f, 1f);
        _panelRoot.anchorMax = new Vector2(1f, 1f);
        _panelRoot.pivot = new Vector2(1f, 1f);
        _panelRoot.anchoredPosition = new Vector2(-6f, -4f);
        _panelRoot.localScale = Vector3.one;
        _panelRoot.localRotation = Quaternion.identity;
        _panelRoot.SetAsLastSibling();
    }

    private void CreatePanel(RectTransform parent)
    {
        var panelGo = new GameObject("NoDeathRecordPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        _panelRoot = panelGo.GetComponent<RectTransform>();

        _panelRoot.anchorMin = new Vector2(1f, 1f);
        _panelRoot.anchorMax = new Vector2(1f, 1f);
        _panelRoot.pivot = new Vector2(1f, 1f);
        _panelRoot.anchoredPosition = new Vector2(-6f, -4f);
        _panelRoot.sizeDelta = new Vector2(272f, 96f);

        _panelImage = panelGo.GetComponent<Image>();
        _panelImage.color = _basePanelColor;
        _panelImage.raycastTarget = false;

        _titleText = CreateText("Title", _panelRoot, new Vector2(-8f, -7f), new Vector2(220f, 24f), 17f, TextAlignmentOptions.TopRight, true);
        _valueText = CreateText("Value", _panelRoot, new Vector2(-8f, -36f), new Vector2(220f, 30f), 29f, TextAlignmentOptions.TopRight, false);
        _descriptionText = CreateText("Description", _panelRoot, new Vector2(-8f, 5f), new Vector2(220f, 20f), 15f, TextAlignmentOptions.BottomRight, false);

        SetAnchorsTopRight(_titleText.rectTransform);
        SetAnchorsTopRight(_valueText.rectTransform);
        SetAnchorsBottomRight(_descriptionText.rectTransform);
    }

    private static void SetAnchorsTopRight(RectTransform rect)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
    }

    private static void SetAnchorsBottomRight(RectTransform rect)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
    }

    private TMP_Text CreateText(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, bool isBold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.color = _baseTextColor;
        text.outlineColor = Color.black;
        text.outlineWidth = 0f;
        text.raycastTarget = false;
        return text;
    }

    private void ApplyPreferredFont()
    {
#if UNITY_EDITOR
        _englishFont = TryLoadFontAssetByPathEditor(EnglishFontAssetPath);
        _russianFont = TryLoadFontAssetByPathEditor(RussianFontAssetPath);
#endif

        if (_englishFont == null)
            _englishFont = FindFontByName(EnglishFontName);
        if (_russianFont == null)
            _russianFont = FindFontByName(RussianFontName);

        if (_englishFont == null)
            _englishFont = TMP_Settings.defaultFontAsset;
        if (_russianFont == null)
            _russianFont = TMP_Settings.defaultFontAsset;

        _englishMaterial = _englishFont != null ? _englishFont.material : null;
        _russianMaterial = _russianFont != null ? _russianFont.material : null;
    }

    private static TMP_FontAsset FindFontByName(string fontName)
    {
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
        {
            var font = fonts[i];
            if (font != null && string.Equals(font.name, fontName, StringComparison.OrdinalIgnoreCase))
                return font;
        }

        return null;
    }

#if UNITY_EDITOR
    private static TMP_FontAsset TryLoadFontAssetByPathEditor(string path)
    {
        if (!string.IsNullOrEmpty(path))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        return null;
    }
#endif

    private void Refresh()
    {
        ReattachToRuntimeCanvas();

        if (_titleText == null || _valueText == null || _descriptionText == null)
            return;

        bool isRussian = TooltipLocalization.CurrentLanguage == TooltipLanguage.Russian;
        TMP_FontAsset font = isRussian ? _russianFont : _englishFont;
        Material material = isRussian ? _russianMaterial : _englishMaterial;

        ApplyFontPair(_titleText, font, material);
        ApplyFontPair(_valueText, font, material);
        ApplyFontPair(_descriptionText, font, material);

        _titleText.text = isRussian ? "\u041B\u0423\u0427\u0428\u0418\u0419 \u0420\u0415\u041A\u041E\u0420\u0414 \u0411\u0415\u0417 \u0421\u041C\u0415\u0420\u0422\u0415\u0419" : "BEST NO-DEATH RECORD";
        _valueText.text = NoDeathStreakRecord.BestStreak.ToString();
        _descriptionText.text = isRussian ? "\u0423\u0420\u041E\u0412\u041D\u0415\u0419 \u041F\u041E\u0414\u0420\u042F\u0414 \u0411\u0415\u0417 \u0421\u041C\u0415\u0420\u0422\u0418" : "LEVELS CLEARED IN A ROW";

        ApplyAlphaMultiplier(FixedAlphaMultiplier);
    }

    private static void ApplyFontPair(TMP_Text text, TMP_FontAsset font, Material material)
    {
        if (text == null)
            return;

        if (font != null)
            text.font = font;

        if (material != null)
            text.fontSharedMaterial = material;
        else if (font != null && font.material != null)
            text.fontSharedMaterial = font.material;
    }

    private void ApplyAlphaMultiplier(float multiplier)
    {
        float clamped = Mathf.Clamp01(multiplier);

        if (_panelImage != null)
        {
            Color c = _basePanelColor;
            c.a *= clamped;
            _panelImage.color = c;
        }

        ApplyTextAlpha(_titleText, clamped);
        ApplyTextAlpha(_valueText, clamped);
        ApplyTextAlpha(_descriptionText, clamped);
    }

    private void ApplyTextAlpha(TMP_Text text, float multiplier)
    {
        if (text == null)
            return;

        Color c = _baseTextColor;
        c.a *= multiplier;
        text.color = c;
    }
}



