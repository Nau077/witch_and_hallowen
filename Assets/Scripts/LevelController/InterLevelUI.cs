using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics; // Stopwatch
using System.Collections;
using System.Collections.Generic;

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

    // ------------------------------------------------------------
    // ✅ Intro POP (все маркеры при входе на stage 1)
    // ------------------------------------------------------------
    [Header("Intro POP (stage 1)")]
    [Tooltip("Включить POP всех маркеров при входе на stage 1.")]
    public bool introPopEnabled = true;

    [Tooltip("Если true — POP запускается через WaitForEndOfFrame (обычно делает анимацию видимой).")]
    public bool introPopUseEndOfFrame = true;

    [Tooltip("Доп. задержка перед стартом POP (сек). 0.05-0.15 часто помогает увидеть анимацию.")]
    [Range(0f, 0.5f)] public float introPopDelay = 0.06f;

    [Tooltip("Во сколько раз увеличиваем маркер в пике (например 1.8 = на 80% больше).")]
    [Range(1f, 4f)] public float introPopScale = 2.2f;

    [Tooltip("Время схлопывания к 1 (сек).")]
    [Range(0.03f, 1f)] public float introPopDownDuration = 0.22f;

    [Tooltip("Доп. 'подпрыгивание' — второй маленький POP после основного (прикольнее и заметнее).")]
    public bool introPopSecondaryBounce = true;

    [Tooltip("Сила второго подпрыгивания (например 1.15).")]
    [Range(1f, 2f)] public float introPopBounceScale = 1.18f;

    [Tooltip("Длительность второго подпрыгивания.")]
    [Range(0.03f, 0.6f)] public float introPopBounceDuration = 0.10f;

    [Tooltip("Использовать unscaled time (не зависит от timeScale).")]
    public bool introPopUseUnscaledTime = true;

    // ------------------------------------------------------------
    // Обычный POP (если вдруг нужен по месту)
    // ------------------------------------------------------------
    [Header("Marker POP animation (per marker)")]
    [Tooltip("Во сколько раз увеличиваем маркер при появлении.")]
    [Range(1f, 3f)] public float markerPopScale = 1.6f;

    [Tooltip("Сколько секунд маркер схлопывается к 1.")]
    [Range(0.03f, 0.4f)] public float markerPopDownDuration = 0.14f;

    [Tooltip("Использовать unscaled time (не зависит от timeScale).")]
    public bool markerPopUseUnscaledTime = true;

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

    // чтобы POP не повторялся бесконечно
    private readonly HashSet<int> _poppedStages = new(); // stage index (1..N), где маркер уже попался
    private readonly Dictionary<int, Coroutine> _popRoutines = new(); // stage -> coroutine

    // ✅ POP всех маркеров ровно один раз при входе на stage 1
    private bool _didIntroPopOnStage1 = false;
    private Coroutine _introPopRoutine;

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

        // ✅ Stage 0 -> маркеров НЕТ + сброс флагов
        if (currentStage <= 0)
        {
            SetMarkersVisible(false);
            _didIntroPopOnStage1 = false;

            if (_introPopRoutine != null)
            {
                StopCoroutine(_introPopRoutine);
                _introPopRoutine = null;
            }
        }
        else
        {
            SetMarkersVisible(true);

            // 1) рисуем маркеры как обычно (позиции/размеры не трогаем)
            ApplyShopSchedule(totalStages, animateNewlyShown: false);

            // 2) если мы ВПЕРВЫЕ вошли на stage 1 — попаем ВСЕ активные маркеры
            if (introPopEnabled && currentStage == 1 && !_didIntroPopOnStage1)
            {
                _didIntroPopOnStage1 = true;

                if (_introPopRoutine != null)
                    StopCoroutine(_introPopRoutine);

                _introPopRoutine = StartCoroutine(IntroPopAllMarkersRoutine(totalStages));
            }
        }

        sw.Stop();
        LogTimingIfNeeded("SetProgress", sw);
    }

    // ------------------------------------------------------------------
    // ✅ Совместимость со старым кодом: RunLevelManager может вызывать старую сигнатуру
    // ------------------------------------------------------------------
    public void ApplyShopSchedule(int totalStages)
    {
        ApplyShopSchedule(totalStages, animateNewlyShown: false);
    }

    /// <summary>
    /// Рисуем ВСЕ маркеры магазинов для стадий 1..totalStages.
    /// (Stage 0 — никогда)
    /// </summary>
    public void ApplyShopSchedule(int totalStages, bool animateNewlyShown)
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

            // позиция на стрелке stage -> stage+1
            int arrowIndex = Mathf.Clamp(stage + 1, 1, totalStages);
            Vector2 pos = GetCachedArrowPos(arrowIndex);
            img.rectTransform.anchoredPosition = pos;

            if (!show)
            {
                if (img.gameObject.activeSelf) img.gameObject.SetActive(false);
                continue;
            }

            bool wasInactive = !img.gameObject.activeSelf;
            if (wasInactive) img.gameObject.SetActive(true);

            img.sprite = (mode == ShopCurrencyMode.CoinsOnly) ? coinsMarkerSprite : coinsAndSoulsMarkerSprite;

            // scale по умолчанию
            img.rectTransform.localScale = Vector3.one;

            // (оставил на будущее, но сейчас ты этим не пользуешься)
            if (animateNewlyShown && wasInactive && !_poppedStages.Contains(stage))
            {
                _poppedStages.Add(stage);
                StartMarkerPop(stage, img.rectTransform);
            }
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
            Transform parent = _progressRect.parent;
            var go = new GameObject("ShopMarkersRow", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _markersRow = go.GetComponent<RectTransform>();

            _markersRow.anchorMin = _progressRect.anchorMin;
            _markersRow.anchorMax = _progressRect.anchorMax;
            _markersRow.pivot = _progressRect.pivot;
            _markersRow.anchoredPosition = _progressRect.anchoredPosition;
            _markersRow.localScale = Vector3.one;
            _markersRow.localRotation = Quaternion.identity;
        }
    }

    void SetMarkersVisible(bool visible)
    {
        if (_markersRow != null && _markersRow.gameObject.activeSelf != visible)
            _markersRow.gameObject.SetActive(visible);
    }

    void UpdateRowSizeIfNeeded()
    {
        if (_markersRow == null || _progressRect == null) return;

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

        // пересборка -> сброс POP состояния
        _poppedStages.Clear();
        foreach (var kv in _popRoutines)
            if (kv.Value != null) StopCoroutine(kv.Value);
        _popRoutines.Clear();

        // и вступительный поп тоже сбросим (на случай пересоздания)
        _didIntroPopOnStage1 = false;
        if (_introPopRoutine != null)
        {
            StopCoroutine(_introPopRoutine);
            _introPopRoutine = null;
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

    // ---------------- POP animation ----------------

    // ✅ Вступительный POP: запускаем НА СЛЕДУЮЩИЙ КАДР и/или с задержкой
    private IEnumerator IntroPopAllMarkersRoutine(int totalStages)
    {
        if (introPopUseEndOfFrame)
            yield return new WaitForEndOfFrame();
        else
            yield return null;

        if (introPopDelay > 0f)
        {
            if (introPopUseUnscaledTime)
            {
                float t = 0f;
                while (t < introPopDelay)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(introPopDelay);
            }
        }

        // На всякий случай: ещё раз обновим позиции (НЕ обязательно, но безопасно)
        // и гарантируем, что scale стартует с 1, перед POP.
        ApplyShopSchedule(totalStages, animateNewlyShown: false);

        // Поупаем все активные маркеры (только те, что реально включены)
        if (_shopMarkers == null) yield break;

        for (int stage = 1; stage <= totalStages; stage++)
        {
            var img = _shopMarkers[stage];
            if (img == null) continue;
            if (!img.gameObject.activeSelf) continue;

            StartIntroPop(stage, img.rectTransform);
        }
    }

    private void StartMarkerPop(int stage, RectTransform rt)
    {
        if (rt == null) return;

        if (_popRoutines.TryGetValue(stage, out var c) && c != null)
            StopCoroutine(c);

        _popRoutines[stage] = StartCoroutine(MarkerPopRoutine(stage, rt, markerPopScale, markerPopDownDuration, markerPopUseUnscaledTime));
    }

    private void StartIntroPop(int stage, RectTransform rt)
    {
        if (rt == null) return;

        if (_popRoutines.TryGetValue(stage, out var c) && c != null)
            StopCoroutine(c);

        _popRoutines[stage] = StartCoroutine(IntroPopRoutine(stage, rt));
    }

    private IEnumerator IntroPopRoutine(int stage, RectTransform rt)
    {
        if (rt == null) yield break;

        // Основной POP
        yield return MarkerPopRoutine(stage, rt, introPopScale, introPopDownDuration, introPopUseUnscaledTime);

        // Второй небольшой bounce (по желанию)
        if (introPopSecondaryBounce && rt != null)
        {
            yield return MarkerPopRoutine(stage, rt, introPopBounceScale, introPopBounceDuration, introPopUseUnscaledTime);
        }
    }

    private IEnumerator MarkerPopRoutine(int stage, RectTransform rt, float scale, float duration, bool useUnscaled)
    {
        if (rt == null) yield break;

        Vector3 start = Vector3.one * Mathf.Max(1f, scale);
        Vector3 end = Vector3.one;

        // важно: НЕ трогаем anchoredPosition/sizeDelta — только scale
        rt.localScale = start;

        float dur = Mathf.Max(0.001f, duration);
        float time = 0f;

        while (time < dur && rt != null)
        {
            float dt = useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            time += dt;

            float k = Mathf.Clamp01(time / dur);
            k = Mathf.Pow(k, 0.35f); // резче к концу

            rt.localScale = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }

        if (rt != null) rt.localScale = Vector3.one;

        _popRoutines.Remove(stage);
    }
}
