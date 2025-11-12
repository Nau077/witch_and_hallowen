using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillBarUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Button button;          // <- повесь сюда Button на корневой объект слота
        public Image icon;             // иконка навыка
        public Image cooldown;         // радиальная заливка
        public TMP_Text countText;     // "∞" или число
        public Image selectedFrame;    // рамка/подсветка активного

        [Header("Cooldown Visual")]
        [Range(0f, 1f)] public float startAlpha = 0.65f;
        [Range(0f, 1f)] public float endAlpha = 0f;
        public Image.Origin360 origin = Image.Origin360.Top;
    }

    public SkillLoadout loadout;
    public SlotUI[] slotsUI = new SlotUI[SkillLoadout.SlotsCount];

    [Header("Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1, 1, 1, 0.5f);

    void Awake()
    {
        SetupCooldownImages();
        WireButtons();
    }

    void OnEnable()
    {
        // на всякий — подтянуть валидный актив
        if (loadout) loadout.EnsureValidActive();
    }

    void Update()
    {
        if (loadout == null || loadout.slots == null) return;

        for (int i = 0; i < slotsUI.Length && i < loadout.slots.Length; i++)
        {
            var sUI = slotsUI[i];
            var s = loadout.slots[i];
            bool isUsable = (s?.def != null);
            bool isActive = (i == loadout.ActiveIndex);

            // icon
            if (sUI.icon)
            {
                sUI.icon.sprite = isUsable ? s.def.icon : null;
                sUI.icon.color = (isUsable && isActive) ? activeColor :
                                 (isUsable ? inactiveColor : new Color(1, 1, 1, 0.2f));
            }

            // selected frame
            if (sUI.selectedFrame)
                sUI.selectedFrame.enabled = isActive && isUsable;

            // text
            if (sUI.countText)
            {
                if (!isUsable) sUI.countText.text = "";
                else if (s.def.infiniteCharges) sUI.countText.text = "∞";
                else sUI.countText.text = s.charges.ToString();
            }

            // cooldown
            if (sUI.cooldown)
            {
                if (!isUsable || !s.IsOnCooldown)
                {
                    sUI.cooldown.enabled = false;
                }
                else
                {
                    sUI.cooldown.enabled = true;
                    float t = s.CooldownNormalized; // 1..0
                    sUI.cooldown.fillAmount = t;

                    var c = sUI.cooldown.color;
                    c.a = Mathf.Lerp(sUI.endAlpha, sUI.startAlpha, t);
                    sUI.cooldown.color = c;
                }
            }

            // кнопка должна быть кликабельной только если там есть навык
            if (sUI.button) sUI.button.interactable = isUsable;
        }
    }

    void SetupCooldownImages()
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
            var c = img.color; c.a = 0f; img.color = c;
        }
    }

    void WireButtons()
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

    void OnSlotClicked(int index)
    {
        if (!loadout) return;
        // если на слоте реально есть скилл — активируем его
        // (активный индекс поставим напрямую и проверим валидность)
        loadout.SetActiveIndex(index);
    }
}
