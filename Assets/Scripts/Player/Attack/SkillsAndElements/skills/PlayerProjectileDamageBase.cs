// Assets/Scripts/Player/Attack/SkillsAndElements/skills/PlayerProjectileDamageBase.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class PlayerProjectileDamageBase : MonoBehaviour, IProjectile
{
    [Header("Flight")]
    public float speed = 8f;
    public float lifetime = 2f;

    [Header("Damage")]
    public int damage = 10;

    [HideInInspector]
    public float ignoreEnemiesFirstMeters = 0f;

    [Header("Crit (Color Blink + Stagger)")]
    public bool enableCrit = true;

    [Range(0f, 1f)]
    public float critChance = 0.25f;

    [Min(0f)]
    public float critDuration = 0.5f;

    [Tooltip("Интервал мигания. <= 0 — взять дефолт врага")]
    public float critBlinkInterval = -1f;

    [Min(0f)]
    [Tooltip("0 = нет стаггера вообще, 1 = норм, 2 = сильнее")]
    public float staggerMultiplier = 1f;

    [Tooltip("Цвет мигания при крите (задаётся в префабе снаряда)")]
    public Color critBlinkColor = Color.red;

    protected Vector2 _dir = Vector2.up;
    protected Vector2 _startPos;
    protected float _traveled;

    public virtual void Init(
        Vector2 dir,
        float distance,
        float speedOverride = -1f,
        float ignoreFirstMeters = 0f
    )
    {
        _dir = dir.normalized;
        if (speedOverride > 0f) speed = speedOverride;

        lifetime = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, speed));
        ignoreEnemiesFirstMeters = Mathf.Max(0f, ignoreFirstMeters);

        _startPos = transform.position;
        _traveled = 0f;

        Destroy(gameObject, lifetime);
    }

    protected virtual void Update()
    {
        Vector3 delta = (Vector3)(_dir * speed * Time.deltaTime);
        transform.Translate(delta, Space.World);
        _traveled = Vector2.Distance(transform.position, _startPos);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) goto OTHER;

        if (_traveled < ignoreEnemiesFirstMeters) return;

        var hp = other.GetComponent<EnemyHealth>();
        if (hp != null)
        {
            ApplyDamage(hp);
            OnHitEnemy(hp);
        }

        Destroy(gameObject);
        return;

    OTHER:
        if (other.CompareTag("Border") || other.CompareTag("EnemyLaneLimit"))
            Destroy(gameObject);
    }

    protected void ApplyDamage(EnemyHealth hp)
    {
        if (!enableCrit || critChance <= 0f || critDuration <= 0f)
        {
            hp.TakeDamage(damage);
            return;
        }

        hp.TakeDamage(
            damage,
            critChance,
            critDuration,
            critBlinkInterval,
            staggerMultiplier,
            critBlinkColor
        );
    }

    protected virtual void OnHitEnemy(EnemyHealth hp) { }
}
