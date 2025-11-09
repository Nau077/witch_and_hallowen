// ProjectileSkillEffect.cs
using UnityEngine;

public class ProjectileSkillEffect : MonoBehaviour
{
    public SkillDefinition sourceSkill; // заполняется при спавне

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!sourceSkill) return;
        if (other.CompareTag("Enemy"))
        {
            var a = other.GetComponent<IEnemyAilments>();
            if (a != null)
            {
                if (sourceSkill.tag == SkillTag.IceFreeze && sourceSkill.freezeSeconds > 0f)
                    a.ApplyFreeze(sourceSkill.freezeSeconds);

                if (sourceSkill.tag == SkillTag.EarthSlow && sourceSkill.slowPercent > 0f && sourceSkill.slowSeconds > 0f)
                    a.ApplySlow(sourceSkill.slowPercent, sourceSkill.slowSeconds);
            }
        }
    }
}
