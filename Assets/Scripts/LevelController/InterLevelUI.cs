using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InterLevelUI : MonoBehaviour
{
    [Header("Progress text (0 -> I -> II ...)")]
    public TMP_Text progressText;

    [Header("Marker visuals")]
    public Vector2 markerSize = new Vector2(16, 16);

    [Tooltip("Смещение маркеров по Y (меньше = ниже). Например -14 опустит ниже.")]
    public float markerYOffset = -14f;

    [Tooltip("Смещение маркеров по X для тонкой подстройки.")]
    public float markerXOffset = 0f;

    [Header("Shop marker sprites")]
    public Sprite coinsMarkerSprite;
    public Sprite coinsAndSoulsMarkerSprite;

    private RectTransform _progressRect;
    private RectTransform _markersRow;
    private Image[] _shopMarkers;

    // stageArrowIndex -> charIndex in TMP text for arrow marker
    // stageArrowIndex = 1 means arrow "0 -> I"
    // stageArrowIndex = 2 means arrow "I -> II" etc.
    private int[] _arrowCharIndexForStage;
    private Vector2[] _arrowPosCache; // cached anchored positions in local space of progressText

    private int _cachedTotalStages = -1;
    private int _cachedCurrentStage = -999;
    private string _cachedText = null;

    public void SetProgress(int currentStage, int totalStages)
    {
        EnsureRefs();
        if (progressText == null) return;

        // Не делаем лишних перестроений
        if (_cachedTotalStages == totalStages && _cachedCurrentStage == currentStage && _cachedText == progressText.text)
            return;

        _cachedTotalStages = totalStages;
        _cachedCurrentStage = currentStage;

        string txt = BuildRomanProgressAndCacheArrowMap(totalStages, currentStage);

        // Если текст не изменился — всё равно нам нужно обновить кэш позиций (например, шрифт/масштаб мог поменяться),
        // но обычно это будет редко.
        progressText.text = txt;

        // ВАЖНО: ForceMeshUpdate вызываем ТОЛЬКО тут (а не в ApplyShopSchedule), чтобы не было лагов/мерцаний.
        progressText.ForceMeshUpdate(true, true);
        _cachedText = txt;

        CacheArrowPositions(totalStages);

        // Маркеры могут уже существовать — просто обновим их позиции при следующем ApplyShopSchedule
    }

    /// <summary>
    /// Показ маркеров магазина.
    /// - на базе (stage 0) не показываем
    /// - магазин "после победы на stage N" показываем над стрелкой N->N+1, т.е. над arrowIndex = N+1
    /// </summary>
    public void ApplyShopSchedule(int totalStages)
    {
        EnsureRefs();
        if (progressText == null) return;

        EnsureRow();
        EnsureMarkers(totalStages);

        // База не показывает иконки
        if (_shopMarkers != null && _shopMarkers.Length > 0 && _shopMarkers[0] != null)
            _shopMarkers[0].gameObject.SetActive(false);

        // Магазин может быть после победы на stage 1..(totalStages-1) (после последнего смысла нет для стрелки)
        for (int stage = 1; stage <= totalStages; stage++)
        {
            var mode = ShopKeeperManager.Instance != null
                ? ShopKeeperManager.Instance.GetShopModeForStage(stage)
                : ShopCurrencyMode.None;

            bool show = mode != ShopCurrencyMode.None;
            var img = _shopMarkers[stage];
            img.gameObject.SetActive(show);

            if (!show) continue;

            img.sprite = (mode == ShopCurrencyMode.CoinsOnly) ? coinsMarkerSprite : coinsAndSoulsMarkerSprite;

            // Магазин после stage N рисуем на стрелке N->N+1 = arrowIndex (N+1)
            int arrowIndex = Mathf.Clamp(stage + 1, 1, totalStages);

            Vector2 pos = GetCachedArrowPos(arrowIndex);
            img.rectTransform.anchoredPosition = pos;
        }
    }

    // ---------------- internals ----------------

    private void EnsureRefs()
    {
        if (progressText != null && _progressRect == null)
            _progressRect = progressText.GetComponent<RectTransform>();
    }

    private void EnsureRow()
    {
        if (_progressRect == null) return;

        if (_markersRow == null)
        {
            var go = new GameObject("ShopMarkersRow", typeof(RectTransform));
            go.transform.SetParent(_progressRect, false);
            _markersRow = go.GetComponent<RectTransform>();
        }

        // Ключевой фикс "поехало": делаем row в той же локальной системе, что и TMP
        // (центр, sizeDelta как у текста, anchoredPosition = 0)
        _markersRow.anchorMin = new Vector2(0.5f, 0.5f);
        _markersRow.anchorMax = new Vector2(0.5f, 0.5f);
        _markersRow.pivot = new Vector2(0.5f, 0.5f);
        _markersRow.sizeDelta = _progressRect.rect.size;
        _markersRow.anchoredPosition = Vector2.zero;
        _markersRow.localScale = Vector3.one;
        _markersRow.localRotation = Quaternion.identity;
    }

    private void EnsureMarkers(int totalStages)
    {
        int count = Mathf.Max(0, totalStages) + 1;

        if (_shopMarkers != null && _shopMarkers.Length == count)
            return;

        // Важно: пересоздаём ТОЛЬКО если изменилось totalStages (обычно никогда в рантайме)
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
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.gameObject.SetActive(false);

            _shopMarkers[i] = img;
        }
    }

    private Vector2 GetCachedArrowPos(int arrowIndex)
    {
        if (_arrowPosCache == null || arrowIndex < 0 || arrowIndex >= _arrowPosCache.Length)
            return new Vector2(markerXOffset, markerYOffset);

        return _arrowPosCache[arrowIndex];
    }

    private void CacheArrowPositions(int totalStages)
    {
        _arrowPosCache = new Vector2[totalStages + 1];

        // stage 0 не используем, но положим туда 0,0
        _arrowPosCache[0] = new Vector2(markerXOffset, markerYOffset);

        for (int arrowIndex = 1; arrowIndex <= totalStages; arrowIndex++)
        {
            _arrowPosCache[arrowIndex] = ComputeAnchoredPositionForArrow(arrowIndex);
        }
    }

    private Vector2 ComputeAnchoredPositionForArrow(int stageArrowIndex)
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
            // Если вдруг попали в невидимый, ищем ближайший видимый справа
            int i = charIndex;
            while (i < ti.characterCount && !ti.characterInfo[i].isVisible) i++;
            if (i < ti.characterCount) ch = ti.characterInfo[i];
        }

        float x = (ch.bottomLeft.x + ch.topRight.x) * 0.5f + markerXOffset;
        float y = (ch.bottomLeft.y + ch.topRight.y) * 0.5f + markerYOffset;

        return new Vector2(x, y);
    }

    private string BuildRomanProgressAndCacheArrowMap(int totalStages, int currentStage)
    {
        // arrow index 1..totalStages
        _arrowCharIndexForStage = new int[totalStages + 1];
        for (int i = 0; i < _arrowCharIndexForStage.Length; i++)
            _arrowCharIndexForStage[i] = -1;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // stage 0
        if (currentStage == 0) sb.Append("(0)");
        else sb.Append("0");

        for (int stage = 1; stage <= totalStages; stage++)
        {
            sb.Append(" ");

            // Мы хотим якориться к символу '>' (а не к '-'), чтобы иконка не залезала на римские цифры.
            // Формат: "->"
            int arrowStart = sb.Length;   // позиция начала "->"
            sb.Append("->");
            int gtIndex = arrowStart + 1; // индекс '>'

            sb.Append(" ");

            // arrowIndex = stage (стрелка перед этим stage)
            _arrowCharIndexForStage[stage] = gtIndex;

            string roman = ToRoman(stage);
            if (stage == currentStage)
                roman = "(" + roman + ")";

            sb.Append(roman);
        }

        return sb.ToString();
    }

    private string ToRoman(int number)
    {
        (int value, string symbol)[] map = {
            (1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),
            (100,"C"),(90,"XC"),(50,"L"),(40,"XL"),
            (10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")
        };

        int n = number;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
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
}