using UnityEngine;

public static class ProgressResetter
{
    // Souls (перманент)
    private const string SOULS_KEY = "kills_lifetime";

    // Perks
    private const string PERK_HP_LEVEL = "perk_hp_level";
    private const string PERK_SOULS_SPENT = "perk_souls_spent";

    // SaveManager (если используешь) — можно оставить, но мы чистим ключи
    private const string SAVE_KEY = "game_save_v1";
    private const string HAS_SAVE_KEY = "has_save_v1";

    // Coins snapshot в SoulCounter
    private const string LAST_RUN_COINS_KEY = "coins_last_run";

    public static void ResetAllProgressForNewGame()
    {
        // 1) Souls
        PlayerPrefs.SetInt(SOULS_KEY, 0);

        // 2) Perks
        PlayerPrefs.SetInt(PERK_HP_LEVEL, 0);
        PlayerPrefs.SetInt(PERK_SOULS_SPENT, 0);

        // 3) SaveManager keys
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.SetInt(HAS_SAVE_KEY, 0);

        // 4) last run coins
        PlayerPrefs.SetInt(LAST_RUN_COINS_KEY, 0);

        PlayerPrefs.Save();

        // Runtime sync (если менеджеры уже живут)
        if (SoulCounter.Instance != null)
        {
            SoulCounter.Instance.SetSouls(0);
            SoulCounter.Instance.RefreshUI();
        }

        if (SoulPerksManager.Instance != null)
        {
            SoulPerksManager.Instance.Load();
            SoulPerksManager.Instance.ApplyToPlayerIfPossible();
        }

        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.SetCoins(0);
            PlayerWallet.Instance.RefreshUI();
        }
    }
}
