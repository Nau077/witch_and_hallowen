using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SoulPerksPanelUI (объединённая панель для HP, Mana, Stamina)
/// --------
/// ✔ Отображает 3 типа сердец вертикально стопкой:
///   - Красные (HP) сверху
///   - Синие (Mana) посередине
///   - Жёлтые (Stamina) снизу
/// ✔ Каждый тип имеет свою строку (VerticalLayoutGroup)
/// ✔ При добавлении нового сердца: POP x7 и схлопывание
/// ✔ Пулл объектов для каждого типа
/// ✔ Дыхание всех активных сердец
/// </summary>
public class SoulPerksPanelUI : MonoBehaviour
{
    [Header("UI Containers")]
    public RectTransform hpContent;
    public RectTransform manaContent;
    public RectTransform staminaContent;

    public GameObject iconPrefab;

    [Header("Heart Appearance")]
    [Tooltip("Optional override sprite for all hearts. If empty, prefab's Image.sprite will be used.")]
    public Sprite heartSpriteOverride;
    [Tooltip("Optional override sprite for HP hearts. If empty, falls back to global override/prefab sprite.")]
    public Sprite hpHeartSpriteOverride;
    [Tooltip("Optional override sprite for Mana hearts. If empty, falls back to global override/prefab sprite.")]
    public Sprite manaHeartSpriteOverride;
    [Tooltip("Optional override sprite for Stamina hearts. If empty, falls back to global override/prefab sprite.")]
    public Sprite staminaHeartSpriteOverride;

    [Tooltip("Tint color for HP hearts (red)")]
    public Color hpColor = new Color(0.85f, 0.12f, 0.12f, 1f);

    [Tooltip("Tint color for Mana hearts (blue)")]
    public Color manaColor = new Color(0.18f, 0.55f, 1f, 1f);

    [Tooltip("Tint color for Stamina hearts (yellow)")]
    public Color staminaColor = new Color(0.95f, 0.78f, 0.16f, 1f);
    [Tooltip("Optional: name of the Image GameObject inside the prefab to tint (exact match). If empty, script picks first Image with a sprite.")]
    public string heartImageName = "";
    [Tooltip("Enable debug logs for container arrangement")]
    public bool debugArrange = false;

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

    [Tooltip("Доп. сдвиг по X (px).")]
    public float horizontalOffsetPx = 0f;

    [Tooltip("Вертикальный отступ между группами контейнеров (HP->Mana->Stamina)")]
    public float containerSpacingPx = 6f;

    [Tooltip("Если true — каждый кадр дожимает LayoutGroup настройки.")]
    public bool enforceLayoutEveryFrame = true;

    [Header("Force icon size (recommended)")]
    public bool forceIconSize = true;
    public float forceIconWidth = 0f;   // 0 = взять из sprite
    public float forceIconHeight = 0f;  // 0 = взять из sprite

    [Header("Breathing (all hearts)")]
    [Range(0f, 0.25f)] public float breatheAmplitude = 0.06f;
    [Range(0.1f, 6f)] public float breatheSpeed = 1.2f;
    public bool breatheUseUnscaledTime = true;

    [Header("New heart POP")]
    [Range(1f, 10f)] public float newHeartPopScale = 7f;
    [Range(0.01f, 0.5f)] public float popDownDuration = 0.12f;
    public bool popUseUnscaledTime = true;

    // --- Enums for perk types ---
    private enum PerkType { HP, Mana, Stamina }

    // --- Perk data ---
    private class PerkData
    {
        public PerkType type;
        public RectTransform content;
        public VerticalLayoutGroup vlg;
        public List<GameObject> pool = new();
        public Dictionary<GameObject, Coroutine> popRoutines = new();
        public int lastTotalAmount = -1;
        public Color color;
        public Sprite spriteOverride;
    }

    private PerkData[] perks;
    private float breatheT;

    private void Awake()
    {
        InitPerksArray();
        ApplyAllLayouts();
        ApplyAllOffsets();
    }

    private void OnEnable()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged += Refresh;

