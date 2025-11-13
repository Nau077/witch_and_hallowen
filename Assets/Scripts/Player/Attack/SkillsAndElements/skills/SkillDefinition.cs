// SkillDefinition.cs
using UnityEngine;

/// <summary>
/// Типы уникальных эффектов для навыков (заморозка, замедление и т.д.)
/// </summary>
public enum SkillTag
{
    Default,     // обычный урон без особых эффектов
    IceFreeze,   // заморозка врага
    EarthSlow,   // замедление
    AirPush      // отбрасывание / толчок
}

/// <summary>
/// Элементы, к которым принадлежит навык (для визуала, иконок, систем урона)
/// </summary>
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
    // ------------------------------------------------------------------------
    // 🔹 ИДЕНТИФИКАЦИЯ
    // ------------------------------------------------------------------------
    [Header("Identity")]
    [Tooltip("Отображаемое имя навыка (в UI и отладке).")]
    public string displayName;

    [Tooltip("Иконка навыка для интерфейса.")]
    public Sprite icon;

    [Tooltip("К какому элементу относится этот навык.")]
    public ElementId element;

    // ------------------------------------------------------------------------
    // 🔹 ОСНОВНЫЕ ПАРАМЕТРЫ
    // ------------------------------------------------------------------------
    [Header("Core")]
    [Tooltip("Префаб снаряда, реализующего IProjectile.")]
    public GameObject projectilePrefab;

    [Tooltip("Базовый урон, наносимый этим скиллом.")]
    public int damage = 10;

    [Tooltip("Кулдаун (в секундах) между использованием.")]
    [Min(0f)] public float cooldown = 0.6f;

    [Tooltip("Сколько опыта получает навык за одно использование.")]
    [Min(0)] public int xpPerUse = 1;

    // ------------------------------------------------------------------------
    // 🔹 МАНА
    // ------------------------------------------------------------------------
    [Header("Mana Cost")]
    [Tooltip("Сколько маны тратится за одно применение навыка.")]
    [Min(0)] public int manaCostPerShot = 0;

    // ------------------------------------------------------------------------
    // 🔹 ЗАРЯДЫ (если навык ограничен)
    // ------------------------------------------------------------------------
    [Header("Charges")]
    [Tooltip("Если включено — навык имеет бесконечные заряды (например, базовая атака).")]
    public bool infiniteCharges = false;

    [Tooltip("Сколько зарядов доступно при старте игры / покупке.")]
    [Min(0)] public int startCharges = 0;

    [Tooltip("Цена одной единицы заряда при покупке (в монетах).")]
    [Min(0)] public int coinCostPerCharge = 1;

    // ------------------------------------------------------------------------
    // 🔹 УНИКАЛЬНЫЕ ЭФФЕКТЫ
    // ------------------------------------------------------------------------
    [Header("Unique flags/params")]
    [Tooltip("Тип уникального эффекта (заморозка, замедление, толчок и т.д.).")]
    public SkillTag tag = SkillTag.Default;

    [Tooltip("Если навык — IceFreeze, длительность заморозки (сек).")]
    [Min(0)] public float freezeSeconds = 0f;

    [Tooltip("Если навык — EarthSlow, сила замедления (0..1).")]
    [Range(0, 1)] public float slowPercent = 0f;

    [Tooltip("Если навык — EarthSlow, длительность замедления (сек).")]
    [Min(0)] public float slowSeconds = 0f;
}
