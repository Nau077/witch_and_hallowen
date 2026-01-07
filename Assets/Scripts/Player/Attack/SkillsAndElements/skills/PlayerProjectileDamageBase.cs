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

    // ---------- CRIT (Enemy Blink + Micro Stagger) ----------
    [Header("Crit (Enemy Blink + Micro Stagger)")]
    [Tooltip("Включает crit-мигание у врага при попадании")]
    public bool enableCritBlink = true;

    [Range(0f, 1f)]
    [Tooltip("Шанс крита. Дефолт задаётся здесь, но может быть переопределён в инспекторе.")]
    public float critChance = 0.3f;

    [Min(0f)]
    [Tooltip("Длительность крита (сек). Важно: от неё же считается базовая длительность микро-стаггера на враге.")]
    public float critDuration = 0.5f;

    [Tooltip("Интервал мигания. <= 0 — взять дефолт врага")]
    public float critBlinkInterval = -1f;

    [Header("Micro Stagger Strength")]
    [Tooltip("Множитель микро-стаггера от этого снаряда. 1 = норм, 2 = сильнее, 0 = вообще без стаггера.")]
    [Min(0f)]
    public float staggerMultiplier = 1f;

    [Tooltip("Если враг уже в атаке/подготовке атаки — можно дать более сильный стаггер, чтобы сбивать атаки.")]
    [Min(0f)]
    public float staggerMultiplierWhenEnemyAttacking = 1.5f;
    // ---------------------------------------

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
            ApplyDamage(hp, other);
            OnHitEnemy(hp);
        }

        Destroy(gameObject);
        return;

    OTHER:
        if (other.CompareTag("Border") || other.CompareTag("EnemyLaneLimit"))
            Destroy(gameObject);
    }

    private void ApplyDamage(EnemyHealth hp, Collider2D enemyCollider)
    {
        if (!enableCritBlink || critChance <= 0f || critDuration <= 0f)
        {
            hp.TakeDamage(damage);
            return;
        }

        // Если враг сейчас в атаке — усиливаем стаггер (чтобы лучше сбивало атаки)
        float mult = Mathf.Max(0f, staggerMultiplier);

        var walker = enemyCollider.GetComponent<EnemyWalker>();
        if (walker != null && walker.IsBusyAttacking)
            mult = Mathf.Max(0f, staggerMultiplierWhenEnemyAttacking);

        hp.TakeDamage(damage, critChance, critDuration, critBlinkInterval, mult);
    }

    /// <summary>
    /// Хук для наследников (freeze, poison, chain, etc)
    /// </summary>
    protected virtual void OnHitEnemy(EnemyHealth hp) { }
}