        InitPerksArray();
        ApplyAllLayouts();
        ApplyAllOffsets();
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
            ApplyAllLayouts();
            ApplyAllOffsets();
        }

        TickBreathing();
    }

    // ============ INIT ============

    private void InitPerksArray()
    {
        if (perks != null) return;

        perks = new PerkData[3];

        perks[0] = new PerkData
        {
            type = PerkType.HP,
            content = hpContent ?? GetComponent<RectTransform>(),
            color = hpColor,
            spriteOverride = hpHeartSpriteOverride
        };

        perks[1] = new PerkData
        {
            type = PerkType.Mana,
            content = manaContent,
            color = manaColor,
            spriteOverride = manaHeartSpriteOverride
        };

        perks[2] = new PerkData
        {
            type = PerkType.Stamina,
            content = staminaContent,
            color = staminaColor,
            spriteOverride = staminaHeartSpriteOverride
        };

        foreach (var perk in perks)
        {
            if (perk.content != null && perk.vlg == null)
                perk.vlg = perk.content.GetComponent<VerticalLayoutGroup>();
        }
    }

    // ============ LAYOUT ============

    private void ApplyAllLayouts()
    {
        foreach (var perk in perks)
            ApplyLayoutToPane(perk);
    }

    private void ApplyLayoutToPane(PerkData perk)
    {
        if (perk.content == null) return;
        if (perk.vlg == null && perk.content != null)
            perk.vlg = perk.content.GetComponent<VerticalLayoutGroup>();

        if (perk.vlg == null) return;

        perk.vlg.childAlignment = TextAnchor.UpperCenter;
        perk.vlg.childControlWidth = true;
        perk.vlg.childControlHeight = true;
        perk.vlg.childForceExpandWidth = false;
        perk.vlg.childForceExpandHeight = false;

        perk.vlg.spacing = spacingPx;

        var p = perk.vlg.padding;
        p.left = leftPaddingPx;
        p.right = rightPaddingPx;
        p.top = innerTopPaddingPx;
        p.bottom = bottomPaddingPx;
        perk.vlg.padding = p;
    }

    private void ApplyAllOffsets()
    {
        foreach (var perk in perks)
            ApplyOffsetToPane(perk);
    }

    private void ApplyOffsetToPane(PerkData perk)
    {
        if (perk.content == null) return;

        var pos = perk.content.anchoredPosition;
        pos.x = horizontalOffsetPx;
        perk.content.anchoredPosition = pos;
    }

    // ============ REFRESH ============

    public void Refresh()
    {
        InitPerksArray();
        ApplyAllLayouts();
        ApplyAllOffsets();

        RefreshPane(perks[0], GetHpLevelSafe());
        RefreshPane(perks[1], GetManaLevelSafe());
        RefreshPane(perks[2], GetStaminaLevelSafe());

        // After refreshing panes, ensure containers are stacked properly
        ArrangeContainerStack();
    }

    private void RefreshPane(PerkData perk, int totalAmount)
    {
        if (perk.content == null) return;

        if (perk.lastTotalAmount < 0)
            perk.lastTotalAmount = totalAmount;

        EnsurePoolSize(perk, totalAmount);

        for (int i = 0; i < totalAmount; i++)
        {
            var go = perk.pool[i];
            if (go == null) continue;

            bool wasInactive = !go.activeSelf;
            go.SetActive(true);

            // Find the most likely Image to tint: prefer Image components that have a sprite
            Image img = null;
            var imgs = go.GetComponentsInChildren<Image>(true);
            if (imgs != null && imgs.Length > 0)
            {
                // prefer Image that already has a sprite
                foreach (var ii in imgs)
                {
                    if (ii.sprite != null)
                    {
                        img = ii;
                        break;
                    }
                }

                // fallback to first Image
                if (img == null) img = imgs[0];
            }

            // If a specific heartImageName is provided, try to find that child Image first
            if (!string.IsNullOrEmpty(heartImageName))
            {
                var named = go.transform.Find(heartImageName);
                if (named != null)
                {
                    var namedImg = named.GetComponent<Image>();
                    if (namedImg != null) img = namedImg;
                }
            }

            if (img != null)
            {
                // Prefer per-type sprite override. Fallback to global override for backward compatibility.
                Sprite spriteToUse = perk.spriteOverride != null ? perk.spriteOverride : heartSpriteOverride;
                if (spriteToUse != null)
                    img.sprite = spriteToUse;

                img.preserveAspect = true;
                img.enabled = true;

                // Apply tint color to the heart image
                img.color = perk.color;
            }

            go.transform.localScale = Vector3.one;

            if (forceIconSize)
                EnsureLayoutElementSize(go, img);

            bool isNew = (totalAmount > perk.lastTotalAmount) && (i >= perk.lastTotalAmount);
            if (isNew)
                Pop(perk, go);
        }

        for (int i = totalAmount; i < perk.pool.Count; i++)
        {
            if (perk.pool[i] != null) perk.pool[i].SetActive(false);
        }

        perk.lastTotalAmount = totalAmount;
    }

    // Arrange containers vertically so Mana is below HP and Stamina below Mana
    private void ArrangeContainerStack()
    {
        if (perks == null || perks.Length == 0) return;

        // Rebuild layouts so sizes are up-to-date and set container heights to their preferred heights
        foreach (var p in perks)
        {
            if (p.content == null) continue;
            LayoutRebuilder.ForceRebuildLayoutImmediate(p.content);
            // set RectTransform height to preferred height so stacking is predictable
            float preferH = LayoutUtility.GetPreferredHeight(p.content);
            if (!float.IsNaN(preferH) && preferH > 0f)
                p.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferH);
        }

        // Apply container bottom padding according to containerSpacingPx so groups separate visually
        int pad = Mathf.RoundToInt(containerSpacingPx);
        if (pad > 0)
        {
            // HP container bottom padding
            if (perks[0].vlg != null)
            {
                var pp = perks[0].vlg.padding;
                pp.bottom = pad;
                perks[0].vlg.padding = pp;
            }

            // Mana container bottom padding
            if (perks[1].vlg != null)
            {
                var pp = perks[1].vlg.padding;
                pp.bottom = pad;
                perks[1].vlg.padding = pp;
            }
        }

        // Start from HP container anchoredPosition.y as top baseline
        float spacingBetweenGroups = (containerSpacingPx > 0f) ? containerSpacingPx : spacingPx; // use inspector spacing between containers (fallback to spacingPx)

        // Use anchoredPosition.y values; assume anchors/pivots are top (Y=1) for predictable stacking
        var hp = perks[0];
        if (hp.content == null) return;

        float hpHeight = hp.content.rect.height;
        Vector2 hpPos = hp.content.anchoredPosition;

        // Mana below HP
        var mana = perks[1];
        if (mana.content != null)
        {
            float manaY = hpPos.y - (hpHeight + spacingBetweenGroups);
            var pos = mana.content.anchoredPosition;
            pos.y = manaY;
            mana.content.anchoredPosition = pos;
            // ensure layout is rebuilt for mana and set its height
            LayoutRebuilder.ForceRebuildLayoutImmediate(mana.content);
            float newManaH = LayoutUtility.GetPreferredHeight(mana.content);
            if (!float.IsNaN(newManaH) && newManaH > 0f)
                mana.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newManaH);
        }

        // compute mana height after rebuild
        float manaHeight = mana.content != null ? mana.content.rect.height : 0f;

        // Stamina below Mana
        var sta = perks[2];
        if (sta.content != null)
        {
            float manaPosY = mana.content != null ? mana.content.anchoredPosition.y : hpPos.y;
            float staY = manaPosY - (manaHeight + spacingBetweenGroups);
            var pos = sta.content.anchoredPosition;
            pos.y = staY;
            sta.content.anchoredPosition = pos;
        }
    }

    private int GetHpLevelSafe()
    {
        var perks = SoulPerksManager.Instance;
        return perks != null ? (1 + perks.HpLevel) : 1;
    }

    private int GetManaLevelSafe()
    {
        var perks = SoulPerksManager.Instance;
        // Show one base Mana heart plus purchased levels (1 + ManaLevel)
        return perks != null ? (1 + perks.ManaLevel) : 1;
    }

    private int GetStaminaLevelSafe()
    {
        var perks = SoulPerksManager.Instance;
        // Show one base Stamina heart plus purchased levels (1 + StaminaLevel)
        return perks != null ? (1 + perks.StaminaLevel) : 1;
    }

    // ============ POOL ============

    private void EnsurePoolSize(PerkData perk, int need)
    {
        if (iconPrefab == null || perk.content == null) return;

        while (perk.pool.Count < need)
        {
            var go = Instantiate(iconPrefab, perk.content);
            go.SetActive(false);
            go.transform.localScale = Vector3.one;

            perk.pool.Add(go);
        }
    }

    private void EnsureLayoutElementSize(GameObject go, Image img)
    {
        if (go == null) return;

        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();

        float w = forceIconWidth;
        float h = forceIconHeight;

        if ((w <= 0f || h <= 0f) && img != null && img.sprite != null)
        {
            var r = img.sprite.rect;
            if (w <= 0f) w = r.width;
            if (h <= 0f) h = r.height;
        }

        if (w > 0f) le.preferredWidth = w;
        if (h > 0f) le.preferredHeight = h;

        if (w > 0f) le.minWidth = w;
        if (h > 0f) le.minHeight = h;
    }

    // ============ ANIMATIONS ============

    private void TickBreathing()
    {
        float dt = breatheUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        breatheT += dt * breatheSpeed;

        float s = 1f + Mathf.Sin(breatheT * Mathf.PI * 2f) * breatheAmplitude;

        foreach (var perk in perks)
        {
            foreach (var go in perk.pool)
            {
                if (go == null || !go.activeSelf) continue;
                if (perk.popRoutines.ContainsKey(go)) continue;

                go.transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }

    private void Pop(PerkData perk, GameObject go)
    {
        if (go == null) return;

        if (perk.popRoutines.TryGetValue(go, out var c) && c != null)
            StopCoroutine(c);

        perk.popRoutines[go] = StartCoroutine(PopRoutine(perk, go));
    }

    private IEnumerator PopRoutine(PerkData perk, GameObject go)
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
            k = Mathf.Pow(k, 0.35f);

            t.localScale = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }

        if (t != null) t.localScale = Vector3.one;
        perk.popRoutines.Remove(go);
    }
}
