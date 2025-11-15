using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerIceShard : MonoBehaviour, IProjectile
{
    [Header("Flight")]
    public float speed = 7.5f;
    public float lifetime = 2.2f;

    [Header("Damage")]
    public int damage = 9;

    [HideInInspector] public float ignoreEnemiesFirstMeters = 0f;

    // ---------- ICE FREEZE (настройки СКИЛА) ----------
    [Header("Ice Freeze Settings")]
    [Tooltip("Текущий уровень скила льда (1 – базовый, потом 2 и 3).")]
    [Range(1, 3)] public int currentIceSkillLevel = 1;

    [Tooltip("Через сколько попаданий IceShard по ЛЮБЫМ врагам замораживать на 1-м уровне.")]
    public int hitsToFreezeLvl1 = 3;
    [Tooltip("Через сколько попаданий IceShard на 2-м уровне.")]
    public int hitsToFreezeLvl2 = 2;
    [Tooltip("Через сколько попаданий IceShard на 3-м уровне.")]
    public int hitsToFreezeLvl3 = 1;

    [Tooltip("Длительность заморозки на 1-м уровне, сек.")]
    public float freezeDurationLvl1 = 1.5f;
    [Tooltip("Длительность заморозки на 2-м уровне, сек.")]
    public float freezeDurationLvl2 = 2.0f;
    [Tooltip("Длительность заморозки на 3-м уровне, сек.")]
    public float freezeDurationLvl3 = 2.5f;

    // Глобальный счётчик попаданий этим скиллом (между всеми снарядами)
    private static int _globalIceHitCounter = 0;

    private Vector2 _dir = Vector2.up;
    private Vector2 _startPos;
    private float _traveled;

    public void Init(Vector2 dir, float distance, float speedOverride = -1f, float ignoreFirstMeters = 0f)
    {
        _dir = dir.normalized;
        if (speedOverride > 0f) speed = speedOverride;

        lifetime = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, speed));
        ignoreEnemiesFirstMeters = Mathf.Max(0f, ignoreFirstMeters);

        _startPos = (Vector2)transform.position;
        _traveled = 0f;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        Vector3 d = (Vector3)(_dir * speed * Time.deltaTime);
        transform.Translate(d, Space.World);
        _traveled = Vector2.Distance(transform.position, _startPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (_traveled < ignoreEnemiesFirstMeters) return;

            var hp = other.GetComponent<EnemyHealth>();
            if (hp != null)
            {
                // 1) Наносим обычный урон, как всегда
                hp.TakeDamage(damage);

                // 2) Регистрируем ледяное попадание ГЛОБАЛЬНО
                _globalIceHitCounter++;

                int needHits;
                float freezeDuration;

                switch (currentIceSkillLevel)
                {
                    default:
                    case 1:
                        needHits = Mathf.Max(1, hitsToFreezeLvl1);
                        freezeDuration = Mathf.Max(0.1f, freezeDurationLvl1);
                        break;
                    case 2:
                        needHits = Mathf.Max(1, hitsToFreezeLvl2);
                        freezeDuration = Mathf.Max(0.1f, freezeDurationLvl2);
                        break;
                    case 3:
                        needHits = Mathf.Max(1, hitsToFreezeLvl3);
                        freezeDuration = Mathf.Max(0.1f, freezeDurationLvl3);
                        break;
                }

                if (_globalIceHitCounter >= needHits)
                {
                    _globalIceHitCounter = 0; // начинаем считать заново
                    hp.ApplyFreeze(freezeDuration);
                }
            }

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Border") || other.CompareTag("EnemyLaneLimit"))
            Destroy(gameObject);
    }
}
