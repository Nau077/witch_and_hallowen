using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Shop Item")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Basic")]
    public string itemId;
    public string displayName;
    public Sprite icon;

    [Header("Price")]
    public ShopCurrency currency = ShopCurrency.Souls;
    public int price = 10;

    [Header("Skill effect")]
    [Tooltip(" онкретный SkillDefinition, в который добавл€ем зар€ды в инвентарь.")]
    public SkillDefinition skillDef;

    [Tooltip("ID скила дл€ системы прогресса.")]
    public SkillId skillId = SkillId.None;

    [Tooltip("≈сли true Ч разлочить скилл и выставить ему уровень.")]
    public bool unlockSkill = false;
    public int unlockLevel = 1;

    [Tooltip("≈сли > 0, установить минимум такой уровень.")]
    public int upgradeToLevel = 0;

    [Tooltip("—колько зар€дов добавить в инвентарь (SkillLoadout).")]
    public int addCharges = 0;

    [Header("Availability conditions")]
    [Tooltip("—килл, который должен быть разлочен, чтобы товар стал активен.")]
    public SkillId requiredSkill = SkillId.None;
    public int requiredSkillLevel = 1;
}
