using UnityEngine;

public interface IProjectile
{
    // Настраивает полёт снаряда.
    void Init(UnityEngine.Vector2 dir, float distance, float speedOverride = -1f, float ignoreFirstMeters = 0f);
}

[CreateAssetMenu(menuName = "Combat/Skill", fileName = "SKILL_New")]
public class SkillDefinition : ScriptableObject
{
    public string displayName;
    [Header("Spawn")]
    public GameObject projectilePrefab;   // префаб с IProjectile (PlayerFireball / PlayerIceShard и т.д.)
    [Header("Params")]
    public int manaCost = 5;
    public float cooldown = 0.6f;

    [Header("UI")]
    public Sprite icon;                   // иконка скилла (на будущее)
}