using UnityEngine;

/// <summary>
/// Кошелёк для SOULS (перманентной валюты).
/// Источник истины: SoulCounter.souls
/// </summary>
public class PlayerSoulsWallet : MonoBehaviour
{
    public static PlayerSoulsWallet Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int CurrentSouls
    {
        get
        {
            if (SoulCounter.Instance == null) return 0;
            return SoulCounter.Instance.Souls;
        }
    }

    public bool CanSpend(int amount) => CurrentSouls >= amount;

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (!CanSpend(amount)) return false;

        // уменьшаем перманентные souls и сохраняем
        SoulCounter.Instance.SetSouls(CurrentSouls - amount);
        SoulCounter.Instance.RefreshUI();

        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        if (SoulCounter.Instance == null) return;

        SoulCounter.Instance.AddSouls(amount);
        SoulCounter.Instance.RefreshUI();
    }
}
