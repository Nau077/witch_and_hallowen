// Assets/Scripts/Enemy/EnemyWalker.cs
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

    [Header("Keep Distance from Player (X axis) - legacy")]
    public int minCellsFromPlayer = 4;
    public float cellSize = 1f;
    public float softStopDistanceFudge = 0.15f;

    [Header("External Busy (channeling skills)")]
    [Tooltip("Если true — во время внешней 'занятости' (луч/каст) враг всё равно может двигаться.")]
    public bool allowMoveWhileExternallyBusy = false;

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

    // -------------------- NEW: Anti-clump (NO collision matrix changes) --------------------
    [Header("Anti-clump (script only, keeps damage working)")]
    [Tooltip("Включить мягкое 'расталкивание' врагов друг от друга.")]
    public bool enableAntiClump = true;

    [Tooltip("С какого расстояния начинаем раздвигать (в клетках).")]
    public float clumpStartDistanceCells = 0.85f;

    [Tooltip("На каком расстоянии считаем, что уже 'разошлись' (в клетках). Должно быть чуть меньше start.")]
    public float clumpStopDistanceCells = 0.70f;

    [Tooltip("Сколько секунд держим выбранное направление расталкивания (чтобы не дёргалось).")]
    public float clumpStickTime = 0.20f;

    [Tooltip("Кулдаун между пересчётами расталкивания (сек).")]
    public float clumpRecalcCooldown = 0.12f;

    [Tooltip("Насколько сильно добавлять push к текущему движению (0..1.5).")]
    public float clumpPushWeight = 1.0f;

    [Tooltip("Радиус поиска соседей (в клетках).")]
    public float clumpSearchRadiusCells = 1.35f;

    [Tooltip("Сколько коллайдеров максимум проверяем.")]
    public int clumpMaxHits = 24;

    // внутреннее состояние анти-скучивания
    private Vector2 _pushDir = Vector2.zero;
    private float _pushUntilTime = 0f;
    private float _nextClumpRecalcTime = 0f;
    private Collider2D[] _clumpHits;
    // ----------------------------------------------------------------------

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
    private Coroutine _attackRoutine;

    private bool _wasFrozen;
    private bool _wasStaggered;

    // -------- External busy (for channel skills) --------
    private float _externalBusyUntil;
    public bool IsExternallyBusy => Time.time < _externalBusyUntil;

    public void SetExternalBusy(float duration)
    {
        _externalBusyUntil = Mathf.Max(_externalBusyUntil, Time.time + Mathf.Max(0f, duration));
    }

    public void ClearExternalBusy()
    {
        _externalBusyUntil = 0f;
    }
    // ---------------------------------------------------

    // -------- Movement Brain (optional SOLID extension) --------
    private EnemyMoveBrainBase moveBrain;
    // ----------------------------------------------------------

    public int AttackIndex => attackIndex;
    public bool PlayerIsDead => playerHP != null && playerHP.IsDead;
    public Transform PlayerTransform => player;

    public bool IsBusyAttacking => isAttacking || isHoldingForAttack || IsExternallyBusy;

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

        // NEW
        int n = Mathf.Clamp(clumpMaxHits, 4, 64);
        _clumpHits = new Collider2D[n];
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
                if (s != null) s.Init(this);
        }

        // optional movement brain
        moveBrain = GetComponent<EnemyMoveBrainBase>();
        if (moveBrain != null)
            moveBrain.Init(this);

        StartCoroutine(DecideLoop());
    }

    private void Update()
    {
        if ((selfHP && selfHP.IsDead) || (playerHP && playerHP.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        bool frozenNow = selfHP && selfHP.IsFrozen;
        bool staggerNow = selfHP && selfHP.IsStaggered;

        // вошли во freeze/stagger -> немедленно прерываем атаку/скиллы
        if ((frozenNow && !_wasFrozen) || (staggerNow && !_wasStaggered))
        {
            InterruptAttackAndSkills();
        }

        // вышли из freeze/stagger -> форсим новое решение, чтобы не "залип"
        if ((!frozenNow && _wasFrozen) || (!staggerNow && _wasStaggered))
        {
            if (moveBrain == null) ForceNewDecisionDirection();
        }

        _wasFrozen = frozenNow;
        _wasStaggered = staggerNow;

        // пока freeze/stagger — стоим и не атакуем
        if (frozenNow || staggerNow)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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

        // если идёт стандартная атака — стоп
        if (isAttacking || isHoldingForAttack)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // внешняя занятость (луч/каст): блокируем новые атаки,
        // но движение разрешаем, если allowMoveWhileExternallyBusy = true
        if (IsExternallyBusy && !allowMoveWhileExternallyBusy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // -------- choose movement direction --------
        if (moveBrain != null)
        {
            desiredDir = moveBrain.GetDesiredMoveDir();
        }
        else
        {
            // legacy keep distance
            if (player)
            {
                float minDistX = minCellsFromPlayer * cellSize;
                float dx = Mathf.Abs(player.position.x - transform.position.x);
                if (minCellsFromPlayer > 0 && dx <= minDistX + softStopDistanceFudge)
                {
                    float signAway = Mathf.Sign(transform.position.x - player.position.x);
                    desiredDir = new Vector2(signAway, 0f);
                }
            }
        }
        // ------------------------------------------

        // атаки запускаем, только если не externally busy
        if (!IsExternallyBusy)
            HandleAttackTimer();
    }

    private void FixedUpdate()
    {
        if ((selfHP && selfHP.IsDead) || (playerHP && playerHP.IsDead)) return;

        if (selfHP && (selfHP.IsFrozen || selfHP.IsStaggered))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isAttacking || isHoldingForAttack) return;

        if (IsExternallyBusy && !allowMoveWhileExternallyBusy)
            return;

        Vector2 cur = rb.position;

        // NEW: анти-скучивание (НЕ трогаем коллизии, только добавляем push-dir)
        Vector2 push = Vector2.zero;
        if (enableAntiClump)
        {
            UpdateClumpPush(cur);
            if (Time.time < _pushUntilTime)
                push = _pushDir;
            else
                _pushDir = Vector2.zero;
        }

        // складываем направления так, чтобы не ломать твою логику маршрута
        Vector2 finalDir = desiredDir;

        if (push != Vector2.zero)
        {
            // чтобы не было "кружений": push строго осевой
            Vector2 weightedPush = push * Mathf.Max(0f, clumpPushWeight);
            finalDir = finalDir + weightedPush;
        }

        if (finalDir.sqrMagnitude > 0.000001f)
            finalDir = finalDir.normalized;
        else
            finalDir = Vector2.zero;

        Vector2 next = cur + finalDir * moveSpeed * Time.fixedDeltaTime;
        Vector2 clamped = ClampToBounds(next);

        rb.MovePosition(clamped);

        // ВАЖНО: возвращаем старое поведение — стабильная скорость для анимаций/флипа
        rb.linearVelocity = (clamped - cur) / Time.fixedDeltaTime;

        if (sr)
        {
            float mx = rb.linearVelocity.x;
            if (mx > flipDeadzone) sr.flipX = false;
            else if (mx < -flipDeadzone) sr.flipX = true;
        }
    }

    // -------------------- NEW: anti-clump helpers --------------------
    private void UpdateClumpPush(Vector2 curPos)
    {
        if (Time.time < _nextClumpRecalcTime) return;
        _nextClumpRecalcTime = Time.time + Mathf.Max(0.02f, clumpRecalcCooldown);

        float step = Mathf.Max(0.01f, cellSize);
        float searchR = clumpSearchRadiusCells * step;

        // ищем ближайшего врага рядом (по коллайдерам), фильтруя по EnemyWalker
        int count = Physics2D.OverlapCircleNonAlloc(curPos, searchR, _clumpHits);
        if (count <= 0) return;

        EnemyWalker closest = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            var col = _clumpHits[i];
            if (col == null) continue;

            var other = col.GetComponentInParent<EnemyWalker>();
            if (other == null || other == this) continue;

            Vector2 op = other.rb ? other.rb.position : (Vector2)other.transform.position;
            float sqr = (op - curPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closest = other;
            }
        }

        if (closest == null) return;

        Vector2 otherPos = closest.rb ? closest.rb.position : (Vector2)closest.transform.position;
        float dist = Vector2.Distance(curPos, otherPos);

        float startD = Mathf.Max(0.01f, clumpStartDistanceCells) * step;
        float stopD = Mathf.Max(0.00f, clumpStopDistanceCells) * step;

        // гистерезис: если push уже активен, держим его пока не разойдёмся до stopD
        bool pushActive = Time.time < _pushUntilTime;

        if (!pushActive)
        {
            if (dist >= startD) return; // не близко — ничего
        }
        else
        {
            if (dist >= stopD)
            {
                // уже разошлись — отпускаем
                _pushDir = Vector2.zero;
                _pushUntilTime = 0f;
                return;
            }
        }

        // вычисляем осевое направление "от соседа"
        Vector2 away = curPos - otherPos;

        Vector2 axisDir;
        if (Mathf.Abs(away.x) >= Mathf.Abs(away.y))
            axisDir = new Vector2(Mathf.Sign(away.x == 0 ? 1f : away.x), 0f);
        else
            axisDir = new Vector2(0f, Mathf.Sign(away.y == 0 ? 1f : away.y));

        // если почти в одной точке, выбираем стабильное направление по instanceID (чтобы не дергалось)
        if (away.sqrMagnitude < 0.000001f)
        {
            axisDir = (GetInstanceID() & 1) == 0 ? Vector2.right : Vector2.left;
        }

        // продлеваем “липкость” push’а
        _pushDir = axisDir;
        _pushUntilTime = Time.time + Mathf.Max(0.05f, clumpStickTime);
    }
    // ----------------------------------------------------------------------

    private IEnumerator DecideLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decideEvery);

            if ((selfHP && selfHP.IsDead) || (selfHP && selfHP.IsFrozen) || (selfHP && selfHP.IsStaggered))
                continue;

            if (IsExternallyBusy && !allowMoveWhileExternallyBusy)
                continue;

            if (moveBrain != null)
                moveBrain.OnDecideTick();
            else
                ForceNewDecisionDirection();
        }
    }

    private void ForceNewDecisionDirection()
    {
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

    private void HandleAttackTimer()
    {
        if (!CanAttackNow()) return;
        if (isAttacking || isHoldingForAttack) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;

            if (_attackRoutine != null) StopCoroutine(_attackRoutine);
            _attackRoutine = StartCoroutine(AttackSequence());
        }
    }

    private bool CanAttackNow()
    {
        if (player == null) return false;
        if (selfHP && selfHP.IsDead) return false;
        if (playerHP && playerHP.IsDead) return false;
        if (selfHP && selfHP.IsFrozen) return false;
        if (selfHP && selfHP.IsStaggered) return false;
        return true;
    }

    private IEnumerator AttackSequence()
    {
        if (!CanAttackNow()) { _attackRoutine = null; yield break; }
        if (isAttacking) { _attackRoutine = null; yield break; }
        if (IsExternallyBusy) { _attackRoutine = null; yield break; }

        isAttacking = true;
        isHoldingForAttack = true;
        rb.linearVelocity = Vector2.zero;

        if (preAttackHold > 0f)
            yield return new WaitForSeconds(preAttackHold);

        isHoldingForAttack = false;

        if (!CanAttackNow() || IsExternallyBusy)
        {
            isAttacking = false;
            RestoreSprite();
            _attackRoutine = null;
            yield break;
        }

        // Старое поведение: attackSprite показывается на каждую атаку (совместимость)
        if (sr && attackSprite)
            sr.sprite = attackSprite;

        yield return new WaitForSeconds(0.10f);

        attackIndex++;
        bool attackConsumed = false;

        if (skills != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[i];
                if (s == null || !s.isActiveAndEnabled) continue;

                if (selfHP && (selfHP.IsFrozen || selfHP.IsStaggered))
                {
                    InterruptAttackAndSkills();
                    _attackRoutine = null;
                    yield break;
                }

                s.OnBrainAttackTick(attackIndex, ref attackConsumed);
            }
        }

        if (postAttackHold > 0f)
            yield return new WaitForSeconds(postAttackHold);

        RestoreSprite();
        isAttacking = false;
        _attackRoutine = null;
    }

    private void InterruptAttackAndSkills()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        isAttacking = false;
        isHoldingForAttack = false;

        if (skills != null)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[i];
                if (s == null || !s.isActiveAndEnabled) continue;
                s.OnBrainAttackInterrupted();
            }
        }

        RestoreSprite();
    }

    public void ForceInterruptFromExternalStagger()
    {
        InterruptAttackAndSkills();
        rb.linearVelocity = Vector2.zero;
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

    public Vector2 DebugClampToBounds(Vector2 pos) => ClampToBounds(pos);

    // -------------------- DESYNC SUPPORT (FOR SPAWNER) --------------------
    public void ApplyDesync(float decideJitter, float attackJitter, float firstDecisionDelay, float firstAttackDelay)
    {
        decideJitter = Mathf.Clamp01(decideJitter);
        attackJitter = Mathf.Clamp01(attackJitter);

        decideEvery = decideEvery * Random.Range(1f - decideJitter, 1f + decideJitter);
        attackInterval = attackInterval * Random.Range(1f - attackJitter, 1f + attackJitter);

        // атаки могут стартовать "не сразу"
        attackTimer = -Mathf.Max(0f, firstAttackDelay);

        // решения направления тоже можем сместить
        StartCoroutine(_FirstDecisionDelay(Mathf.Max(0f, firstDecisionDelay)));
    }

    private IEnumerator _FirstDecisionDelay(float t)
    {
        if (t > 0f) yield return new WaitForSeconds(t);
    }
    // ----------------------------------------------------------------------

    public void OnDeathExternal()
    {
        StopAllCoroutines();

        if (TryGetComponent<Rigidbody2D>(out var rb2))
        {
            rb2.linearVelocity = Vector2.zero;
            rb2.angularVelocity = 0f;
        }
    }
}
