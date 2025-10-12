using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWalker : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer zoneRenderer; // перетащи сюда EnemyZone
    public Transform player;            // перетащи сюда Player

    [Header("Grid / Movement")]
    public float cellSize = 1f;
    public float moveSpeed = 1.6f;
    public float decideEvery = 1.8f;
    public bool snapToGrid = true;

    [Header("Keep Distance")]
    public int minCellsFromPlayer = 4;
    public float softStopDistanceFudge = 0.15f;

    private Rigidbody2D rb;
    private Vector2 desiredDir = Vector2.zero;
    private Bounds zoneBounds;

    [Header("Attack Settings")]
    public GameObject fireballPrefab;   // перетащи сюда prefab фаербола
    public Transform firePoint;         // перетащи сюда пустой объект FirePoint
    public float attackInterval = 3f;   // каждые 3 секунды стреляет
    public Sprite attackSprite;         // перетащи сюда Enemy1_0 (спрайт атаки)

    [Header("Attack Timing")]
    public float preAttackHold = 0.6f;   // пауза перед любой атакой (замах)
    public float postAttackHold = 0.25f; // пауза после броска

    // внутреннее состояние
    private bool isAttacking = false;        // атака выполняется прямо сейчас
    private bool isHoldingForAttack = false; // “режим у стены”
    private float attackTimer;
    private Sprite idleSpriteBase;           // исходный idle-спрайт

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        CacheZoneBounds();
        StartCoroutine(DecideLoop());
        // сохраним настоящий idle-спрайт один раз — сюда всегда вернёмся после атаки
        var sr = GetComponent<SpriteRenderer>();
        if (sr) idleSpriteBase = sr.sprite;
    }

    private void Update()
    {
        if (zoneRenderer) zoneBounds = zoneRenderer.bounds;

        // если сейчас атакуем или держим паузу — стоим
        if (isAttacking || isHoldingForAttack)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // держим горизонтальную дистанцию до игрока
        if (player)
        {
            float minDist = minCellsFromPlayer * cellSize;
            Vector2 toPlayer = (player.position - transform.position);
            float dist = toPlayer.magnitude;
            if (dist <= minDist + softStopDistanceFudge)
            {
                desiredDir = new Vector2(Mathf.Sign(-toPlayer.x), 0f);
            }
        }

        // если следующий клеточный шаг в стену — останавливаемся и атакуем с переориентацией
        if (WillHitWall(desiredDir))
        {
            if (!isHoldingForAttack && !isAttacking)
                StartCoroutine(HitWallAndRedirect());
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = desiredDir * moveSpeed;
        }

        // жёсткий кламп внутри зоны (с учётом габаритов спрайта)
        Vector3 p = transform.position;
        float halfW = GetComponent<SpriteRenderer>().bounds.extents.x;
        float halfH = GetComponent<SpriteRenderer>().bounds.extents.y;
        p.x = Mathf.Clamp(p.x, zoneBounds.min.x + halfW, zoneBounds.max.x - halfW);
        p.y = Mathf.Clamp(p.y, zoneBounds.min.y + halfH, zoneBounds.max.y - halfH);
        transform.position = p;
    }

    private IEnumerator DecideLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(decideEvery);

            int r = Random.Range(0, 6);
            Vector2 dir;
            if (r <= 2) dir = Vector2.right;
            else if (r <= 4) dir = Vector2.left;
            else dir = Vector2.up;

            if (snapToGrid)
            {
                Vector2 target = (Vector2)transform.position + dir * cellSize;
                target.x = Mathf.Clamp(target.x, zoneBounds.min.x, zoneBounds.max.x);
                target.y = Mathf.Clamp(target.y, zoneBounds.min.y, zoneBounds.max.y);

                if (Vector2.Distance(target, transform.position) < 0.05f)
                    dir = -dir;

                // если сразу ведёт в стену — развернёмся
                if (WillHitWall(dir)) dir = -dir;
            }

            desiredDir = dir;

            var sr = GetComponent<SpriteRenderer>();
            if (sr && Mathf.Abs(desiredDir.x) > 0.01f)
                sr.flipX = desiredDir.x < 0;
        }
    }

    private void LateUpdate()
    {
        HandleAttackTimer();
    }

    private void HandleAttackTimer()
    {
        if (fireballPrefab == null || firePoint == null || player == null) return;

        // не копим таймер, если заняты атакой/холдом
        if (isAttacking || isHoldingForAttack) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            StartAttack(); // централизованный запуск
        }
    }

    // централизованный запуск атаки с защёлкой
    private void StartAttack()
    {
        if (isAttacking) return; // уже атакуем — вторую не начинаем
        StartCoroutine(PerformAttack());
    }

    private IEnumerator HitWallAndRedirect()
    {
        isHoldingForAttack = true;

        // атака у стены (StartAttack сам проверит флаги)
        if (!isAttacking) StartAttack();
        // ждём завершения текущей атаки
        while (isAttacking) yield return null;

        // выбираем новое направление, чтобы не тереться об стену
        desiredDir = ChooseDirectionAwayFromWall();

        isHoldingForAttack = false;
    }

    private IEnumerator PerformAttack()
    {
        // защёлка “атака началась”
        isAttacking = true;
        attackTimer = 0f; // сбросим таймер, чтобы в момент атаки не стартовала ещё одна
        rb.linearVelocity = Vector2.zero;

        // замах
        yield return new WaitForSeconds(preAttackHold);

        var sr = GetComponent<SpriteRenderer>();

        // кадр атаки
        if (attackSprite && sr) sr.sprite = attackSprite;

        // короткая задержка “броска”
        yield return new WaitForSeconds(0.25f);

        // направление
        Vector2 toPlayer = player.position - transform.position;
        Vector2 dir;
        float xDiff = Mathf.Abs(toPlayer.x);
        if (xDiff < 0.5f)
            dir = Vector2.down;      // строго вниз, если герой почти под магом
        else
            dir = toPlayer.normalized; // по касательной

        // фаербол
        GameObject fb = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        var fireball = fb.GetComponent<Fireball>();
        if (fireball != null) fireball.Init(dir);

        // “передышка” после броска
        yield return new WaitForSeconds(postAttackHold);

        // вернуть idle-кадр ГАРАНТИРОВАННО
        if (sr && idleSpriteBase) sr.sprite = idleSpriteBase;

        isAttacking = false;
    }

    private bool WillHitWall(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;
        Vector2 next = (Vector2)transform.position + dir * cellSize;
        float halfW = GetComponent<SpriteRenderer>().bounds.extents.x;
        float halfH = GetComponent<SpriteRenderer>().bounds.extents.y;
        return next.x - halfW < zoneBounds.min.x ||
               next.x + halfW > zoneBounds.max.x ||
               next.y - halfH < zoneBounds.min.y ||
               next.y + halfH > zoneBounds.max.y;
    }

    private Vector2 ChooseDirectionAwayFromWall()
    {
        bool left = transform.position.x <= zoneBounds.min.x + 0.1f;
        bool right = transform.position.x >= zoneBounds.max.x - 0.1f;
        bool top = transform.position.y >= zoneBounds.max.y - 0.1f;
        bool bottom = transform.position.y <= zoneBounds.min.y + 0.1f;

        if (left || right) return Random.value < 0.5f ? Vector2.up : Vector2.down;
        if (top || bottom) return Random.value < 0.5f ? Vector2.left : Vector2.right;

        int r = Random.Range(0, 4);
        return r switch
        {
            0 => Vector2.left,
            1 => Vector2.right,
            2 => Vector2.up,
            _ => Vector2.down
        };
    }

    private void CacheZoneBounds()
    {
        if (zoneRenderer == null)
        {
            Debug.LogError("EnemyWalker: zoneRenderer не задан. Перетащи сюда EnemyZone SpriteRenderer.");
            return;
        }
        zoneBounds = zoneRenderer.bounds;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player)
        {
            Gizmos.color = Color.green;
            float r = (minCellsFromPlayer * cellSize);
            Gizmos.DrawWireSphere(player.position, r);
        }

        if (zoneRenderer)
        {
            var b = zoneRenderer.bounds;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
#endif
}
