using System;
using UnityEngine;

public static class RunSaveSystem
{
    private const string KEY_HAS_RUN = "run_has_snapshot";
    private const string KEY_RUN_JSON = "run_snapshot_json";

    [Serializable]
    public class SkillSlotSave
    {
        public int skillId;          // SkillId enum -> int
        public int charges;
        public float cooldownLeft;   // сколько осталось на момент сохранения
    }

    [Serializable]
    public class RunSnapshot
    {
        public int stage;
        public int coins;
        public int playerCurrentHealth;

        public int loadoutActiveIndex;
        public SkillSlotSave[] loadoutSlots;

        // PlayerSkills прогресс (уровни/заряды)
        public int[] skillIds;
        public int[] skillLevels;
        public int[] skillCharges;
    }

    public static bool HasSnapshot()
    {
        return PlayerPrefs.GetInt(KEY_HAS_RUN, 0) == 1 && !string.IsNullOrEmpty(PlayerPrefs.GetString(KEY_RUN_JSON, ""));
    }

    public static void ClearSnapshot()
    {
        PlayerPrefs.SetInt(KEY_HAS_RUN, 0);
        PlayerPrefs.SetString(KEY_RUN_JSON, "");
        PlayerPrefs.Save();
    }

    public static void SaveSnapshot(int stage)
    {
        var snap = new RunSnapshot();
        snap.stage = Mathf.Max(0, stage);

        // coins (run-only, но сохраняем для Continue)
        snap.coins = PlayerWallet.Instance != null ? Mathf.Max(0, PlayerWallet.Instance.coins) : 0;
        snap.playerCurrentHealth = 0;

        if (RunLevelManager.Instance != null && RunLevelManager.Instance.playerHealth != null)
            snap.playerCurrentHealth = Mathf.Max(1, RunLevelManager.Instance.playerHealth.CurrentHealth);

        // loadout
        var loadout = SkillLoadout.Instance;
        if (loadout != null && loadout.slots != null)
        {
            snap.loadoutActiveIndex = loadout.ActiveIndex;

            int n = loadout.slots.Length;
            snap.loadoutSlots = new SkillSlotSave[n];

            for (int i = 0; i < n; i++)
            {
                var s = loadout.slots[i];
                var ss = new SkillSlotSave();

                if (s != null && s.def != null)
                {
                    SkillId sid = s.def.skillId;
                    bool canPersistSlotSkill = PlayerSkills.Instance != null && PlayerSkills.Instance.GetSkillLevel(sid) > 0;

                    if (!canPersistSlotSkill)
                    {
                        ss.skillId = 0;
                        ss.charges = 0;
                        ss.cooldownLeft = 0f;
                        snap.loadoutSlots[i] = ss;
                        continue;
                    }

                    ss.skillId = (int)sid; // ВАЖНО: SkillDefinition должен иметь поле skillId (enum SkillId)
                    ss.charges = s.charges;

                    float left = Mathf.Max(0f, s.cooldownUntil - Time.time);
                    ss.cooldownLeft = left;
                }
                else
                {
                    ss.skillId = 0; // SkillId.None
                    ss.charges = 0;
                    ss.cooldownLeft = 0f;
                }

                snap.loadoutSlots[i] = ss;
            }
        }
        else
        {
            snap.loadoutActiveIndex = -1;
            snap.loadoutSlots = Array.Empty<SkillSlotSave>();
        }

        // PlayerSkills
        var ps = PlayerSkills.Instance;
        if (ps != null)
        {
            ps.ExportStates(out var ids, out var lvls, out var ch);
            snap.skillIds = ids;
            snap.skillLevels = lvls;
            snap.skillCharges = ch;
        }
        else
        {
            snap.skillIds = Array.Empty<int>();
            snap.skillLevels = Array.Empty<int>();
            snap.skillCharges = Array.Empty<int>();
        }

        string json = JsonUtility.ToJson(snap);

        PlayerPrefs.SetInt(KEY_HAS_RUN, 1);
        PlayerPrefs.SetString(KEY_RUN_JSON, json);
        PlayerPrefs.Save();
    }

    public static bool TryLoadSnapshot(out RunSnapshot snapshot)
    {
        snapshot = null;
        if (!HasSnapshot()) return false;

        string json = PlayerPrefs.GetString(KEY_RUN_JSON, "");
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            snapshot = JsonUtility.FromJson<RunSnapshot>(json);
            return snapshot != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[RunSaveSystem] Failed to parse snapshot: " + e.Message);
            return false;
        }
    }

    public static bool ApplySnapshot(RunSnapshot snap)
    {
        if (snap == null) return false;

        // coins
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.SetCoins(snap.coins);

        // PlayerSkills
        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.ImportStates(snap.skillIds, snap.skillLevels, snap.skillCharges);

        if (snap.playerCurrentHealth > 0 && RunLevelManager.Instance != null && RunLevelManager.Instance.playerHealth != null)
            RunLevelManager.Instance.playerHealth.SetCurrentHealthClamped(snap.playerCurrentHealth);

        // SkillLoadout
        var loadout = SkillLoadout.Instance;
        if (loadout != null)
        {
            ApplyLoadout(loadout, snap);
        }

        // UI sync
        SoulCounter.Instance?.RefreshUI();
        return true;
    }

    private static void ApplyLoadout(SkillLoadout loadout, RunSnapshot snap)
    {
        int n = SkillLoadout.SlotsCount;

        if (loadout.slots == null || loadout.slots.Length != n)
            loadout.slots = new SkillSlot[n];

        for (int i = 0; i < n; i++)
            if (loadout.slots[i] == null) loadout.slots[i] = new SkillSlot();

        var arr = snap.loadoutSlots ?? Array.Empty<SkillSlotSave>();

        for (int i = 0; i < n; i++)
        {
            var s = loadout.slots[i];
            var saved = (i < arr.Length) ? arr[i] : null;

            if (saved == null || saved.skillId == 0)
            {
                s.def = null;
                s.charges = 0;
                s.cooldownUntil = 0f;
                continue;
            }

            // найти SkillDefinition по SkillId
            SkillId id = (SkillId)saved.skillId;
            SkillDefinition def = SkillDefinitionLookup.FindById(id);

            // Do not restore locked skills into active loadout.
            if (!IsSkillUnlockedByState(id, snap))
            {
                s.def = null;
                s.charges = 0;
                s.cooldownUntil = 0f;
                continue;
            }

            // If definition is missing, clear slot (do not keep ghost charges).
            if (def == null)
            {
                s.def = null;
                s.charges = 0;
                s.cooldownUntil = 0f;
                continue;
            }

            s.def = def;
            s.charges = saved.charges;

            if (def != null && def.cooldown > 0f && saved.cooldownLeft > 0f)
                s.cooldownUntil = Time.time + saved.cooldownLeft;
            else
                s.cooldownUntil = 0f;
        }

        loadout.SetActiveIndex(snap.loadoutActiveIndex);
        loadout.EnsureValidActive();
    }

    private static bool IsSkillUnlockedByState(SkillId id, RunSnapshot snap)
    {
        if (id == SkillId.None) return false;
        if (snap == null || snap.skillIds == null || snap.skillLevels == null)
            return false;

        int count = Mathf.Min(snap.skillIds.Length, snap.skillLevels.Length);
        for (int i = 0; i < count; i++)
        {
            if ((SkillId)snap.skillIds[i] != id) continue;
            return snap.skillLevels[i] > 0;
        }

        // Fallback for runtime state if arrays are incomplete.
        return PlayerSkills.Instance != null && PlayerSkills.Instance.GetSkillLevel(id) > 0;
    }

}
