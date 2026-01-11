using System;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int version = 1;

    // meta
    public long unixTimeUtc;

    // run/progress
    public int currentStage = 0;

    // currencies
    public int souls = 0; // permanent
    public int coins = 0; // run currency (for Continue)

    // perks
    public int perkHpLevel = 0;
    public int perkSoulsSpent = 0;

    // skills (unlock/level/charges) — универсально
    public SkillSaveEntry[] skills = Array.Empty<SkillSaveEntry>();

    [Serializable]
    public struct SkillSaveEntry
    {
        public int skillId;     // cast from your SkillId enum
        public bool unlocked;
        public int level;
        public int charges;
    }
}
