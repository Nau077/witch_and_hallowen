using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class SoulPerksManager : MonoBehaviour
{
    public static SoulPerksManager Instance { get; private set; }

    public event Action OnPerksChanged;

    // --- PlayerPrefs keys ---
    private const string KEY_HP_LEVEL = "perk_hp_level";         // 0..hpMaxPurchases
    private const string KEY_DASH_LEVEL = "perk_dash_level";     // 0..dashMaxPurchases (0..2)
    private const string KEY_MANA_LEVEL = "perk_mana_level";     // 0..manaMaxPurchases (0..2)
    private const string KEY_STAMINA_LEVEL = "perk_stamina_level"; // 0..staminaMaxPurchases (0..2)
    private const string KEY_SOULS_SPENT = "perk_souls_spent";   // суммарно потрачено на перки

    [Header("Perk: Max HP")]
    public int hpStep = 50;
    public int hpMaxPurchases = 4;
    public int hpBasePrice = 50;

    public int HpLevel { get; private set; }       // 0..4

    [Header("Perk: Dash Level (1..3, but 1 is default)")]
    [Tooltip("Максимум покупок для дэша. 2 покупки = уровни 2 и 3 (уровень 1 бесплатный по дефолту).")]
    public int dashMaxPurchases = 2;

    [Tooltip("Базовая цена улучшения дэша. Например: 60/120")]
    public int dashBasePrice = 60;

    /// <summary>
    /// 0..2 (покупки). Реальный уровень = 1 + DashLevel -> 1..3.
    /// </summary>
    public int DashLevel { get; private set; }    // 0..2

    [Header("Perk: Mana Level")]
    [Tooltip("Максимум покупок для маны. 2 покупки по +20 маны каждая.")]
    public int manaMaxPurchases = 2;

    [Tooltip("Прирост маны за каждый уровень (+20 за уровень).")]
    public int manaStep = 20;

    [Tooltip("Базовая цена улучшения маны. Например: 60/120")]
    public int manaBasePrice = 60;

    public int ManaLevel { get; private set; }    // 0..2

    [Header("Perk: Stamina Level (для Dash выносливости)")]
    [Tooltip("Максимум покупок для выносливости. 2 покупки по +15 выносливости каждая.")]
    public int staminaMaxPurchases = 2;

    [Tooltip("Прирост выносливости за каждый уровень (+15 за уровень).")]
    public int staminaStep = 15;

    [Tooltip("Базовая цена улучшения выносливости. Например: 60/120")]
    public int staminaBasePrice = 60;

    public int StaminaLevel { get; private set; } // 0..2

    [Header("Perk: Reset")]
    public int resetPrice = 100;

    public int SoulsSpent { get; private set; }    // суммарно потрачено

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void Start()
    {
        ApplyToPlayerIfPossible();
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnPerksChanged?.Invoke();
    }

    public void Load()
    {
        HpLevel = Mathf.Clamp(PlayerPrefs.GetInt(KEY_HP_LEVEL, 0), 0, hpMaxPurchases);
        DashLevel = Mathf.Clamp(PlayerPrefs.GetInt(KEY_DASH_LEVEL, 0), 0, dashMaxPurchases);
        ManaLevel = Mathf.Clamp(PlayerPrefs.GetInt(KEY_MANA_LEVEL, 0), 0, manaMaxPurchases);
        StaminaLevel = Mathf.Clamp(PlayerPrefs.GetInt(KEY_STAMINA_LEVEL, 0), 0, staminaMaxPurchases);
        SoulsSpent = Mathf.Max(0, PlayerPrefs.GetInt(KEY_SOULS_SPENT, 0));
    }

    public void Save()
    {
        PlayerPrefs.SetInt(KEY_HP_LEVEL, HpLevel);
        PlayerPrefs.SetInt(KEY_DASH_LEVEL, DashLevel);
        PlayerPrefs.SetInt(KEY_MANA_LEVEL, ManaLevel);
        PlayerPrefs.SetInt(KEY_STAMINA_LEVEL, StaminaLevel);
        PlayerPrefs.SetInt(KEY_SOULS_SPENT, SoulsSpent);
        PlayerPrefs.Save();
    }

    // ---------------- HP ----------------

    public int GetHealthUpgradePrice()
    {
        // 50/100/150/200
        int nextIndex = HpLevel; // 0..3
        return hpBasePrice * (nextIndex + 1);
    }

    public bool CanBuyHealthUpgrade()
    {
        if (HpLevel >= hpMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        return sc.souls >= GetHealthUpgradePrice();
    }

    public bool TryBuyHealthUpgrade()
    {
        if (HpLevel >= hpMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        int price = GetHealthUpgradePrice();
        if (sc.souls < price) return false;

        sc.SetSouls(sc.souls - price);
        sc.RefreshUI();

        SoulsSpent += price;
        HpLevel++;

        Save();
        ApplyToPlayerIfPossible();
        NotifyChanged();

        return true;
    }

    public int GetPermanentMaxHpBonus()
    {
        return HpLevel * hpStep;
    }

    // ---------------- DASH ----------------

    /// <summary>
    /// Реальный уровень дэша: 1..3 (уровень 1 всегда бесплатно).
    /// </summary>
    public int GetDashRealLevel()
    {
        return Mathf.Clamp(1 + DashLevel, 1, 3);
    }

    public int GetDashUpgradePrice()
    {
        // Например: 60/120 (зависит от dashBasePrice)
        // Покупка #1: DashLevel=0 => nextIndex=0 => 60
        // Покупка #2: DashLevel=1 => nextIndex=1 => 120
        int nextIndex = DashLevel; // 0..1
        return dashBasePrice * (nextIndex + 1);
    }

    public bool CanBuyDashUpgrade()
    {
        if (DashLevel >= dashMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        return sc.souls >= GetDashUpgradePrice();
    }

    public bool TryBuyDashUpgrade()
    {
        if (DashLevel >= dashMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        int price = GetDashUpgradePrice();
        if (sc.souls < price) return false;

        sc.SetSouls(sc.souls - price);
        sc.RefreshUI();

        SoulsSpent += price;
        DashLevel++;

        Save();
        ApplyToPlayerIfPossible();
        NotifyChanged();

        return true;
    }

    // ---------------- MANA ----------------

    public int GetManaUpgradePrice()
    {
        // 60/120 (зависит от manaBasePrice)
        // Покупка #1: ManaLevel=0 => nextIndex=0 => 60
        // Покупка #2: ManaLevel=1 => nextIndex=1 => 120
        int nextIndex = ManaLevel; // 0..1
        return manaBasePrice * (nextIndex + 1);
    }

    public bool CanBuyManaUpgrade()
    {
        if (ManaLevel >= manaMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        return sc.souls >= GetManaUpgradePrice();
    }

    public bool TryBuyManaUpgrade()
    {
        if (ManaLevel >= manaMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        int price = GetManaUpgradePrice();
        if (sc.souls < price) return false;

        sc.SetSouls(sc.souls - price);
        sc.RefreshUI();

        SoulsSpent += price;
        ManaLevel++;

        Save();
        ApplyToPlayerIfPossible();
        NotifyChanged();

        return true;
    }

    public int GetPermanentManaBonus()
    {
        return ManaLevel * manaStep;
    }

    // ---------------- STAMINA ----------------

    public int GetStaminaUpgradePrice()
    {
        // 60/120 (зависит от staminaBasePrice)
        // Покупка #1: StaminaLevel=0 => nextIndex=0 => 60
        // Покупка #2: StaminaLevel=1 => nextIndex=1 => 120
        int nextIndex = StaminaLevel; // 0..1
        return staminaBasePrice * (nextIndex + 1);
    }

    public bool CanBuyStaminaUpgrade()
    {
        if (StaminaLevel >= staminaMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        return sc.souls >= GetStaminaUpgradePrice();
    }

    public bool TryBuyStaminaUpgrade()
    {
        if (StaminaLevel >= staminaMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        int price = GetStaminaUpgradePrice();
        if (sc.souls < price) return false;

        sc.SetSouls(sc.souls - price);
        sc.RefreshUI();

        SoulsSpent += price;
        StaminaLevel++;

        Save();
        ApplyToPlayerIfPossible();
        NotifyChanged();

        return true;
    }

    public int GetPermanentStaminaBonus()
    {
        return StaminaLevel * staminaStep;
    }

    // ---------------- RESET ----------------

    public bool HasAnythingToReset()
    {
        // учитываем HP, Dash, Mana, Stamina
        return HpLevel > 0 || DashLevel > 0 || ManaLevel > 0 || StaminaLevel > 0 || SoulsSpent > 0;
    }

    public bool ResetAllPerksWithRefund()
    {
        if (!HasAnythingToReset())
            return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        if (sc.souls < resetPrice)
            return false;

        // вернём (SoulsSpent - resetPrice), но не меньше 0
        int refund = Mathf.Max(0, SoulsSpent - resetPrice);

        sc.SetSouls(sc.souls - resetPrice + refund);
        sc.RefreshUI();

        HpLevel = 0;
        DashLevel = 0;
        ManaLevel = 0;
        StaminaLevel = 0;
        SoulsSpent = 0;

        Save();
        ApplyToPlayerIfPossible();
        NotifyChanged();

        return true;
    }

    // ---------------- APPLY ----------------

    public void ApplyToPlayerIfPossible()
    {
        var rlm = RunLevelManager.Instance;
        if (rlm == null) return;

        if (rlm.playerHealth != null)
            rlm.playerHealth.ApplyPermanentMaxHpBonus(GetPermanentMaxHpBonus());

        // Дэш применять напрямую не нужно — PlayerDash читает уровень из SoulPerksManager в рантайме.
        // Но если у тебя будет UI, который зависит от перков, OnPerksChanged уже триггерит обновления.
    }
}
