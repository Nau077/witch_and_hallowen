using UnityEngine;
using System;

[DefaultExecutionOrder(-200)]
public class SoulPerksManager : MonoBehaviour
{
    public static SoulPerksManager Instance { get; private set; }

    // Событие для UI и других систем
    public event Action OnPerksChanged;

    // --- PlayerPrefs keys ---
    private const string KEY_HP_LEVEL = "perk_hp_level";       // 0..hpMaxPurchases
    private const string KEY_SOULS_SPENT = "perk_souls_spent"; // суммарно потрачено на перки

    [Header("Perk: Max HP")]
    public int hpStep = 50;
    public int hpMaxPurchases = 4;
    public int hpBasePrice = 50;

    public int HpLevel { get; private set; }       // 0..4
    public int SoulsSpent { get; private set; }    // суммарно потрачено

    [Header("Perk: Reset")]
    public int resetPrice = 100;

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
        // 50/100/150/200
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

    public bool TryBuyHealthUpgrade()
    {
        if (HpLevel >= hpMaxPurchases) return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        int price = GetHealthUpgradePrice();
        if (sc.killsLifetime < price) return false;

        // списали души
        sc.killsLifetime -= price;
        sc.RefreshUI();

        // сохранили прогресс перков
        SoulsSpent += price;
        HpLevel++;

        Save();
        ApplyToPlayerIfPossible();
        NotifyChanged();

        return true;
    }

    public bool ResetAllPerksWithRefund()
    {
        if (!HasAnythingToReset())
            return false;

        var sc = SoulCounter.Instance;
        if (sc == null) return false;

        // цена ресета = 100 душ
        if (sc.killsLifetime < resetPrice)
            return false;

        // вернём (SoulsSpent - resetPrice), но не меньше 0
        int refund = Mathf.Max(0, SoulsSpent - resetPrice);

        sc.killsLifetime -= resetPrice;
        sc.killsLifetime += refund;
        sc.RefreshUI();

        HpLevel = 0;
        SoulsSpent = 0;

        Save();
        ApplyToPlayerIfPossible();
        NotifyChanged();

        return true;
    }

    public int GetPermanentMaxHpBonus()
    {
        return HpLevel * hpStep;
    }

    public void ApplyToPlayerIfPossible()
    {
        var rlm = RunLevelManager.Instance;
        if (rlm == null || rlm.playerHealth == null) return;

        rlm.playerHealth.ApplyPermanentMaxHpBonus(GetPermanentMaxHpBonus());
    }
}
