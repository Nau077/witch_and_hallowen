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

        // Base source: Resources.
        var all = Resources.LoadAll<SkillDefinition>("");
        foreach (var def in all)
        {
            if (def == null) continue;
            if (def.skillId == SkillId.None) continue;
            if (_map.ContainsKey(def.skillId))
            {
                Debug.LogWarning("[SkillDefinitionLookup] Duplicate SkillId in Resources: " + def.skillId + ". Last asset wins: " + def.name);
            }
            _map[def.skillId] = def;
        }

        // Fallback source: loaded assets that are not in Resources.
        var loaded = Resources.FindObjectsOfTypeAll<SkillDefinition>();
        for (int i = 0; i < loaded.Length; i++)
        {
            var def = loaded[i];
            if (def == null) continue;
            if (def.skillId == SkillId.None) continue;
            if (_map.ContainsKey(def.skillId)) continue;
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
