using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSkillPerksPanelUI : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private bool includeFireball;
    [SerializeField] private string levelTextChildName = "LevelText";
    [SerializeField] private Color iconTint = Color.white;

    [Header("Layout")]
    [SerializeField] private bool normalizeRuntimeIconRect = true;
    [SerializeField] private Vector2 iconSize = new Vector2(20f, 20f);
    [SerializeField] private int contentSidePadding = 2;
    [SerializeField] private int firstIconTopOffset = 15;
    [SerializeField] private float slotHorizontalPadding = 1f;

    [Header("Tooltip")]
    [SerializeField] private float tooltipDelay = 0.14f;

    [Header("New Icon Animation")]
    [SerializeField] private float revealDuration = 0.12f;
    [SerializeField] private float revealScaleMultiplier = 2.3f;
    [SerializeField] private Color revealFlashTint = new Color(1f, 0.95f, 0.45f, 1f);

    [Header("Breathing (all visible icons)")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breatheAmplitude = 0.06f;
    [SerializeField] private float breatheSpeed = 1.2f;
    [SerializeField] private bool breatheUseUnscaledTime = true;

    private readonly List<GameObject> _icons = new List<GameObject>();
    private readonly List<SkillId> _tracked = new List<SkillId>();
    private readonly HashSet<SkillId> _visibleSkills = new HashSet<SkillId>();
    private readonly Dictionary<GameObject, Coroutine> _popRoutines = new Dictionary<GameObject, Coroutine>();
    private bool _skillsSubscribed;
    private float _breatheTime;
    private int _lastVisibleCount;

    private void Awake()
    {
        if (content == null)
            content = transform as RectTransform;

        ApplyContentPaddingIfPossible();
    }

    private void OnEnable()
    {
        ApplyContentPaddingIfPossible();
        RegisterUiZonesForCursor();
        TrySubscribeSkills();
        Refresh();
    }

    private void Update()
    {
        // PlayerSkills can be spawned after this UI object.
        if (!_skillsSubscribed)
            TrySubscribeSkills();

        TickBreathing();
    }

    private void OnDisable()
    {
        if (_skillsSubscribed && PlayerSkills.Instance != null)
            PlayerSkills.Instance.OnSkillsChanged -= Refresh;
        _skillsSubscribed = false;

        UnregisterUiZonesForCursor();
    }

    public void Refresh()
    {
        RebuildTrackedSkills();
        CleanupVisibleSkills();
        EnsurePoolSize(_tracked.Count);

        int visibleCount = 0;

        for (int i = 0; i < _icons.Count; i++)
        {
            bool visible = i < _tracked.Count;
            _icons[i].SetActive(visible);
            if (!visible) continue;
            visibleCount++;

            SkillId skillId = _tracked[i];
            SetupIcon(_icons[i], skillId);

            bool isNewSkill = _visibleSkills.Add(skillId);
            bool isNewByCount = i >= _lastVisibleCount;
            if (isNewSkill || isNewByCount)
                PlayRevealAnimation(_icons[i]);
        }

        _lastVisibleCount = visibleCount;
    }

    private void RebuildTrackedSkills()
    {
        _tracked.Clear();

        if (PlayerSkills.Instance == null)
            return;

        AddIfUnlocked(SkillId.IceShard);
        AddIfUnlocked(SkillId.Lightning);

        if (includeFireball)
            AddIfUnlocked(SkillId.Fireball);
    }

    private void AddIfUnlocked(SkillId id)
    {
        if (!PlayerSkills.Instance.IsSkillUnlocked(id)) return;
        if (PlayerSkills.Instance.GetSkillLevel(id) <= 0) return;
        _tracked.Add(id);
    }

    private void EnsurePoolSize(int need)
    {
        if (iconPrefab == null || content == null) return;

        while (_icons.Count < need)
        {
            GameObject go = Instantiate(iconPrefab, content);
            NormalizeRuntimeRect(go, null);
            go.SetActive(false);
            _icons.Add(go);
        }
    }

    private void SetupIcon(GameObject iconGO, SkillId skillId)
    {
        if (iconGO == null) return;

        SkillDefinition def = SkillDefinitionLookup.FindById(skillId);
        int level = PlayerSkills.Instance != null ? PlayerSkills.Instance.GetSkillLevel(skillId) : 0;
        int charges = PlayerSkills.Instance != null ? PlayerSkills.Instance.GetCharges(skillId) : 0;

        Image image = iconGO.GetComponentInChildren<Image>(true);
        if (image != null)
        {
            if (def != null)
                image.sprite = def.icon;
            image.color = iconTint;
            image.raycastTarget = true;
            image.preserveAspect = true;
        }

        NormalizeRuntimeRect(iconGO, image);

        TMP_Text levelText = null;
        if (!string.IsNullOrWhiteSpace(levelTextChildName))
        {
            Transform child = iconGO.transform.Find(levelTextChildName);
            if (child != null)
                levelText = child.GetComponent<TMP_Text>();
        }
        if (levelText == null)
            levelText = iconGO.GetComponentInChildren<TMP_Text>(true);

        if (levelText != null)
            levelText.text = "Lv." + level;

        BindTooltip(iconGO, def, skillId, level, charges, tooltipDelay);
        if (image != null && image.gameObject != iconGO)
            BindTooltip(image.gameObject, def, skillId, level, charges, tooltipDelay);
    }

    private void NormalizeRuntimeRect(GameObject iconGO, Image iconImage)
    {
        if (!normalizeRuntimeIconRect || iconGO == null)
            return;

        RectTransform rootRect = iconGO.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.localScale = Vector3.one;
            rootRect.localRotation = Quaternion.identity;
            rootRect.anchoredPosition3D = Vector3.zero;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = iconSize;
        }

        LayoutElement layoutElement = iconGO.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = iconGO.AddComponent<LayoutElement>();

        layoutElement.minWidth = iconSize.x;
        layoutElement.minHeight = iconSize.y;
        layoutElement.preferredWidth = iconSize.x + Mathf.Max(0f, slotHorizontalPadding) * 2f;
        layoutElement.preferredHeight = iconSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        Graphic rootGraphic = iconGO.GetComponent<Graphic>();
        if (rootGraphic != null)
            rootGraphic.raycastTarget = true;

        if (iconImage != null)
            iconImage.raycastTarget = true;
    }

    private void ApplyContentPaddingIfPossible()
    {
        if (content == null)
            return;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            return;

        RectOffset pad = layout.padding ?? new RectOffset();
        int side = Mathf.Max(0, contentSidePadding);
        pad.left = Mathf.Max(pad.left, side);
        pad.right = Mathf.Max(pad.right, side);
        pad.top = Mathf.Max(pad.top, Mathf.Max(0, firstIconTopOffset));
        layout.padding = pad;
    }

    private void BindTooltip(GameObject target, SkillDefinition def, SkillId skillId, int level, int charges, float delay)
    {
        if (target == null)
            return;

        HoverTooltipTrigger tooltip = target.GetComponent<HoverTooltipTrigger>();
        if (tooltip == null)
            tooltip = target.AddComponent<HoverTooltipTrigger>();

        tooltip.Bind(() => BuildTooltip(def, skillId, level, charges), delay);
    }

    private void RegisterUiZonesForCursor()
    {
        if (CursorManager.Instance == null)
            return;

        RectTransform ownRect = transform as RectTransform;
        if (ownRect != null)
            CursorManager.Instance.RegisterForcedUiZone(ownRect);

        if (content != null)
            CursorManager.Instance.RegisterForcedUiZone(content);
    }

    private void UnregisterUiZonesForCursor()
    {
        if (CursorManager.Instance == null)
            return;

        RectTransform ownRect = transform as RectTransform;
        if (ownRect != null)
            CursorManager.Instance.UnregisterForcedUiZone(ownRect);

        if (content != null)
            CursorManager.Instance.UnregisterForcedUiZone(content);
    }

    private HoverTooltipData BuildTooltip(SkillDefinition def, SkillId skillId, int level, int charges)
    {
        string title = def != null && !string.IsNullOrWhiteSpace(def.displayName)
            ? def.displayName
            : skillId.ToString();

        string priceLine = def != null
            ? (TooltipLocalization.Tr("Charge price: ", "Charge price: ") + def.coinCostPerCharge + TooltipLocalization.Tr(" coins", " coins"))
            : "";

        string desc = (def != null && def.infiniteCharges)
            ? TooltipLocalization.Tr("Charges: infinite", "Charges: infinite")
            : (TooltipLocalization.Tr("Charges: ", "Charges: ") + Mathf.Max(0, charges));

        return new HoverTooltipData
        {
            title = title,
            levelLine = TooltipLocalization.Tr("Skill level: ", "Skill level: ") + Mathf.Max(0, level),
            priceLine = priceLine,
            description = desc
        };
    }

    private void TrySubscribeSkills()
    {
        if (_skillsSubscribed) return;
        if (PlayerSkills.Instance == null) return;

        PlayerSkills.Instance.OnSkillsChanged += Refresh;
        _skillsSubscribed = true;
    }

    private void CleanupVisibleSkills()
    {
        if (_visibleSkills.Count == 0) return;
        _visibleSkills.RemoveWhere(skillId => !_tracked.Contains(skillId));
    }

    private void PlayRevealAnimation(GameObject iconGO)
    {
        if (iconGO == null) return;

        if (_popRoutines.TryGetValue(iconGO, out var running) && running != null)
            StopCoroutine(running);

        _popRoutines[iconGO] = StartCoroutine(RevealRoutine(iconGO));
    }

    private IEnumerator RevealRoutine(GameObject iconGO)
    {
        if (iconGO == null) yield break;

        float duration = Mathf.Max(0.05f, revealDuration);
        float t = 0f;

        Transform tr = iconGO.transform;
        Vector3 endScale = tr.localScale;
        Vector3 startScale = endScale * Mathf.Max(1.02f, revealScaleMultiplier);
        tr.localScale = startScale;

        Image image = iconGO.GetComponentInChildren<Image>(true);
        Color startColor = revealFlashTint;
        Color endColor = iconTint;
        if (image != null)
            image.color = startColor;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            tr.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            if (image != null)
                image.color = Color.Lerp(startColor, endColor, eased);
            yield return null;
        }

        tr.localScale = endScale;
        if (image != null)
            image.color = endColor;

        _popRoutines.Remove(iconGO);
    }

    private void TickBreathing()
    {
        if (!enableBreathing)
            return;

        float dt = breatheUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _breatheTime += dt * Mathf.Max(0.1f, breatheSpeed);

        float amp = Mathf.Clamp(breatheAmplitude, 0f, 0.25f);
        float s = 1f + Mathf.Sin(_breatheTime * Mathf.PI * 2f) * amp;
        Vector3 scale = new Vector3(s, s, 1f);

        for (int i = 0; i < _icons.Count; i++)
        {
            var go = _icons[i];
            if (go == null || !go.activeSelf)
                continue;
            if (_popRoutines.ContainsKey(go))
                continue;

            go.transform.localScale = scale;
        }
    }
}

