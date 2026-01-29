using UnityEngine;

public enum ShopItemEffectType
{
    None = 0,

    // Перманентные покупки за души
    IncreaseMaxHealth = 10,   // +50 max hp, до 4 раз, цена растёт
    ResetSoulPerks = 11,      // сброс перков + возврат потраченных душ

    // NEW: Перманентный скилл дэш (уровни 1..3, но 1 всегда есть)
    IncreaseDashLevel = 12
}

[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Shop Item")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Basic")]
    public string itemId;
    public string displayName;
    public Sprite icon;

    [Header("Price")]
    [Tooltip("Чем платим за товар: Coins (монеты) или Souls (души).")]
    public ShopCurrency currency = ShopCurrency.Souls;

    [Tooltip("Базовая стоимость (для обычных товаров). Для перков (IncreaseMaxHealth) цена берётся динамически.")]
    public int price = 10;

    [Header("Effect Type")]
    [Tooltip("Тип эффекта. Для обычных предметов оставь None.")]
    public ShopItemEffectType effectType = ShopItemEffectType.None;

    // -------------------------
    // Старое: скиллы/заряды
    // -------------------------
    [Header("Skill effect (optional)")]
    public SkillDefinition skillDef;

    public SkillId skillId = SkillId.None;

    public bool unlockSkill = false;
    public int unlockLevel = 1;

    public int upgradeToLevel = 0;
    public int addCharges = 0;

    [Header("Availability conditions")]
    public SkillId requiredSkill = SkillId.None;
    public int requiredSkillLevel = 1;
}
