using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class NoDeathStreakRecord
{
    private const string KEY_CURRENT_STREAK = "no_death_streak_current";
    private const string KEY_BEST_STREAK = "no_death_streak_best";

    public static event Action OnChanged;

    public static int CurrentStreak => Mathf.Max(0, PlayerPrefs.GetInt(KEY_CURRENT_STREAK, 0));
    public static int BestStreak => Mathf.Max(0, PlayerPrefs.GetInt(KEY_BEST_STREAK, 0));

    public static void RegisterStageCleared()
    {
        int nextCurrent = CurrentStreak + 1;
        int best = BestStreak;

        PlayerPrefs.SetInt(KEY_CURRENT_STREAK, nextCurrent);

        if (nextCurrent > best)
        {
            best = nextCurrent;
            PlayerPrefs.SetInt(KEY_BEST_STREAK, best);
        }

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public static void RegisterDeath()
    {
        if (CurrentStreak == 0)
            return;

        PlayerPrefs.SetInt(KEY_CURRENT_STREAK, 0);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}

[DefaultExecutionOrder(-9000)]
public sealed class NoDeathStreakRecordUI : MonoBehaviour
{
    private const string RUNTIME_ROOT_NAME = "NoDeathStreakRecordUI_Auto";

    private const string ENGLISH_FONT_NAME = "CinzelDecorative-Black SDF";
    private const string RUSSIAN_FONT_NAME = "LiberationSans SDF";

#if UNITY_EDITOR
    private const string ENGLISH_FONT_ASSET_PATH = "Assets/TextMesh Pro/Fonts/Cinzel/CinzelDecorative-Black SDF.asset";
    private const string RUSSIAN_FONT_ASSET_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
#endif

    private static NoDeathStreakRecordUI _instance;

    private TMP_Text _titleText;
    private TMP_Text _valueText;
    private TMP_Text _descriptionText;

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

        var go = new GameObject(RUNTIME_ROOT_NAME);
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
        Refresh();
    }

    private void BuildUi()
    {
        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(transform, false);

        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-24f, -24f);
        panelRect.sizeDelta = new Vector2(420f, 142f);

        var panelImage = panelGo.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.22f);
        panelImage.raycastTarget = false;

        _titleText = CreateText("Title", panelRect, new Vector2(-14f, -10f), new Vector2(390f, 34f), 25f, TextAlignmentOptions.TopRight, true);
        _valueText = CreateText("Value", panelRect, new Vector2(-14f, -52f), new Vector2(390f, 44f), 34f, TextAlignmentOptions.TopRight, false);
        _descriptionText = CreateText("Description", panelRect, new Vector2(-14f, 10f), new Vector2(390f, 30f), 25f, TextAlignmentOptions.BottomRight, false);

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
        text.color = Color.white;
        text.outlineColor = Color.black;
        text.outlineWidth = 0f;
        text.raycastTarget = false;

        return text;
    }

    private void ApplyPreferredFont()
    {
        _englishFont = TryLoadFontAssetByPathEditor(ENGLISH_FONT_ASSET_PATH);
        _russianFont = TryLoadFontAssetByPathEditor(RUSSIAN_FONT_ASSET_PATH);

        if (_englishFont == null)
            _englishFont = FindFontByName(ENGLISH_FONT_NAME);
        if (_russianFont == null)
            _russianFont = FindFontByName(RUSSIAN_FONT_NAME);

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

    private static TMP_FontAsset TryLoadFontAssetByPathEditor(string path)
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(path))
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
#endif
        return null;
    }

    private void Refresh()
    {
        if (_titleText == null || _valueText == null || _descriptionText == null)
            return;

        bool isRussian = TooltipLocalization.CurrentLanguage == TooltipLanguage.Russian;
        TMP_FontAsset font = isRussian ? _russianFont : _englishFont;
        Material material = isRussian ? _russianMaterial : _englishMaterial;

        ApplyFontPair(_titleText, font, material);
        ApplyFontPair(_valueText, font, material);
        ApplyFontPair(_descriptionText, font, material);

        _titleText.text = isRussian ? "Лучший рекорд без смертей" : "BEST NO-DEATH RECORD";
        _valueText.text = NoDeathStreakRecord.BestStreak.ToString();
        _descriptionText.text = isRussian ? "Уровней подряд без смерти" : "LEVELS CLEARED IN A ROW";
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
}
