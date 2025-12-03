using UnityEngine;
using System;

[Serializable]
public class SkillSlot
{
    public SkillDefinition def;
    public int charges;
    public float cooldownUntil;

    public bool HasCharges => def == null ? false : (def.infiniteCharges || charges > 0);
    public bool IsOnCooldown => def != null && Time.time < cooldownUntil;

    public float CooldownNormalized
    {
        get
        {
            if (def == null || def.cooldown <= 0f) return 0f;
            float left = Mathf.Max(0f, cooldownUntil - Time.time);
            return Mathf.Clamp01(left / def.cooldown);
        }
    }
}

public class SkillLoadout : MonoBehaviour
{
    public static SkillLoadout Instance { get; private set; }

    public const int SlotsCount = 5;

    public SkillSlot[] slots = new SkillSlot[SlotsCount];

    [SerializeField] private int activeIndex = 0;
    public int ActiveIndex => activeIndex;
    public SkillSlot Active =>
        (slots != null && activeIndex >= 0 && activeIndex < slots.Length)
            ? slots[activeIndex]
            : null;

    public event Action<int, float> OnCooldownStarted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (slots == null || slots.Length != SlotsCount)
            slots = new SkillSlot[SlotsCount];

        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) slots[i] = new SkillSlot();

        EnsureValidActive();
    }

    public void EnsureValidActive()
    {
        if (!IsSlotUsable(activeIndex))
        {
            int first = FindNextUsableFrom(-1, forward: true);
            activeIndex = first;
        }
    }

    bool IsSlotUsable(int idx)
    {
        if (slots == null || idx < 0 || idx >= slots.Length) return false;
        return slots[idx] != null && slots[idx].def != null;
    }

    int FindNextUsableFrom(int start, bool forward)
    {
        if (slots == null || slots.Length == 0) return -1;
        int n = slots.Length;
        int i = start;
        for (int step = 0; step < n; step++)
        {
            i = forward ? (i + 1 + n) % n : (i - 1 + n) % n;
            if (IsSlotUsable(i)) return i;
        }
        return -1;
    }

    public void SelectNext()
    {
        int next = FindNextUsableFrom(activeIndex, forward: true);
        activeIndex = (next != -1) ? next : -1;
    }

    public void SelectPrev()
    {
        int prev = FindNextUsableFrom(activeIndex, forward: false);
        activeIndex = (prev != -1) ? prev : -1;
    }

    public void SwitchToNextAvailable()
    {
        int n = slots?.Length ?? 0;
        if (n == 0) { activeIndex = -1; return; }

        for (int step = 0, i = activeIndex; step < n; step++)
        {
            i = (i + 1) % n;
            if (IsSlotUsable(i))
            {
                var s = slots[i];
                if (s.def.infiniteCharges || s.charges > 0)
                {
                    activeIndex = i;
                    return;
                }
            }
        }
        int any = FindNextUsableFrom(activeIndex, true);
        activeIndex = (any != -1) ? any : -1;
    }

    public bool IsActiveReadyToUse()
    {
        var s = Active;
        if (s == null || s.def == null) return false;
        if (s.IsOnCooldown) return false;
        if (!s.HasCharges) return false;
        return true;
    }

    public void TrySpendOneCharge()
    {
        var s = Active;
        if (s == null || s.def == null) return;
        if (!s.def.infiniteCharges && s.charges > 0)
            s.charges--;
    }

    public void StartCooldownNow()
    {
        StartCooldownFor(activeIndex);
    }

    public void StartCooldownFor(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;
        var s = slots[index];
        if (s == null || s.def == null) return;

        float cd = Mathf.Max(0f, s.def.cooldown);
        if (cd > 0f)
        {
            s.cooldownUntil = Time.time + cd;
            OnCooldownStarted?.Invoke(index, cd);
        }
    }

    public void SetActiveIndex(int index)
    {
        activeIndex = Mathf.Clamp(index, -1, (slots?.Length ?? 1) - 1);
        EnsureValidActive();
    }

    // ====== НОВОЕ: добавление зарядов в инвентарь ======

    public bool AddChargesToSkill(SkillDefinition def, int amount)
    {
        if (def == null || amount <= 0) return false;
        int n = slots?.Length ?? 0;
        if (n == 0) return false;

        // 1) скилл уже есть в одном из слотов
        for (int i = 0; i < n; i++)
        {
            var s = slots[i];
            if (s != null && s.def == def)
            {
                if (!s.def.infiniteCharges)
                    s.charges += amount;
                return true;
            }
        }

        // 2) ищем пустой слот
        for (int i = 0; i < n; i++)
        {
            if (slots[i] == null || slots[i].def == null)
            {
                if (slots[i] == null)
                    slots[i] = new SkillSlot();

                slots[i].def = def;
                slots[i].cooldownUntil = 0f;
                if (!def.infiniteCharges)
                    slots[i].charges = amount;
                else
                    slots[i].charges = 0;

                EnsureValidActive();
                return true;
            }
        }

        Debug.LogWarning("[SkillLoadout] Нет свободного слота для " + def.displayName);
        return false;
    }

    /// <summary>
    /// Очистить слот — скилл исчезает с панели.
    /// </summary>
    public void ClearSkillAtIndex(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;

        var s = slots[index];
        if (s != null)
        {
            s.def = null;
            s.charges = 0;
            s.cooldownUntil = 0f;
        }

        if (activeIndex == index)
            EnsureValidActive();
    }
}
