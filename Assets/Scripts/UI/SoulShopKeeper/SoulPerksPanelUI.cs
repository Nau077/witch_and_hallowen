using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SoulPerksPanelUI
/// ----------------
/// ✔ Сердца идут сверху вниз (VerticalLayoutGroup UpperCenter)
/// ✔ Нормальная высота детей (childControlHeight = true) -> не "1px и пропали"
/// ✔ Настраиваемые spacing/padding/сдвиг X
/// ✔ Постоянное "дыхание" всех активных сердец
/// ✔ При добавлении нового сердца: POP x7 и схлопывание
/// ✔ Пулл объектов
/// </summary>
public class SoulPerksPanelUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform content;     // Content (где стоит VerticalLayoutGroup)
    public GameObject iconPrefab;     // PerkIcon prefab

    [Header("Icons")]
    public Sprite hpStickSprite;

    [Header("Layout tuning")]
    [Tooltip("Отступ сверху ВНУТРИ VerticalLayoutGroup (px).")]
    public int innerTopPaddingPx = 0;

    [Tooltip("Отступ снизу (px).")]
    public int bottomPaddingPx = 0;

    [Tooltip("Отступ слева (px).")]
    public int leftPaddingPx = 0;

    [Tooltip("Отступ справа (px).")]
    public int rightPaddingPx = 0;

    [Tooltip("Расстояние между сердцами (px).")]
    public float spacingPx = 2f;

    [Tooltip("Доп. сдвиг всего Content по X (px).")]
    public float horizontalOffsetPx = 0f;

    [Tooltip("Если true — каждый кадр дожимает LayoutGroup настройки (на случай, если что-то их меняет).")]
    public bool enforceLayoutEveryFrame = true;

    [Header("Force icon size (recommended)")]
    [Tooltip("Если включено — добавит/обновит LayoutElement на иконках и задаст preferred size.")]
    public bool forceIconSize = true;

    [Tooltip("Preferred width для сердца. 0 = взять из sprite.rect.width.")]
    public float forceIconWidth = 0f;

    [Tooltip("Preferred height для сердца. 0 = взять из sprite.rect.height.")]
    public float forceIconHeight = 0f;

    [Header("Breathing (all hearts)")]
    [Range(0f, 0.25f)] public float breatheAmplitude = 0.06f;
    [Range(0.1f, 6f)] public float breatheSpeed = 1.2f;
    public bool breatheUseUnscaledTime = true;

    [Header("New heart POP")]
    [Range(1f, 10f)] public float newHeartPopScale = 7f;
    [Range(0.01f, 0.5f)] public float popDownDuration = 0.12f;
    public bool popUseUnscaledTime = true;

    private VerticalLayoutGroup vlg;

    private readonly List<GameObject> pool = new();
    private readonly Dictionary<GameObject, Coroutine> popRoutines = new();

    private int lastTotalHearts = -1;
    private float breatheT;

    private void Awake()
    {
        CacheRefs();
        ApplyLayoutNow();
        ApplyContentOffset();
    }

    private void OnEnable()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged += Refresh;

        CacheRefs();
        ApplyLayoutNow();
        ApplyContentOffset();
        Refresh();
    }

    private void OnDisable()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (enforceLayoutEveryFrame)
        {
            ApplyLayoutNow();
            ApplyContentOffset();
        }

        TickBreathing();
    }

    public void Refresh()
    {
        CacheRefs();
        ApplyLayoutNow();
        ApplyContentOffset();

        var perks = SoulPerksManager.Instance;
        int totalHearts = perks == null ? 0 : (1 + perks.HpLevel);

        if (lastTotalHearts < 0)
            lastTotalHearts = totalHearts;

        EnsurePoolSize(totalHearts);

        for (int i = 0; i < totalHearts; i++)
        {
            var go = pool[i];
            if (go == null) continue;

            bool wasInactive = !go.activeSelf;
            go.SetActive(true);

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = hpStickSprite;
                img.preserveAspect = true;
                img.enabled = (hpStickSprite != null);
            }

            // ВАЖНО: сброс масштаба, иначе после POP/дыхания может накопиться
            go.transform.localScale = Vector3.one;

            // Размер (чтобы не было 1x1 иконок)
            if (forceIconSize)
                EnsureLayoutElementSize(go, img);

            // POP только на новые сердца (когда количество выросло)
            bool isNew = (totalHearts > lastTotalHearts) && (i >= lastTotalHearts);
            if (isNew)
                Pop(go);
        }

        for (int i = totalHearts; i < pool.Count; i++)
        {
            if (pool[i] != null) pool[i].SetActive(false);
        }

        lastTotalHearts = totalHearts;
    }

    private void CacheRefs()
    {
        if (content == null)
            content = GetComponent<RectTransform>();

        if (vlg == null && content != null)
            vlg = content.GetComponent<VerticalLayoutGroup>();
    }

    private void ApplyLayoutNow()
    {
        if (vlg == null) return;

        // Ключевой фикс: одно сердце должно быть СВЕРХУ, не по центру
        vlg.childAlignment = TextAnchor.UpperCenter;

        // Ключевой фикс: иначе дети остаются Height=1 и "пропадают"
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        // Не растягиваем детей по высоте/ширине "в бесконечность"
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        vlg.spacing = spacingPx;

        var p = vlg.padding;
        p.left = leftPaddingPx;
        p.right = rightPaddingPx;
        p.top = innerTopPaddingPx;
        p.bottom = bottomPaddingPx;
        vlg.padding = p;
    }

    private void ApplyContentOffset()
    {
        if (content == null) return;

        // НЕ ТРОГАЕМ anchors/pivot (чтобы не улетало за пределы)
        var pos = content.anchoredPosition;
        pos.x = horizontalOffsetPx;
        content.anchoredPosition = pos;
    }

    private void EnsurePoolSize(int need)
    {
        if (iconPrefab == null || content == null) return;

        while (pool.Count < need)
        {
            var go = Instantiate(iconPrefab, content);
            go.SetActive(false);
            go.transform.localScale = Vector3.one;

            pool.Add(go);
        }
    }

    private void EnsureLayoutElementSize(GameObject go, Image img)
    {
        if (go == null) return;

        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();

        float w = forceIconWidth;
        float h = forceIconHeight;

        // Если не задано — пытаемся взять из спрайта
        if ((w <= 0f || h <= 0f) && img != null && img.sprite != null)
        {
            var r = img.sprite.rect;
            if (w <= 0f) w = r.width;
            if (h <= 0f) h = r.height;
        }

        // Если вообще нечего взять — оставим как есть (не ставим 0)
        if (w > 0f) le.preferredWidth = w;
        if (h > 0f) le.preferredHeight = h;

        // Также можно зафиксить min, чтобы LayoutGroup не схлопнул
        if (w > 0f) le.minWidth = w;
        if (h > 0f) le.minHeight = h;
    }

    private void TickBreathing()
    {
        float dt = breatheUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        breatheT += dt * breatheSpeed;

        float s = 1f + Mathf.Sin(breatheT * Mathf.PI * 2f) * breatheAmplitude;

        foreach (var go in pool)
        {
            if (go == null || !go.activeSelf) continue;
            if (popRoutines.ContainsKey(go)) continue; // пока POP — не дышим

            go.transform.localScale = new Vector3(s, s, 1f);
        }
    }

    private void Pop(GameObject go)
    {
        if (go == null) return;

        if (popRoutines.TryGetValue(go, out var c) && c != null)
            StopCoroutine(c);

        popRoutines[go] = StartCoroutine(PopRoutine(go));
    }

    private IEnumerator PopRoutine(GameObject go)
    {
        if (go == null) yield break;
        var t = go.transform;

        Vector3 start = Vector3.one * newHeartPopScale;
        Vector3 end = Vector3.one;
        t.localScale = start;

        float dur = Mathf.Max(0.001f, popDownDuration);
        float time = 0f;

        while (time < dur && t != null)
        {
            float dt = popUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            time += dt;

            float k = Mathf.Clamp01(time / dur);
            k = Mathf.Pow(k, 0.35f); // резче к концу

            t.localScale = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }

        if (t != null) t.localScale = Vector3.one;
        popRoutines.Remove(go);
    }
}
