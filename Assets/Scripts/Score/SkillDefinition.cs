using UnityEngine;

public enum SkillTag
{
    Default,
    IceFreeze,
    EarthSlow,
    AirPush
}

public enum ElementId
{
    Fire = 0,
    Ice = 1,
    Earth = 2,
    Air = 3
}

[CreateAssetMenu(menuName = "Combat/Skill (New)", fileName = "SKILL_New")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID скила для связки с прогрессом/магазином.")]
    public SkillId skillId;     // <<< важно задать в инспекторе

    [Tooltip("Отображаемое имя навыка (в UI и отладке).")]
    public string displayName;

    [Tooltip("Иконка навыка для интерфейса.")]
    public Sprite icon;

    [Tooltip("К какому элементу относится этот навык.")]
    public ElementId element;

    [Header("Core")]
    [Tooltip("Префаб снаряда, реализующего IProjectile.")]
    public GameObject projectilePrefab;

    [Tooltip("Базовый урон.")]
    public int damage = 10;

    [Tooltip("Кулдаун (сек).")]
    [Min(0f)] public float cooldown = 0.6f;

    [Tooltip("Сколько опыта получает навык за одно использование.")]
    [Min(0)] public int xpPerUse = 1;

    [Header("Mana Cost")]
    [Tooltip("Сколько маны тратится за одно применение.")]
    [Min(0)] public int manaCostPerShot = 0;

    [Header("Charges")]
    [Tooltip("Имеет ли навык бесконечные заряды.")]
    public bool infiniteCharges = false;

    [Tooltip("Сколько зарядов доступно при старте (если не бесконечный).")]
    [Min(0)] public int startCharges = 0;

    [Tooltip("Цена одной единицы заряда при покупке (в монетах).")]
    [Min(0)] public int coinCostPerCharge = 1;

    [Header("Unique flags/params")]
    [Tooltip("Тип уникального эффекта (заморозка / замедление / толчок).")]
    public SkillTag tag = SkillTag.Default;

    [Tooltip("Если IceFreeze, длительность заморозки, сек.")]
    [Min(0)] public float freezeSeconds = 0f;

    [Tooltip("Если EarthSlow, сила замедления (0..1).")]
    [Range(0, 1)] public float slowPercent = 0f;

    [Tooltip("Если EarthSlow, длительность замедления, сек.")]
    [Min(0)] public float slowSeconds = 0f;
}
