using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillBarUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Button button;
        public Image icon;
        public Image cooldown;
        public TMP_Text countText;
        public Image selectedFrame;

        [Header("Cooldown Visual")]
        [Range(0f, 1f)] public float startAlpha = 0.65f;
        [Range(0f, 1f)] public float endAlpha = 0f;
        public Image.Origin360 origin = Image.Origin360.Top;
    }

    public PlayerSkillShooter shooter;
    public SkillLoadout loadout;
    public SlotUI[] slotsUI = new SlotUI[SkillLoadout.SlotsCount];

    [Header("Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);

    private HoverTooltipTrigger[] _tooltipTriggers;

    private void Awake()
    {
        if (!shooter)
            shooter = FindObjectOfType<PlayerSkillShooter>();

        SetupCooldownImages();
        WireButtons();
        EnsureTooltips();
    }

    private void OnEnable()
    {
        if (loadout) loadout.EnsureValidActive();
    }

    private void Update()
    {
        if (loadout == null || loadout.slots == null) return;

        for (int i = 0; i < slotsUI.Length && i < loadout.slots.Length; i++)
        {
            var sUI = slotsUI[i];
            var s = loadout.slots[i];
            bool isUsable = (s?.def != null);
            bool isActive = (i == loadout.ActiveIndex);

            if (sUI.icon)
            {
                sUI.icon.sprite = isUsable ? s.def.icon : null;
                sUI.icon.color = (isUsable && isActive) ? activeColor :
                    (isUsable ? inactiveColor : new Color(1f, 1f, 1f, 0.2f));
            }

            if (sUI.selectedFrame)
                sUI.selectedFrame.enabled = isActive && isUsable;

            if (sUI.countText)
            {
                if (!isUsable) sUI.countText.text = "";
                else if (s.def.infiniteCharges) sUI.countText.text = "∞";
                else sUI.countText.text = s.charges.ToString();
            }

            if (sUI.cooldown)
            {
                if (!isUsable || !s.IsOnCooldown)
                {
                    sUI.cooldown.enabled = false;
                }
                else
                {
                    sUI.cooldown.enabled = true;
                    float t = s.CooldownNormalized;
                    sUI.cooldown.fillAmount = t;

                    var c = sUI.cooldown.color;
                    c.a = Mathf.Lerp(sUI.endAlpha, sUI.startAlpha, t);
                    sUI.cooldown.color = c;
                }
            }

            if (sUI.button) sUI.button.interactable = isUsable;
        }
    }

    private void SetupCooldownImages()
    {
        foreach (var sUI in slotsUI)
        {
            if (sUI == null || !sUI.cooldown) continue;
            var img = sUI.cooldown;
            img.raycastTarget = false;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillClockwise = true;
            img.fillOrigin = (int)sUI.origin;
            img.fillAmount = 0f;
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    private void WireButtons()
    {
        for (int i = 0; i < slotsUI.Length; i++)
        {
            int captured = i;
            var sUI = slotsUI[i];
            if (sUI != null && sUI.button != null)
            {
                sUI.button.onClick.RemoveAllListeners();
                sUI.button.onClick.AddListener(() => OnSlotClicked(captured));
            }
        }
    }

    private void EnsureTooltips()
    {
        if (slotsUI == null) return;

        if (_tooltipTriggers == null || _tooltipTriggers.Length != slotsUI.Length)
            _tooltipTriggers = new HoverTooltipTrigger[slotsUI.Length];

        for (int i = 0; i < slotsUI.Length; i++)
        {
            var sUI = slotsUI[i];
            if (sUI == null || sUI.button == null) continue;

            int captured = i;
            var trigger = sUI.button.GetComponent<HoverTooltipTrigger>();
            if (trigger == null)
                trigger = sUI.button.gameObject.AddComponent<HoverTooltipTrigger>();

            trigger.Bind(() => BuildSkillTooltipData(captured), 0.3f);
            _tooltipTriggers[i] = trigger;
        }
    }

    private HoverTooltipData BuildSkillTooltipData(int index)
    {
        if (loadout == null || loadout.slots == null || index < 0 || index >= loadout.slots.Length)
            return default;

        var slot = loadout.slots[index];
        if (slot == null || slot.def == null) return default;

        int level = 1;
        if (PlayerSkills.Instance != null)
            level = Mathf.Max(1, PlayerSkills.Instance.GetSkillLevel(slot.def.skillId));

        string chargesLine = slot.def.infiniteCharges
            ? TooltipLocalization.Tr("Charges: infinite", "Заряды: бесконечно")
            : (TooltipLocalization.Tr("Charges: ", "Заряды: ") + slot.charges);

        string extraLine = TooltipLocalization.Tr("CD: ", "КД: ")
            + slot.def.cooldown.ToString("0.##")
            + TooltipLocalization.Tr(" sec | Mana: ", " сек | Мана: ")
            + slot.def.manaCostPerShot;

        return new HoverTooltipData
        {
            title = slot.def.displayName,
            levelLine = TooltipLocalization.Tr("Skill level: ", "Уровень навыка: ") + level,
            priceLine = TooltipLocalization.Tr("Charge price: ", "Цена заряда: ")
                + slot.def.coinCostPerCharge
                + TooltipLocalization.Tr(" coins", " монеты"),
            description = chargesLine + "\n" + extraLine
        };
    }

    private void OnSlotClicked(int index)
    {
        if (!loadout) return;

        loadout.SetActiveIndex(index);

        if (shooter != null)
            shooter.SkipNextClickFromUI();
    }
}

