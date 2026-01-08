using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWalker : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;

    [Header("Visuals")]
    public Sprite attackSprite;
    public Sprite idleSprite;

    [Header("Movement Settings")]
    public float moveSpeed = 6.6f;
    public float decideEvery = 0.8f;
    public bool snapToGrid = false;
    [SerializeField] private float flipDeadzone = 0.02f;

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
    [SerializeField] private bool enforceGlobalBounds = true;

    private const float GLOBAL_LEFT = -9.0f;
    private const float GLOBAL_RIGHT = 8.5f;
    private const float GLOBAL_TOP = 3.4f;
    private const float GLOBAL_BOTTOM = -2.76f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 desiredDir = Vector2.zero;

    private bool isAttacking = false;
    private bool isHoldingForAttack = false;
    private float attackTimer;
    private int attackIndex;

    private Sprite baseSprite;

    private PlayerHealth playerHP;
    private EnemyHealth selfHP;

    private EnemySkillBase[] skills;

    // ✅ для снарядов: усилить стаггер, когда враг уже атакует/холдит
    public bool IsBusyAttacking => isAttacking || isHoldingForAttack;

    // ✅ чтобы Detect’ить “стаггер начался” и сбрасывать атаку 1 раз
    private bool _wasStaggeredLastFrame = false;

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

        skills = GetComponents<EnemySkillBase>();
        if (skills != null && skills.Length > 0)
        {
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

        if (selfHP && selfHP.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ✅ СТАГГЕР: ощущаемый стоп + сброс атаки
        if (selfHP && selfHP.IsStaggered)
        {
            // если стаггер только что начался — жёстко срываем атаку
            if (!_wasStaggeredLastFrame)
            {
                ForceCancelAttackAndStop();
            }

            _wasStaggeredLastFrame = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }
        _wasStaggeredLastFrame = false;

        // обновление скиллов
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

        // Не подходить к игроку ближе по X
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

        if (selfHP && selfHP.IsFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // ✅ СТАГГЕР: стопим физику движения
        if (selfHP && selfHP.IsStaggered)
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

        if (sr)
        {
            float mx = rb.linearVelocity.x;
            if (mx > flipDeadzone) sr.flipX = false;
            else if (mx < -flipDeadzone) sr.flipX = true;
        }
    }

    private IEnumerator DecideLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decideEvery);

            if ((selfHP && selfHP.IsDead) || (selfHP && selfHP.IsFrozen))
                continue;

            if (selfHP && selfHP.IsStaggered)
                continue;

            int r = Random.Range(0, 4);
            Vector2 dir = r switch
            {
                0 => Vector2.right,
                1 => Vector2.left,
                2 => Vector2.up,
                _ => Vector2.down
            };

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

        // ✅ стаггер блокирует атаку
        if (selfHP && selfHP.IsStaggered) return false;

        return true;
    }

    private IEnumerator AttackSequence()
    {
        if (!CanAttackNow()) yield break;
        if (isAttacking) yield break;

        isAttacking = true;
        isHoldingForAttack = true;
        rb.linearVelocity = Vector2.zero;

        if (preAttackHold > 0f)
            yield return new WaitForSeconds(preAttackHold);

        // ✅ если стаггер случился во время подготовки — срываем атаку
        if (!CanAttackNow())
        {
            isHoldingForAttack = false;
            isAttacking = false;
            RestoreSprite();
            yield break;
        }

        isHoldingForAttack = false;

        if (sr && attackSprite)
            sr.sprite = attackSprite;

        yield return new WaitForSeconds(0.25f);

        // ✅ если стаггер случился перед выпуском — срываем атаку
        if (!CanAttackNow())
        {
            isAttacking = false;
            RestoreSprite();
            yield break;
        }

        attackIndex++;
        bool attackConsumed = false;

        if (skills != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[i];
                if (s == null || !s.isActiveAndEnabled) continue;

                // на всякий — ещё раз (если стаггер в середине кадра)
                if (!CanAttackNow()) break;

                s.OnBrainAttackTick(attackIndex, ref attackConsumed);
            }
        }

        if (postAttackHold > 0f)
            yield return new WaitForSeconds(postAttackHold);

        RestoreSprite();
        isAttacking = false;
    }

    private void ForceCancelAttackAndStop()
    {
        // стопим движение “на месте”
        desiredDir = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // сбрасываем атаку
        isHoldingForAttack = false;
        isAttacking = false;

        // чтобы враг не “моментально” снова атаковал сразу после стаггера
        attackTimer = 0f;

        RestoreSprite();
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

    public void OnDeathExternal()
    {
        StopAllCoroutines();

        if (TryGetComponent<Rigidbody2D>(out var rb2))
        {
            rb2.linearVelocity = Vector2.zero;
            rb2.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// Десинхронизирует движения и атаки (рандомные сдвиги),
    /// используется спавнерами / волнами.
    /// </summary>
    public void ApplyDesync(
        float decideJitter,
        float attackJitter,
        float firstDecisionDelay,
        float firstAttackDelay
    )
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
        if (t > 0f)
            yield return new WaitForSeconds(t);
    }
}
