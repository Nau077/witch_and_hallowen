using UnityEngine;

/// <summary>
/// Кошелёк для ДУШ (souls).
/// Использует SoulCounter.cursedGoldRun как источник правды.
/// Вешаем, например, на объект SoulScore.
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

    /// <summary>Текущее количество душ в ран-забеге.</summary>
    public int CurrentSouls
    {
        get
        {
            if (SoulCounter.Instance == null) return 0;
            return SoulCounter.Instance.cursedGoldRun;   // <-- ЭТО souls
        }
    }

    public bool CanSpend(int amount) => CurrentSouls >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanSpend(amount)) return false;

        if (SoulCounter.Instance != null)
        {
            SoulCounter.Instance.cursedGoldRun -= amount;
            SoulCounter.Instance.RefreshUI();   // обновляем текст душ
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
