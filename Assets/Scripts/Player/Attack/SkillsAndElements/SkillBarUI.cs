// SkillBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillBarUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image icon;
        public Image cooldown;
        public TMP_Text countText;
        [Range(0f, 1f)] public float startAlpha = 0.65f;
        [Range(0f, 1f)] public float endAlpha = 0f;
        public Image.Origin360 origin = Image.Origin360.Top;
    }

    public SkillLoadout loadout;
    public SlotUI[] slotsUI = new SlotUI[SkillLoadout.SlotsCount];
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1, 1, 1, 0.5f);

    void Awake()
    {
        SetupCooldownImages();
    }

    void Update()
    {
        if (loadout == null || loadout.slots == null) return;

        for (int i = 0; i < slotsUI.Length && i < loadout.slots.Length; i++)
        {
            var sUI = slotsUI[i];
            var s = loadout.slots[i];
            bool isActive = (i == loadout.ActiveIndex);

            // icon
            if (sUI.icon)
            {
                sUI.icon.sprite = s?.def ? s.def.icon : null;
                sUI.icon.color = isActive ? activeColor : inactiveColor;
            }

            // charges text
            if (sUI.countText)
            {
                if (s?.def == null) sUI.countText.text = "";
                else if (s.def.infiniteCharges) sUI.countText.text = "∞";
                else sUI.countText.text = s.charges.ToString();
            }

            // cooldown
            if (sUI.cooldown)
            {
                if (s == null || s.def == null || !s.IsOnCooldown)
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
}
