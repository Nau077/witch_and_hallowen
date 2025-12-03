using UnityEngine;

/// <summary>
/// Фасад для работы с душами как с "кошельком".
/// Использует SoulCounter.cursedGoldRun.
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
            return SoulCounter.Instance.cursedGoldRun;
        }
    }

    public bool CanSpend(int amount) => CurrentSouls >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanSpend(amount)) return false;

        if (SoulCounter.Instance != null)
        {
            SoulCounter.Instance.cursedGoldRun -= amount;
            SoulCounter.Instance.RefreshUI();
        }
        return true;
    }

    public void Add(int amount)
    {
        if (SoulCounter.Instance != null)
        {
            SoulCounter.Instance.cursedGoldRun += amount;
            SoulCounter.Instance.RefreshUI();
        }
    }
}
