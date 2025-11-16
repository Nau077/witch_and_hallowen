using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWalker : MonoBehaviour
{
    [Header("Refs")]
    public Transform player; // цель (игрок)

    [Header("Visuals")]
    public Sprite attackSprite;
    public Sprite idleSprite;

    [Header("Movement Settings")]
    public float moveSpeed = 6.6f;
    public float decideEvery = 0.8f;
    public bool snapToGrid = false;
    [SerializeField] private float flipDeadzone = 0.02f; // чтобы не дёргалось при почти нулевой скорости

    [Header("Movement Bounds (World Coordinates)")]
    public float leftLimit = -9.0f;
    public float rightLimit = 9f;
    public float topLimit = 2.5f;
    public float bottomLimit = -1.76f;

    [Header("Keep Distance from Player (X axis)")]
    public int minCellsFromPlayer = 4;
    public float cellSize = 1f;
    public float softStopDistanceFudge = 0.15f;

    [Header("Attack")]
    public float attackInterval = 1.2f;
    public float preAttackHold = 0.6f;
    public float postAttackHold = 0.25f;

    [Header("Safety")]
    [SerializeField] private bool enforceGlobalBounds = true; // чтобы не менялись границы самопроизвольно

    private const float GLOBAL_LEFT = -9.0f;
    private const float GLOBAL_RIGHT = 8.5f;
    private const float GLOBAL_TOP = 3.4f;
    private const float GLOBAL_BOTTOM = -2.76f;

    // внутренние переменные
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 desiredDir = Vector2.zero;

    private bool isAttacking = false;
    private bool isHoldingForAttack = false;
    private float attackTimer;
    private int attackIndex; // глобальный счётчик атак

    private Sprite baseSprite;

    private PlayerHealth playerHP;
    private EnemyHealth selfHP;

    private EnemySkillBase[] skills;

    // публичные вещи, которые могут смотреть скиллы
    public int AttackIndex => attackIndex;
    public bool PlayerIsDead => playerHP != null && playerHP.IsDead;
    public Transform PlayerTransform => player;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseSprite = sr.sprite;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // жёстко фиксируем границы (если включено)
        if (enforceGlobalBounds)
        {
            leftLimit = GLOBAL_LEFT;
            rightLimit = GLOBAL_RIGHT;
            topLimit = GLOBAL_TOP;
            bottomLimit = GLOBAL_BOTTOM;
        }
    }

    private void Start()
    {
        if (player != null) playerHP = player.GetComponent<PlayerHealth>();
        selfHP = GetComponent<EnemyHealth>();

        // Собираем и инициализируем все скиллы на этом объекте
        skills = GetComponents<EnemySkillBase>();
        if (skills != null && skills.Length > 0)
        {
            // сортируем по приоритету (больший приоритет ходит первым)
            System.Array.Sort(skills, (a, b) => b.Priority.CompareTo(a.Priority));
            foreach (var s in skills)
            {
                if (s != null) s.Init(this);
            }
        }

        StartCoroutine(DecideLoop());
    }

    private void Update()
    {
        if ((selfHP && selfHP.IsDead) || (playerHP && playerHP.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // если заморожен — ничего не делаем
        if (selfHP && selfHP.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // обновление скиллов по времени
        float dt = Time.deltaTime;
        if (skills != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[i];
                if (s != null && s.isActiveAndEnabled)
                    s.OnBrainUpdateSkill(dt);
            }
        }

        if (isAttacking || isHoldingForAttack)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Не подходить к игроку ближе по оси X
        if (player)
        {
            float minDistX = minCellsFromPlayer * cellSize;
            float dx = Mathf.Abs(player.position.x - transform.position.x);
            if (dx <= minDistX + softStopDistanceFudge)
            {
                float signAway = Mathf.Sign(transform.position.x - player.position.x);
                desiredDir = new Vector2(signAway, 0f);
            }
        }

        HandleAttackTimer();
    }

    private void FixedUpdate()
    {
        if ((selfHP && selfHP.IsDead) || (playerHP && playerHP.IsDead)) return;

        // заморожен => вообще не двигаемся
        if (selfHP && selfHP.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isAttacking || isHoldingForAttack) return;

        Vector2 cur = rb.position;
        Vector2 next = cur + desiredDir * moveSpeed * Time.fixedDeltaTime;
        Vector2 clamped = ClampToBounds(next);

        rb.MovePosition(clamped);
        rb.linearVelocity = (clamped - cur) / Time.fixedDeltaTime;

        // флип по фактическому движению
        if (sr)
        {
            float mx = rb.linearVelocity.x;
            if (mx > flipDeadzone) sr.flipX = false; // лицом вправо
            else if (mx < -flipDeadzone) sr.flipX = true; // лицом влево
        }
    }

    private IEnumerator DecideLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decideEvery);

            // если враг умер или заморожен — не пересчитываем направление
            if ((selfHP && selfHP.IsDead) || (selfHP && selfHP.IsFrozen))
                continue;

            int r = Random.Range(0, 4);
            Vector2 dir = r switch
            {
                0 => Vector2.right,
                1 => Vector2.left,
                2 => Vector2.up,
                _ => Vector2.down
            };

            // коррекция, если у краёв по Y
            float y = transform.position.y;
            const float edgeBias = 0.2f;
            if (y > topLimit - edgeBias) dir = Vector2.down;
            else if (y < bottomLimit + edgeBias) dir = Vector2.up;

            desiredDir = dir;

            if (snapToGrid)
            {
                float step = cellSize;
                Vector2 target = (Vector2)transform.position + dir * step;
                target = ClampToBounds(target);
                Vector2 d = target - (Vector2)transform.position;
                desiredDir = d.sqrMagnitude > 0.000001f ? d.normalized : Vector2.zero;
            }
        }
    }

    private void HandleAttackTimer()
    {
        if (!CanAttackNow()) return;
        if (isAttacking || isHoldingForAttack) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            StartCoroutine(AttackSequence());
        }
    }

    private bool CanAttackNow()
    {
        if (player == null) return false;
        if (selfHP && selfHP.IsDead) return false;
        if (playerHP && playerHP.IsDead) return false;
        if (selfHP && selfHP.IsFrozen) return false;
        return true;
    }

    private IEnumerator AttackSequence()
    {
        if (!CanAttackNow()) yield break;
        if (isAttacking) yield break;

        isAttacking = true;
        isHoldingForAttack = true;
        rb.linearVelocity = Vector2.zero;

        // задержка перед атакой
        if (preAttackHold > 0f)
            yield return new WaitForSeconds(preAttackHold);

        isHoldingForAttack = false;

        if (!CanAttackNow())
        {
            isAttacking = false;
            RestoreSprite();
            yield break;
        }

        // включаем спрайт атаки
        if (sr && attackSprite)
            sr.sprite = attackSprite;

        // короткий «кадр броска» как раньше
        yield return new WaitForSeconds(0.25f);

        if (!CanAttackNow())
        {
            isAttacking = false;
            RestoreSprite();
            yield break;
        }

        // глобальный счётчик атак
        attackIndex++;
        bool attackConsumed = false;

        // даём шанс всем скиллам по очереди
        if (skills != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[i];
                if (s == null || !s.isActiveAndEnabled) continue;
                s.OnBrainAttackTick(attackIndex, ref attackConsumed);
            }
        }

        // пост-задержка
        if (postAttackHold > 0f)
            yield return new WaitForSeconds(postAttackHold);

        RestoreSprite();
        isAttacking = false;
    }

    private void RestoreSprite()
    {
        if (!sr) return;

        if (idleSprite)
            sr.sprite = idleSprite;
        else if (baseSprite)
            sr.sprite = baseSprite;
    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        float x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        float y = Mathf.Clamp(pos.y, bottomLimit, topLimit);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Десинхронизирует движения и атаки (рандомные сдвиги).
    /// </summary>
    public void ApplyDesync(float decideJitter, float attackJitter, float firstDecisionDelay, float firstAttackDelay)
    {
        decideJitter = Mathf.Clamp01(decideJitter);
        attackJitter = Mathf.Clamp01(attackJitter);

        decideEvery = decideEvery * Random.Range(1f - decideJitter, 1f + decideJitter);
        attackInterval = attackInterval * Random.Range(1f - attackJitter, 1f + attackJitter);

        attackTimer = -Mathf.Max(0f, firstAttackDelay);
        StartCoroutine(_FirstDecisionDelay(Mathf.Max(0f, firstDecisionDelay)));
    }

    private IEnumerator _FirstDecisionDelay(float t)
    {
        if (t > 0f) yield return new WaitForSeconds(t);
    }

    public void OnDeathExternal()
    {
        StopAllCoroutines();

        if (TryGetComponent<Rigidbody2D>(out var rb2))
        {
            rb2.linearVelocity = Vector2.zero;
            rb2.angularVelocity = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 c = new Vector3((leftLimit + rightLimit) / 2f, (bottomLimit + topLimit) / 2f, 0f);
        Vector3 s = new Vector3(Mathf.Abs(rightLimit - leftLimit), Mathf.Abs(topLimit - bottomLimit), 0f);
        Gizmos.DrawWireCube(c, s);

        if (player)
        {
            float minDistX = minCellsFromPlayer * cellSize;
            Vector3 p = player.position;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(p.x - minDistX, bottomLimit, 0), new Vector3(p.x - minDistX, topLimit, 0));
            Gizmos.DrawLine(new Vector3(p.x + minDistX, bottomLimit, 0), new Vector3(p.x + minDistX, topLimit, 0));
        }
    }
#endif
}
