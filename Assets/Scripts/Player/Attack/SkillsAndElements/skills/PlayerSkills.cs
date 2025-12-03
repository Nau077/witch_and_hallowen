using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простое хранилище прогресса скиллов:
/// - какие разлочены,
/// - какой уровень,
/// - сколько "общих" зарядов (если хочешь использовать).
/// Это не UI, а чистая логика прогресса.
/// </summary>
public class PlayerSkills : MonoBehaviour
{
    public static PlayerSkills Instance { get; private set; }

    [System.Serializable]
    public class SkillState
    {
        public SkillId skillId;
        public int level;
        public int charges;  // глобальные заряды (по желанию)

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

        // гарантируем фаербол 1 уровня с "бесконечностью"
        if (!_skills.ContainsKey(SkillId.Fireball))
        {
            _skills[SkillId.Fireball] = new SkillState
            {
                skillId = SkillId.Fireball,
                level = 1,
                charges = -1 // -1 = бесконечные
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

        if (state.charges < 0) return; // бесконечные - не считаем
        state.charges += amount;
    }

    public bool MeetsRequirement(SkillId id, int requiredLevel)
    {
        if (id == SkillId.None) return true;
        return HasSkill(id) && GetSkillLevel(id) >= requiredLevel;
    }
}
