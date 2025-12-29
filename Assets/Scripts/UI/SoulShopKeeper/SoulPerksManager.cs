using UnityEngine;

/// <summary>
/// Хранит перманентные покупки за души (souls = SoulCounter.killsLifetime).
/// Сохраняет в PlayerPrefs.
/// </summary>
[DefaultExecutionOrder(-200)]
public class SoulPerksManager : MonoBehaviour
{
    public static SoulPerksManager Instance { get; private set; }

    // --- PlayerPrefs keys ---
    private const string KEY_HP_LEVEL = "perk_hp_level";          // 0..4
    private const string KEY_SOULS_SPENT = "perk_souls_spent";    // сколько душ потрачено на перки (для refund)

    [Header("Perk: Max HP")]
    public int hpStep = 50;
    public int hpMaxPurchases = 4;
    public int hpBasePrice = 50;

    public int HpLevel { get; private set; }       // 0..4
    public int SoulsSpent { get; private set; }    // суммарно потрачено на перки

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void Start()
    {
        // На старте применим к игроку, если есть
        ApplyToPlayerIfPossible();
    }

    public void Load()
    {
        HpLevel = Mathf.Clamp(PlayerPrefs.GetInt(KEY_HP_LEVEL, 0), 0, hpMaxPurchases);
        SoulsSpent = Mathf.Max(0, PlayerPrefs.GetInt(KEY_SOULS_SPENT, 0));
    }

    public void Save()
    {
        PlayerPrefs.SetInt(KEY_HP_LEVEL, HpLevel);
        PlayerPrefs.SetInt(KEY_SOULS_SPENT, SoulsSpent);
        PlayerPrefs.Save();
    }

    public int GetHealthUpgradePrice()
    {
        // 1-я покупка: 50, 2-я: 100, 3-я: 150, 4-я: 200
        int nextIndex = HpLevel; // 0..3
        return hpBasePrice * (nextIndex + 1);
    }

    public bool CanBuyHealthUpgrade()
    {
        if (HpLevel >= hpMaxPurchases) return false;
        var sc = SoulCounter.Instance;
        if (sc == null) return false;
        return sc.killsLifetime >= GetHealthUpgradePrice();
    }

    public bool HasAnythingToReset()
    {
        return HpLevel > 0 || SoulsSpent > 0;
    }

    /// <summary>
    /// Покупка +50 max hp (перманентно) за души, до 4 раз, цена растёт.
    /// </summary>
    public bool TryBuyHealthUpgrade()
    {
        if (HpLevel >= hpMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        int price = GetHealthUpgradePrice();
        if (sc.killsLifetime < price) return false;

        sc.killsLifetime -= price;
        sc.RefreshUI();

        SoulsSpent += price;
        HpLevel++;

        Save();
        ApplyToPlayerIfPossible();

        return true;
    }

    /// <summary>
    /// Сброс всех soul-perks + возврат потраченных душ.
    /// </summary>
    public bool ResetAllPerksWithRefund()
    {
        if (!HasAnythingToReset())
            return false;

        var sc = SoulCounter.Instance;
        if (sc != null)
        {
            sc.killsLifetime += SoulsSpent;
            sc.RefreshUI();
        }

        HpLevel = 0;
        SoulsSpent = 0;

        Save();
        ApplyToPlayerIfPossible();

        return true;
    }

    public int GetPermanentMaxHpBonus()
    {
        return HpLevel * hpStep;
    }

    /// <summary>
    /// Применяет бонус к здоровью игрока. Требует, чтобы PlayerHealth имел метод ApplyPermanentMaxHpBonus(int).
    /// </summary>
    public void ApplyToPlayerIfPossible()
    {
        var rlm = RunLevelManager.Instance;
        if (rlm == null || rlm.playerHealth == null) return;

        // ВАЖНО: этот метод мы добавим в PlayerHealth (ниже)
        rlm.playerHealth.ApplyPermanentMaxHpBonus(GetPermanentMaxHpBonus());
    }
}
