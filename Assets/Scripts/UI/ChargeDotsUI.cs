using UnityEngine;
using UnityEngine.UI;

public class ChargeDotsUI : MonoBehaviour
{
    [Header("Refs (drag from Canvas)")]
    public RectTransform Dot1;
    public RectTransform Dot2;
    public RectTransform Dot3;

    [Header("Optional")]
    public SkillLoadout loadout;

    [Header("Visuals")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.35f);
    public Color cooldownColor = new Color(0f, 0f, 0f, 0.65f);

    struct Dot
    {
        public Image icon;
        public Image cooldown;
        public float endTime;
        public float duration;
        public bool active;
    }

    Dot[] dots = new Dot[3];

    void Awake()
    {
        SetupDot(0, Dot1);
        SetupDot(1, Dot2);
        SetupDot(2, Dot3);
        ApplyCountVisuals(0);
    }

    void OnEnable()
    {
        if (!loadout) loadout = FindObjectOfType<SkillLoadout>();
        if (loadout) loadout.OnCooldownStarted += HandleCooldownStarted;
        ApplyCountVisuals(CurrentActiveCount);
    }

    void OnDisable()
    {
        if (loadout) loadout.OnCooldownStarted -= HandleCooldownStarted;
    }

    void Update()
    {
        for (int i = 0; i < dots.Length; i++)
        {
            var d = dots[i];
            if (!d.cooldown) continue;

            if (d.endTime > 0f && d.duration > 0f)
            {
                float remain = Mathf.Max(0f, d.endTime - Time.time);
                float fill = Mathf.Clamp01(remain / d.duration);

                if (!d.cooldown.enabled && fill > 0f) d.cooldown.enabled = true;

                d.cooldown.fillAmount = fill;

                if (fill <= 0f)
                {
                    d.endTime = 0f;
                    d.duration = 0f;
                    d.cooldown.enabled = false;
                }
                dots[i] = d;
            }
        }
    }

    // -------- Public API ----------
    public void SetCount(int count)
    {
        count = Mathf.Clamp(count, 0, 3);
        ApplyCountVisuals(count);
    }

    public void Clear()
    {
        ApplyCountVisuals(0);
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i].cooldown)
            {
                dots[i].cooldown.fillAmount = 0f;
                dots[i].cooldown.enabled = false;
            }
            dots[i].endTime = 0f;
            dots[i].duration = 0f;
        }
    }

    // -------- Events ----------
    void HandleCooldownStarted(int slotIndex, float duration)
    {
        if (slotIndex < 0 || slotIndex > 2) return;

        var d = dots[slotIndex];
        if (d.cooldown)
        {
            d.cooldown.enabled = true;
            d.cooldown.fillAmount = (duration <= 0f) ? 0f : 1f;
        }
        d.duration = Mathf.Max(0.0001f, duration);
        d.endTime = Time.time + d.duration;
        dots[slotIndex] = d;
    }

    // -------- Utils ----------
    void SetupDot(int i, RectTransform rt)
    {
        if (!rt) return;

        var icon = rt.GetComponent<Image>();
        Image cooldown = null;
        var child = rt.Find("Cooldown");
        if (child) cooldown = child.GetComponent<Image>();

        dots[i] = new Dot
        {
            icon = icon,
            cooldown = cooldown,
            endTime = 0f,
            duration = 0f,
            active = false
        };

        if (icon)
        {
            icon.enabled = true;
            icon.color = inactiveColor;
        }
        if (cooldown)
        {
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = true;
            cooldown.color = cooldownColor;
            cooldown.fillAmount = 0f;
            cooldown.enabled = false;
            cooldown.raycastTarget = false;
        }
    }

    void ApplyCountVisuals(int count)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            bool isActive = i < count;
            var d = dots[i];
            d.active = isActive;

            if (d.icon)
            {
                d.icon.enabled = true;
                d.icon.color = isActive ? activeColor : inactiveColor;
            }
            dots[i] = d;
        }
    }

    int CurrentActiveCount
    {
        get
        {
            int c = 0;
            for (int i = 0; i < dots.Length; i++) if (dots[i].active) c++;
            return c;
        }
    }
}
