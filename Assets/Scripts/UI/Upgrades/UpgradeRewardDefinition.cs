using UnityEngine;

public enum UpgradeRewardType
{
    None = 0,
    SkillUnlockOrUpgrade = 1,
    SkillCharges = 2,
    HealthHeart = 3,
    ManaHeart = 4,
    StaminaHeart = 5
}

[CreateAssetMenu(fileName = "UpgradeReward", menuName = "Upgrades/Reward Definition")]
public class UpgradeRewardDefinition : ScriptableObject
{
    [Header("UI")]
    public string rewardId;
    public string displayName;
    public Sprite icon;
    [TextArea(2, 5)] public string description;

    [Header("Reward")]
    public UpgradeRewardType rewardType = UpgradeRewardType.None;

    [Header("Skill settings")]
    public SkillId skillId = SkillId.None;
    public SkillDefinition skillDefinitionOverride;
    [Min(1)] public int targetSkillLevel = 1;
    [Min(0)] public int addCharges = 0;
    public bool addToLoadout = true;

    [Header("Perk settings")]
    [Min(1)] public int amount = 1;
}
