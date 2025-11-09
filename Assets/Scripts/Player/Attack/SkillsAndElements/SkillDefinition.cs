// SkillDefinition.cs
using UnityEngine;

public enum SkillTag { Default, IceFreeze, EarthSlow, AirPush /* расшир€й под уникальные эффекты */ }
public enum ElementId { Fire = 0, Ice = 1, Earth = 2, Air = 3 }

[CreateAssetMenu(menuName = "Combat/Skill (New)", fileName = "SKILL_New")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public Sprite icon;
    public ElementId element;

    [Header("Core")]
    public GameObject projectilePrefab; // любой префаб с IProjectile
    public int damage = 10;
    [Min(0)] public float cooldown = 0.6f;
    [Min(0)] public int xpPerUse = 1;

    [Header("Charges")]
    public bool infiniteCharges = false;   // дл€ дефолтного фаербола = true
    [Min(0)] public int startCharges = 0;  // дл€ расходуемых Ч сколько дать при старте/покупке набора
    [Min(0)] public int coinCostPerCharge = 1; // цена 1 зар€да при покупке (пример: лЄд = 1 монета за 1 зар€д)

    [Header("Unique flags/params")]
    public SkillTag tag = SkillTag.Default;
    [Min(0)] public float freezeSeconds = 0f; // если IceFreeze Ч заморозка
    [Range(0, 1)] public float slowPercent = 0f; // если EarthSlow Ч замедление
    [Min(0)] public float slowSeconds = 0f;
}
