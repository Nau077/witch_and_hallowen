using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DashPerkPanelUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform content;
    public GameObject iconPrefab;

    [Header("Dash Icons by Level")]
    public Sprite dashLevel1Sprite;
    public Sprite dashLevel2Sprite;
    public Sprite dashLevel3Sprite;

    [Header("Force icon size (recommended)")]
    public bool forceIconSize = true;
    public float forceIconWidth = 24f;
    public float forceIconHeight = 24f;

    [Header("Breathing")]
    [Range(0f, 0.5f)] public float breatheAmplitude = 0.10f;
    [Range(0.1f, 10f)] public float breatheSpeed = 1.2f;
    public bool breatheUseUnscaledTime = true;

    [Header("POP on level change")]
    [Range(1f, 10f)] public float popScale = 2.6f;
    [Range(0.01f, 0.5f)] public float popDownDuration = 0.12f;
    public bool popUseUnscaledTime = true;

    private GameObject iconGO;
    private Image iconImg;
    private RectTransform iconRect;

    private int lastLevel = -1;
    private float breatheT;
    private Coroutine popRoutine;
    private HoverTooltipTrigger tooltipTrigger;

    private void Awake()
    {
        if (content == null)
            content = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged += Refresh;

        EnsureIconExists();
        Refresh();
    }

    private void OnDisable()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged -= Refresh;
    }

    private void LateUpdate()
    {
        TickBreathing();
    }

    public void Refresh()
    {
        EnsureIconExists();
        if (iconImg == null || iconRect == null) return;

        int lvl = GetDashLevelSafe();
        iconImg.sprite = GetSpriteByLevel(lvl);
        iconImg.preserveAspect = true;

        // ВАЖНО: принудительно делаем видимым
        iconImg.enabled = (iconImg.sprite != null);
        iconImg.color = Color.white; // alpha = 1

        if (forceIconSize)
            ForceSize();

        if (lastLevel >= 0 && lvl != lastLevel)
            Pop();

        lastLevel = lvl;
    }

    private int GetDashLevelSafe()
    {
        var perks = SoulPerksManager.Instance;
        return perks != null ? perks.GetDashRealLevel() : 1;
    }

    private Sprite GetSpriteByLevel(int lvl)
    {
        return lvl switch
        {
            1 => dashLevel1Sprite,
            2 => dashLevel2Sprite,
            3 => dashLevel3Sprite,
            _ => dashLevel1Sprite
        };
    }

    private void EnsureIconExists()
    {
        if (iconGO != null && iconImg != null && iconRect != null) return;

        if (content == null)
        {
            Debug.LogWarning("DashPerkPanelUI: content is NULL");
            return;
        }

        if (iconPrefab == null)
        {
            Debug.LogWarning("DashPerkPanelUI: iconPrefab is NULL");
            return;
        }

        // Если уже есть ребёнок — используем его
        if (content.childCount > 0)
            iconGO = content.GetChild(0).gameObject;
        else
            iconGO = Instantiate(iconPrefab, content);

        iconGO.SetActive(true);

        iconImg = iconGO.GetComponent<Image>();
        if (iconImg == null)
            iconImg = iconGO.GetComponentInChildren<Image>(true);

        if (iconImg == null)
        {
            Debug.LogWarning("DashPerkPanelUI: iconPrefab has no Image (or in children).");
            return;
        }

        iconRect = iconImg.rectTransform;

        // Сброс в адекватное состояние
        iconRect.localScale = Vector3.one;
        iconImg.color = Color.white;

        tooltipTrigger = iconGO.GetComponent<HoverTooltipTrigger>();
        if (tooltipTrigger == null)
            tooltipTrigger = iconGO.AddComponent<HoverTooltipTrigger>();

        tooltipTrigger.Bind(BuildTooltipData, 0.6f);
    }

    private void ForceSize()
    {
        // 1) LayoutElement на контейнер (для LayoutGroup)
        var le = iconGO.GetComponent<LayoutElement>();
        if (le == null) le = iconGO.AddComponent<LayoutElement>();

        le.minWidth = forceIconWidth;
        le.minHeight = forceIconHeight;
        le.preferredWidth = forceIconWidth;
        le.preferredHeight = forceIconHeight;

        // 2) И прямой размер RectTransform у картинки (на случай, если Layout не работает)
        iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, forceIconWidth);
        iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, forceIconHeight);
    }

    private void TickBreathing()
    {
        if (iconRect == null) return;
        if (popRoutine != null) return;

        float dt = breatheUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        breatheT += dt * breatheSpeed;

        float s = 1f + Mathf.Sin(breatheT * Mathf.PI * 2f) * breatheAmplitude;

        // скейлим именно картинку, чтобы LayoutGroup не затирал
        iconRect.localScale = new Vector3(s, s, 1f);
    }

    private void Pop()
    {
        if (iconRect == null) return;

        if (popRoutine != null)
            StopCoroutine(popRoutine);

        popRoutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        if (iconRect == null) yield break;

        Vector3 start = Vector3.one * popScale;
        Vector3 end = Vector3.one;

        iconRect.localScale = start;

        float dur = Mathf.Max(0.001f, popDownDuration);
        float time = 0f;

        while (time < dur && iconRect != null)
        {
            float dt = popUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            time += dt;

            float k = Mathf.Clamp01(time / dur);
            k = Mathf.Pow(k, 0.35f);

            iconRect.localScale = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }

        if (iconRect != null)
            iconRect.localScale = Vector3.one;

        popRoutine = null;
    }

    private HoverTooltipData BuildTooltipData()
    {
        var perks = SoulPerksManager.Instance;
        if (perks == null) return default;

        return new HoverTooltipData
        {
            title = "Перк дэша",
            levelLine = "Уровень: " + perks.GetDashRealLevel() + "/3",
            priceLine = perks.DashLevel >= perks.dashMaxPurchases
                ? "Цена: MAX"
                : ("Цена: " + perks.GetDashUpgradePrice() + " души"),
            description = "Улучшает дальность и эффективность дэша."
        };
    }
}
