using System.Collections.Generic;
using UnityEngine;

public static class SkillDefinitionLookup
{
    private static bool _inited;
    private static Dictionary<SkillId, SkillDefinition> _map;

    private static void InitIfNeeded()
    {
        if (_inited) return;
        _inited = true;

        _map = new Dictionary<SkillId, SkillDefinition>();

        // Самый простой и надёжный вариант без “базы”:
        // грузим все SkillDefinition из Resources.
        // Положи все SkillDefinition в папку Assets/Resources/Skills (или просто Resources где угодно).
        var all = Resources.LoadAll<SkillDefinition>("");
        foreach (var def in all)
        {
            if (def == null) continue;
            if (def.skillId == SkillId.None) continue;
            _map[def.skillId] = def;
        }
    }

    public static SkillDefinition FindById(SkillId id)
    {
        InitIfNeeded();
        if (_map.TryGetValue(id, out var def)) return def;
        return null;
    }
}
