using UnityEngine;

public class EnemySkillFireballFan : EnemySkillBase
{
    [Header("Fireball (fan)")]
    public GameObject fireballPrefab;
    public Transform firePoint;

    [Min(1)]
    public int projectileCount = 3;

    [Tooltip("Общий угол веера в градусах.")]
    public float fanAngle = 40f;

    [Header("Overrides (optional)")]
    public float speedOverride = -1f;   // <= 0 значит «не трогать»
    public int damageOverride = -1;     // <= 0 значит «не трогать»

    public override void Init(EnemyWalker brain)
    {
        base.Init(brain);

        if (firePoint == null && brain != null)
        {
            firePoint = brain.transform;
        }
    }

    public override void OnBrainAttackTick(int attackIndex, ref bool attackConsumed)
    {
        if (!CanUse(attackIndex, attackConsumed)) return;
        if (fireballPrefab == null || firePoint == null || brain == null || brain.PlayerTransform == null)
            return;

        Vector2 toPlayer = brain.PlayerTransform.position - firePoint.position;
        Vector2 baseDir = (Mathf.Abs(toPlayer.x) < 0.5f) ? Vector2.down : toPlayer.normalized;

        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        int count = Mathf.Max(1, projectileCount);

        if (count == 1)
        {
            SpawnOne(baseAngle);
        }
        else
        {
            float total = fanAngle;
            float start = baseAngle - total * 0.5f;
            float step = total / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = start + step * i;
                SpawnOne(angle);
            }
        }

        attackConsumed = true;
    }

    private void SpawnOne(float angle)
    {
        if (fireballPrefab == null || firePoint == null) return;

        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        GameObject go = Object.Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        var fireball = go.GetComponent<Fireball>();

        if (fireball != null)
        {
            if (speedOverride > 0f) fireball.speed = speedOverride;
            if (damageOverride > 0) fireball.damage = damageOverride;
            fireball.Init(dir);
        }
    }
}
