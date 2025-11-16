using UnityEngine;

public class EnemySkillFireballSimple : EnemySkillBase
{
    [Header("Fireball (simple)")]
    public GameObject fireballPrefab;
    public Transform firePoint;

    [Header("Overrides (optional)")]
    public bool overrideSpeed = false;
    public float speed = 6f;

    public bool overrideDamage = false;
    public int damage = 10;

    public override void Init(EnemyWalker brain)
    {
        base.Init(brain);

        // если не задано явно – можно воткнуть firePoint = позиция врага
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
        // как у старого врага: если почти по вертикали, стреляем вниз
        Vector2 dir = (Mathf.Abs(toPlayer.x) < 0.5f) ? Vector2.down : toPlayer.normalized;

        GameObject go = Object.Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        var fireball = go.GetComponent<Fireball>();

        if (fireball != null)
        {
            if (overrideSpeed) fireball.speed = speed;
            if (overrideDamage) fireball.damage = damage;
            fireball.Init(dir);
        }

        // этот тик «занят» этим скиллом
        attackConsumed = true;
    }
}
