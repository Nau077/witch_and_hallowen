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

    private readonly List<GameObject> _icons = new List<GameObject>();
    private readonly List<SkillId> _tracked = new List<SkillId>();

    private void Awake()
    {
        if (content == null)
            content = transform as RectTransform;
    }

    private void OnEnable()
    {
        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.OnSkillsChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.OnSkillsChanged -= Refresh;
    }

    public void Refresh()
    {
        RebuildTrackedSkills();
        EnsurePoolSize(_tracked.Count);

        for (int i = 0; i < _icons.Count; i++)
        {
            bool visible = i < _tracked.Count;
            _icons[i].SetActive(visible);
            if (!visible) continue;

            var skillId = _tracked[i];
            SetupIcon(_icons[i], skillId);
        }
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
            var go = Instantiate(iconPrefab, content);
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

        var image = iconGO.GetComponentInChildren<Image>(true);
        if (image != null)
        {
            if (def != null)
                image.sprite = def.icon;
            image.color = iconTint;
        }

        TMP_Text levelText = null;
        if (!string.IsNullOrWhiteSpace(levelTextChildName))
        {
            var child = iconGO.transform.Find(levelTextChildName);
            if (child != null)
                levelText = child.GetComponent<TMP_Text>();
        }
        if (levelText == null)
            levelText = iconGO.GetComponentInChildren<TMP_Text>(true);

        if (levelText != null)
            levelText.text = "Lv." + level;

        var tooltipTarget = iconGO.GetComponent<Button>() != null
            ? iconGO.GetComponent<Button>().gameObject
            : iconGO;

        var tooltip = tooltipTarget.GetComponent<HoverTooltipTrigger>();
        if (tooltip == null)
            tooltip = tooltipTarget.AddComponent<HoverTooltipTrigger>();

        tooltip.Bind(() => BuildTooltip(def, skillId, level, charges), 0.3f);
    }

    private HoverTooltipData BuildTooltip(SkillDefinition def, SkillId skillId, int level, int charges)
    {
        string title = def != null && !string.IsNullOrWhiteSpace(def.displayName)
            ? def.displayName
            : skillId.ToString();

        string priceLine = def != null
            ? (TooltipLocalization.Tr("Charge price: ", "Цена заряда: ") + def.coinCostPerCharge + TooltipLocalization.Tr(" coins", " монеты"))
            : "";

        string desc = (def != null && def.infiniteCharges)
            ? TooltipLocalization.Tr("Charges: infinite", "Заряды: бесконечно")
            : (TooltipLocalization.Tr("Charges: ", "Заряды: ") + Mathf.Max(0, charges));

        return new HoverTooltipData
        {
            title = title,
            levelLine = TooltipLocalization.Tr("Skill level: ", "Уровень навыка: ") + Mathf.Max(0, level),
            priceLine = priceLine,
            description = desc
        };
    }
}

