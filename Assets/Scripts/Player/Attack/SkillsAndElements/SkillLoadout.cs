// SkillLoadout.cs
using UnityEngine;
using System;

[System.Serializable]
public class SkillSlotRuntime
{
    public SkillDefinition def;
    [Min(0)] public int charges;                 // текущее кол-во зарядов
    [HideInInspector] public float cooldownUntil; // Time.time, когда снова можно юзать

    public bool IsOnCooldown => Time.time < cooldownUntil;

    public float CooldownNormalized
    {
        get
        {
            if (def == null || def.cooldown <= 0f) return 0f;
            float remain = cooldownUntil - Time.time;
            return Mathf.Clamp01(remain / def.cooldown);
        }
    }

    public bool HasCharges => def != null && (def.infiniteCharges || charges > 0);
}

public class SkillLoadout : MonoBehaviour
{
    public const int SlotsCount = 5;

    [Header("Slots (fill in Inspector)")]
    public SkillSlotRuntime[] slots = new SkillSlotRuntime[SlotsCount];

    [Header("Start")]
    public int startIndex = 0;

    public int ActiveIndex { get; private set; }

    // Событие: старт кулдауна (для UI точек)
    public event Action<int, float> OnCooldownStarted;

    void Awake()
    {
        // гарантируем массив и стартовые данные
        for (int i = 0; i < SlotsCount; i++)
            if (slots[i] == null) slots[i] = new SkillSlotRuntime();

        for (int i = 0; i < SlotsCount; i++)
        {
            var s = slots[i];
            if (s.def == null) continue;
            if (!s.def.infiniteCharges && s.charges == 0)
                s.charges = Mathf.Max(0, s.def.startCharges);
        }

        ActiveIndex = Mathf.Clamp(startIndex, 0, SlotsCount - 1);
        AutoSkipIfEmptyForward(); // если стартовый пуст — найдём ближайший доступный
    }

    public SkillSlotRuntime Active =>
        slots != null && ActiveIndex >= 0 && ActiveIndex < slots.Length ? slots[ActiveIndex] : null;

    // ==== ОБСЛУЖИВАНИЕ ВЫСТРЕЛА / КД ====

    public bool TrySpendOneCharge()
    {
        var s = Active;
        if (s == null || s.def == null) return false;
        if (s.def.infiniteCharges) return true;
        if (s.charges <= 0) return false;
        s.charges -= 1;
        return true;
    }

    public void StartCooldownNow()
    {
        var s = Active;
        if (s == null || s.def == null) return;

        float duration = Mathf.Max(0f, s.def.cooldown);
        s.cooldownUntil = Time.time + duration;

        OnCooldownStarted?.Invoke(ActiveIndex, duration); // оповестим UI
    }

    public bool IsActiveReadyToUse()
    {
        var s = Active;
        if (s == null || s.def == null) return false;
        if (s.IsOnCooldown) return false;
        if (!s.def.infiniteCharges && s.charges <= 0) return false;
        return true;
    }

    // ==== ПЕРЕКЛЮЧЕНИЕ СЛОТОВ (ДОБАВЛЕНО) ====

    /// <summary>Выбрать следующий слот. Если onlyAvailable=true — пропускает пустые/на КД.</summary>
    public int SelectNext(bool onlyAvailable = false)
    {
        if (slots == null || slots.Length == 0) return ActiveIndex;

        for (int step = 1; step <= SlotsCount; step++)
        {
            int idx = (ActiveIndex + step) % SlotsCount;
            if (!onlyAvailable) { ActiveIndex = idx; break; }

            var s = slots[idx];
            if (s?.def != null && s.HasCharges && !s.IsOnCooldown)
            {
                ActiveIndex = idx;
                break;
            }
        }
        return ActiveIndex;
    }

    /// <summary>Выбрать предыдущий слот. Если onlyAvailable=true — пропускает пустые/на КД.</summary>
    public int SelectPrev(bool onlyAvailable = false)
    {
        if (slots == null || slots.Length == 0) return ActiveIndex;

        for (int step = 1; step <= SlotsCount; step++)
        {
            int idx = (ActiveIndex - step + SlotsCount) % SlotsCount;
            if (!onlyAvailable) { ActiveIndex = idx; break; }

            var s = slots[idx];
            if (s?.def != null && s.HasCharges && !s.IsOnCooldown)
            {
                ActiveIndex = idx;
                break;
            }
        }
        return ActiveIndex;
    }

    /// <summary>Выбрать конкретный индекс 0..4 (например по цифрам 1..5).</summary>
    public void SelectIndex(int index, bool onlyAvailable = false)
    {
        if (slots == null || slots.Length == 0) return;
        index = Mathf.Clamp(index, 0, SlotsCount - 1);

        if (!onlyAvailable)
        {
            ActiveIndex = index;
            return;
        }

        var s = slots[index];
        if (s?.def != null && s.HasCharges && !s.IsOnCooldown)
            ActiveIndex = index;
        // если недоступен — ничего не делаем
    }

    // ==== АВТО-ПЕРЕКЛЮЧЕНИЕ ПОСЛЕ ВЫСТРЕЛА / СТАРТА ====

    public bool AutoSkipIfEmptyForward()
    {
        for (int i = 0; i < SlotsCount; i++)
        {
            int idx = (ActiveIndex + i) % SlotsCount;
            var s = slots[idx];
            if (s?.def != null && s.HasCharges)
            {
                ActiveIndex = idx;
                return true;
            }
        }
        return false;
    }

    public bool SwitchToNextAvailable()
    {
        for (int step = 1; step < SlotsCount; step++)
        {
            int idx = (ActiveIndex + step) % SlotsCount;
            var s = slots[idx];
            if (s?.def != null && s.HasCharges)
            {
                ActiveIndex = idx;
                return true;
            }
        }
        return false;
    }

    // ==== Покупка зарядов (как было) ====

    public bool BuyCharges(int slotIndex, int count)
    {
        if (slotIndex < 0 || slotIndex >= SlotsCount) return false;
        var s = slots[slotIndex];
        if (s?.def == null || s.def.infiniteCharges) return false;

        int price = count * Mathf.Max(0, s.def.coinCostPerCharge);
        if (!PlayerWallet.Instance || !PlayerWallet.Instance.CanSpend(price)) return false;

        if (!PlayerWallet.Instance.TrySpend(price)) return false;
        s.charges += count;
        return true;
    }
}
