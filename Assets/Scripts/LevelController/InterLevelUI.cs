using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics; // Stopwatch

public class InterLevelUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text progressText;

    [Tooltip("Если не задано — создаст ShopMarkersRow рядом с progressText (в родителе).")]
    public RectTransform markersRowOverride;

    [Header("Marker visuals")]
    public Vector2 markerSize = new Vector2(16, 16);
    public float markerYOffset = -14f;
    public float markerXOffset = 0f;

    [Header("Shop marker sprites")]
    public Sprite coinsMarkerSprite;
    public Sprite coinsAndSoulsMarkerSprite;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugLogTimings = true;
    public float logIfMethodTookMsMoreThan = 2f;

    RectTransform _progressRect;
    RectTransform _markersRow;
    Image[] _shopMarkers;

    int[] _arrowCharIndexForStage; // arrowIndex -> charIndex of '>'
    Vector2[] _arrowPosCache;      // arrowIndex -> anchored position

    int _cachedTotalStages = -1;
    int _cachedCurrentStage = -999;
    string _cachedText = null;

    Vector2 _lastProgressRectSize = Vector2.zero;
    int _id;

    void Awake()
    {
        _id = GetInstanceID();
        EnsureRefs();

        if (debugLogs)
            UnityEngine.Debug.Log($"[InterLevelUI] Awake id={_id} scene={gameObject.scene.name}");
    }

    void OnEnable()
    {
        if (debugLogs)
            UnityEngine.Debug.Log($"[InterLevelUI] OnEnable id={_id} timeScale={Time.timeScale}");
    }

    void OnDisable()
    {
        if (debugLogs)
            UnityEngine.Debug.Log($"[InterLevelUI] OnDisable id={_id} timeScale={Time.timeScale}");
    }

    public void SetProgress(int currentStage, int totalStages)
    {
        if (progressText == null) return;

        var sw = Stopwatch.StartNew();

        EnsureRefs();

        string txt = BuildRomanProgressAndCacheArrowMap(totalStages, currentStage);

        // если реально ничего не менялось — НЕ трогаем TMP
        if (_cachedText == txt && _cachedTotalStages == totalStages && _cachedCurrentStage == currentStage)
        {
            if (debugLogs)
                UnityEngine.Debug.Log($"[InterLevelUI] SetProgress SKIP (no changes) stage={currentStage}/{totalStages}");
            return;
        }

        _cachedTotalStages = totalStages;
        _cachedCurrentStage = currentStage;
        _cachedText = txt;

        progressText.text = txt;

        // ForceMeshUpdate — только когда текст поменялся
        progressText.ForceMeshUpdate(true, true);

        CacheArrowPositions(totalStages);

        EnsureRow();
        UpdateRowSizeIfNeeded();

        sw.Stop();
        LogTimingIfNeeded("SetProgress", sw);
    }

    public void ApplyShopSchedule(int totalStages)
    {
        if (progressText == null) return;

        var sw = Stopwatch.StartNew();

        EnsureRefs();
        EnsureRow();
        UpdateRowSizeIfNeeded();
        EnsureMarkers(totalStages);

        // база — никогда
        if (_shopMarkers != null && _shopMarkers.Length > 0 && _shopMarkers[0] != null)
            _shopMarkers[0].gameObject.SetActive(false);

        for (int stage = 1; stage <= totalStages; stage++)
        {
            var mode = ShopKeeperManager.Instance != null
                ? ShopKeeperManager.Instance.GetShopModeForStage(stage)
                : ShopCurrencyMode.None;

            bool show = mode != ShopCurrencyMode.None;

            var img = _shopMarkers[stage];
            if (img == null) continue;

            if (img.gameObject.activeSelf != show)
                img.gameObject.SetActive(show);

            if (!show) continue;

            img.sprite = (mode == ShopCurrencyMode.CoinsOnly) ? coinsMarkerSprite : coinsAndSoulsMarkerSprite;

            // магазин после stage N -> на стрелке N->N+1 => arrowIndex = N+1
            int arrowIndex = Mathf.Clamp(stage + 1, 1, totalStages);

            Vector2 pos = GetCachedArrowPos(arrowIndex);
            img.rectTransform.anchoredPosition = pos;
        }

        sw.Stop();
        LogTimingIfNeeded("ApplyShopSchedule", sw);
    }

    // ---------------- internals ----------------

    void EnsureRefs()
    {
        if (progressText != null && _progressRect == null)
            _progressRect = progressText.GetComponent<RectTransform>();
    }

    void EnsureRow()
    {
        if (_progressRect == null) return;

        if (markersRowOverride != null)
        {
            _markersRow = markersRowOverride;
            return;
        }

        if (_markersRow == null)
        {
            // создаём НЕ под TMP, а рядом (в родителе)
            Transform parent = _progressRect.parent;
            var go = new GameObject("ShopMarkersRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _markersRow = go.GetComponent<RectTransform>();

            // копируем трансформ у текста
            _markersRow.anchorMin = _progressRect.anchorMin;
            _markersRow.anchorMax = _progressRect.anchorMax;
            _markersRow.pivot = _progressRect.pivot;
            _markersRow.anchoredPosition = _progressRect.anchoredPosition;
            _markersRow.localScale = Vector3.one;
            _markersRow.localRotation = Quaternion.identity;
        }
    }

    void UpdateRowSizeIfNeeded()
    {
        if (_markersRow == null || _progressRect == null) return;

        // если прогрессText растянут (stretch) — rect.size меняется
        Vector2 sz = _progressRect.rect.size;
        if (sz == _lastProgressRectSize) return;

        _lastProgressRectSize = sz;
        _markersRow.sizeDelta = sz;
    }

    void EnsureMarkers(int totalStages)
    {
        int count = Mathf.Max(0, totalStages) + 1;

        if (_shopMarkers != null && _shopMarkers.Length == count)
            return;

        // пересоздаём только если изменилось число стадий
        if (_markersRow != null)
        {
            for (int i = _markersRow.childCount - 1; i >= 0; i--)
                Destroy(_markersRow.GetChild(i).gameObject);
        }

        _shopMarkers = new Image[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"ShopMarker_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_markersRow, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = markerSize;

            // важное: якоримся в центр строки (как у текста)
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.gameObject.SetActive(false);

            _shopMarkers[i] = img;
        }
    }

    Vector2 GetCachedArrowPos(int arrowIndex)
    {
        if (_arrowPosCache == null || arrowIndex < 0 || arrowIndex >= _arrowPosCache.Length)
            return new Vector2(markerXOffset, markerYOffset);

        return _arrowPosCache[arrowIndex];
    }

    void CacheArrowPositions(int totalStages)
    {
        _arrowPosCache = new Vector2[totalStages + 1];
        _arrowPosCache[0] = new Vector2(markerXOffset, markerYOffset);

        for (int arrowIndex = 1; arrowIndex <= totalStages; arrowIndex++)
            _arrowPosCache[arrowIndex] = ComputeAnchoredPositionForArrow(arrowIndex);
    }

    Vector2 ComputeAnchoredPositionForArrow(int stageArrowIndex)
    {
        if (_arrowCharIndexForStage == null || _arrowCharIndexForStage.Length <= stageArrowIndex)
            return new Vector2(markerXOffset, markerYOffset);

        int charIndex = _arrowCharIndexForStage[stageArrowIndex];
        if (charIndex < 0)
            return new Vector2(markerXOffset, markerYOffset);

        var ti = progressText.textInfo;
        if (ti == null || ti.characterCount == 0)
            return new Vector2(markerXOffset, markerYOffset);

        charIndex = Mathf.Clamp(charIndex, 0, ti.characterCount - 1);

        var ch = ti.characterInfo[charIndex];
        if (!ch.isVisible)
        {
            int i = charIndex;
            while (i < ti.characterCount && !ti.characterInfo[i].isVisible) i++;
            if (i < ti.characterCount) ch = ti.characterInfo[i];
        }

        // TMP координаты — в локальном пространстве текста.
        // Мы копируем размер/позицию в row, поэтому это совпадает.
        float x = (ch.bottomLeft.x + ch.topRight.x) * 0.5f + markerXOffset;
        float y = (ch.bottomLeft.y + ch.topRight.y) * 0.5f + markerYOffset;

        return new Vector2(x, y);
    }

    string BuildRomanProgressAndCacheArrowMap(int totalStages, int currentStage)
    {
        _arrowCharIndexForStage = new int[totalStages + 1];
        for (int i = 0; i < _arrowCharIndexForStage.Length; i++)
            _arrowCharIndexForStage[i] = -1;

        var sb = new System.Text.StringBuilder();

        sb.Append(currentStage == 0 ? "(0)" : "0");

        for (int stage = 1; stage <= totalStages; stage++)
        {
            sb.Append(" ");

            int arrowStart = sb.Length;
            sb.Append("->");
            int gtIndex = arrowStart + 1; // '>'

            sb.Append(" ");

            _arrowCharIndexForStage[stage] = gtIndex;

            string roman = ToRoman(stage);
            if (stage == currentStage)
                roman = "(" + roman + ")";

            sb.Append(roman);
        }

        return sb.ToString();
    }

    string ToRoman(int number)
    {
        (int value, string symbol)[] map = {
            (1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),
            (100,"C"),(90,"XC"),(50,"L"),(40,"XL"),
            (10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")
        };

        int n = number;
        var sb = new System.Text.StringBuilder();
        foreach (var (value, symbol) in map)
        {
            while (n >= value)
            {
                sb.Append(symbol);
                n -= value;
            }
        }
        return sb.ToString();
    }

    void LogTimingIfNeeded(string method, Stopwatch sw)
    {
        if (!debugLogTimings) return;

        float ms = (float)sw.Elapsed.TotalMilliseconds;
        if (ms >= logIfMethodTookMsMoreThan)
        {
            UnityEngine.Debug.LogWarning(
                $"[InterLevelUI] {method} took {ms:0.00} ms | " +
                $"stage={_cachedCurrentStage}/{_cachedTotalStages} id={_id}"
            );
        }
        else if (debugLogs)
        {
            UnityEngine.Debug.Log($"[InterLevelUI] {method} {ms:0.00} ms");
        }
    }
}
