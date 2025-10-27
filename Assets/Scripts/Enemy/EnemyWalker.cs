using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWalker : MonoBehaviour
{
    [Header("Refs")]
    public Transform player; // цель (игрок)
    public GameObject fireballPrefab;
    public Transform firePoint;
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
    public float bottomLimit = -2.76f;

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
    private Sprite baseSprite;

    private PlayerHealth playerHP;
    private EnemyHealth selfHP;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr) baseSprite = sr.sprite;

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
        if (player) playerHP = player.GetComponent<PlayerHealth>();
        selfHP = GetComponent<EnemyHealth>();
        StartCoroutine(DecideLoop());
    }

    private void Update()
    {
        if ((selfHP && selfHP.IsDead) || (playerHP && playerHP.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
            return;
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
    }

    private void FixedUpdate()
    {
        if ((selfHP && selfHP.IsDead) || (playerHP && playerHP.IsDead)) return;
        if (isAttacking || isHoldingForAttack) return;

        Vector2 cur = rb.position;
        Vector2 next = cur + desiredDir * moveSpeed * Time.fixedDeltaTime;
        Vector2 clamped = ClampToBounds(next);

        rb.MovePosition(clamped);
        rb.linearVelocity = (clamped - cur) / Time.fixedDeltaTime;

        // === ФЛИП ПО ФАКТИЧЕСКОМУ ДВИЖЕНИЮ ===
        if (sr)
        {
            float mx = rb.linearVelocity.x;
            if (mx > flipDeadzone) sr.flipX = false; // дефолт: лицом вправо
            else if (mx < -flipDeadzone) sr.flipX = true;  // идём влево — отражаем по X
            // при |mx| ~ 0 — оставляем предыдущий флип
        }
    }

    private IEnumerator DecideLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decideEvery);

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

            // Флип здесь больше не трогаем — он делается по реальной скорости в FixedUpdate().
        }
    }

    private void LateUpdate()
    {
        HandleAttackTimer();
    }

    private void HandleAttackTimer()
    {
        if (!fireballPrefab || !firePoint || !player) return;
        if ((playerHP && playerHP.IsDead) || (selfHP && selfHP.IsDead)) return;
        if (isAttacking || isHoldingForAttack) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(preAttackHold);

        if ((selfHP && selfHP.IsDead) || (playerHP && playerHP.IsDead))
        { isAttacking = false; yield break; }

        if (sr && attackSprite) sr.sprite = attackSprite;
        yield return new WaitForSeconds(0.25f);

        Vector2 toPlayer = player.position - transform.position;
        Vector2 dir = (Mathf.Abs(toPlayer.x) < 0.5f) ? Vector2.down : toPlayer.normalized;

        GameObject fb = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        var fireball = fb.GetComponent<Fireball>();
        if (fireball) fireball.Init(dir);

        yield return new WaitForSeconds(postAttackHold);

        if (sr && baseSprite) sr.sprite = baseSprite;
        isAttacking = false;
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
