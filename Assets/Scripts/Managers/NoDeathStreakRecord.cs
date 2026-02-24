using System;
using UnityEngine;

public static class NoDeathStreakRecord
{
    private const string KEY_CURRENT_STREAK = "no_death_streak_current";
    private const string KEY_BEST_STREAK = "no_death_streak_best";
    private const string KEY_LAST_COUNTED_STAGE = "no_death_streak_last_stage";

    public static event Action OnChanged;

    public static int CurrentStreak => Mathf.Max(0, PlayerPrefs.GetInt(KEY_CURRENT_STREAK, 0));
    public static int BestStreak => Mathf.Max(0, PlayerPrefs.GetInt(KEY_BEST_STREAK, 0));

    public static void RegisterStageCleared(int clearedStage, int totalStages)
    {
        if (clearedStage <= 0 || totalStages <= 0)
            return;

        int current = CurrentStreak;
        int best = BestStreak;
        int lastStage = Mathf.Max(0, PlayerPrefs.GetInt(KEY_LAST_COUNTED_STAGE, 0));

        bool isNextFromStart = current == 0 && clearedStage == 1;
        bool isRegularNext = current > 0 && clearedStage == lastStage + 1;
        bool isLoopNext = current > 0 && lastStage == totalStages && clearedStage == 1;
        bool shouldIncrement = isNextFromStart || isRegularNext || isLoopNext;

        if (!shouldIncrement)
            return;

        int nextCurrent = current + 1;
        PlayerPrefs.SetInt(KEY_CURRENT_STREAK, nextCurrent);
        PlayerPrefs.SetInt(KEY_LAST_COUNTED_STAGE, clearedStage);

        if (nextCurrent > best)
        {
            best = nextCurrent;
            PlayerPrefs.SetInt(KEY_BEST_STREAK, best);
        }

        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    public static void RegisterDeath()
    {
        bool hadCurrent = CurrentStreak != 0;
        bool hadLastStage = PlayerPrefs.GetInt(KEY_LAST_COUNTED_STAGE, 0) != 0;
        if (!hadCurrent && !hadLastStage)
            return;

        PlayerPrefs.SetInt(KEY_CURRENT_STREAK, 0);
        PlayerPrefs.SetInt(KEY_LAST_COUNTED_STAGE, 0);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
