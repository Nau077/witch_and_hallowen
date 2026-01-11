using System.Collections.Generic;
using UnityEngine;

public class PlayerSkills : MonoBehaviour
{
    public static PlayerSkills Instance { get; private set; }

    [System.Serializable]
    public class SkillState
    {
        public SkillId skillId;
        public int level;
        public int charges; // -1 = infinite

        public bool unlocked => level > 0;
    }

    [Header("Initial state (for debug / start)")]
    public SkillState[] initialSkills;

    private readonly Dictionary<SkillId, SkillState> _skills =
        new Dictionary<SkillId, SkillState>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _skills.Clear();
        if (initialSkills != null)
        {
            foreach (var s in initialSkills)
            {
                if (s == null || s.skillId == SkillId.None) continue;
                var copy = new SkillState
                {
                    skillId = s.skillId,
                    level = s.level,
                    charges = s.charges
                };
                _skills[s.skillId] = copy;
            }
        }

        // гарантируем Fireball
        if (!_skills.ContainsKey(SkillId.Fireball))
        {
            _skills[SkillId.Fireball] = new SkillState
            {
                skillId = SkillId.Fireball,
                level = 1,
                charges = -1
            };
        }
    }

    public bool HasSkill(SkillId id) =>
        _skills.ContainsKey(id) && _skills[id].unlocked;

    public int GetSkillLevel(SkillId id)
    {
        if (!_skills.TryGetValue(id, out var state)) return 0;
        return state.level;
    }

    public int GetCharges(SkillId id)
    {
        if (!_skills.TryGetValue(id, out var state)) return 0;
        return state.charges;
    }

    public void UnlockSkill(SkillId id, int level = 1, int charges = 0)
    {
        if (!_skills.TryGetValue(id, out var state))
        {
            state = new SkillState { skillId = id };
            _skills[id] = state;
        }

        if (state.level < level)
            state.level = level;

        if (charges != 0 && state.charges >= 0)
            state.charges += charges;
    }

    public void SetSkillLevel(SkillId id, int newLevel)
    {
        if (!_skills.TryGetValue(id, out var state))
        {
            state = new SkillState { skillId = id };
            _skills[id] = state;
        }

        if (newLevel > state.level)
            state.level = newLevel;
    }

    public void AddCharges(SkillId id, int amount)
    {
        if (!_skills.TryGetValue(id, out var state))
        {
            state = new SkillState { skillId = id };
            _skills[id] = state;
        }

        if (state.charges < 0) return; // infinite
        state.charges += amount;
    }

    public bool MeetsRequirement(SkillId id, int requiredLevel)
    {
        if (id == SkillId.None) return true;
        return HasSkill(id) && GetSkillLevel(id) >= requiredLevel;
    }

    // ======================
    // SAVE/LOAD helpers
    // ======================

    public void ExportStates(out int[] ids, out int[] levels, out int[] charges)
    {
        ids = new int[_skills.Count];
        levels = new int[_skills.Count];
        charges = new int[_skills.Count];

        int i = 0;
        foreach (var kv in _skills)
        {
            ids[i] = (int)kv.Key;
            levels[i] = kv.Value.level;
            charges[i] = kv.Value.charges;
            i++;
        }
    }

    public void ImportStates(int[] ids, int[] levels, int[] charges)
    {
        _skills.Clear();

        int n = ids != null ? ids.Length : 0;
        for (int i = 0; i < n; i++)
        {
            SkillId id = (SkillId)ids[i];
            if (id == SkillId.None) continue;

            int lvl = (levels != null && i < levels.Length) ? levels[i] : 0;
            int ch = (charges != null && i < charges.Length) ? charges[i] : 0;

            _skills[id] = new SkillState
            {
                skillId = id,
                level = lvl,
                charges = ch
            };
        }

        // гарантируем Fireball
        if (!_skills.ContainsKey(SkillId.Fireball))
        {
            _skills[SkillId.Fireball] = new SkillState
            {
                skillId = SkillId.Fireball,
                level = 1,
                charges = -1
            };
        }
    }

    public bool IsSkillUnlocked(SkillId id)
    {
        if (id == SkillId.None) return true;
        return _skills.ContainsKey(id) && _skills[id].unlocked;
    }

}
