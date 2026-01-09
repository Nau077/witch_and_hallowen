// Assets/Scripts/Player/Attack/SkillsAndElements/skills/PlayerIceShard.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerIceShard : PlayerProjectileDamageBase
{
    [Header("Ice Freeze")]
    [Range(1, 3)] public int currentIceSkillLevel = 1;

    public int hitsToFreezeLvl1 = 3;
    public int hitsToFreezeLvl2 = 2;
    public int hitsToFreezeLvl3 = 1;

    public float freezeDurationLvl1 = 1.5f;
    public float freezeDurationLvl2 = 2.0f;
    public float freezeDurationLvl3 = 2.5f;

    private static int _globalIceHitCounter;

    protected override void OnHitEnemy(EnemyHealth hp)
    {
        _globalIceHitCounter++;

        int needHits;
        float freezeDuration;

        switch (currentIceSkillLevel)
        {
            default:
            case 1:
                needHits = hitsToFreezeLvl1;
                freezeDuration = freezeDurationLvl1;
                break;
            case 2:
                needHits = hitsToFreezeLvl2;
                freezeDuration = freezeDurationLvl2;
                break;
            case 3:
                needHits = hitsToFreezeLvl3;
                freezeDuration = freezeDurationLvl3;
                break;
        }

        if (_globalIceHitCounter >= Mathf.Max(1, needHits))
        {
            _globalIceHitCounter = 0;
            hp.ApplyFreeze(freezeDuration);
        }
    }
}
