// PlayerFireballShooter.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFireballShooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;                 // точка выстрела (чуть выше головы)
    public GameObject playerFireballPrefab;     // префаб с PlayerFireball + Collider2D (isTrigger)
    public ChargeDotsUI chargeUI;               // скрипт из п.2

    [Header("Charge")]
    public int maxDots = 5;                     // макс. точек (по 1 секунде на точку)
    public float secondsPerDot = 1f;            // каждые N секунд добавляется точка
    public float minDistance = 2.5f;            // дистанция при 1 точке (если не используем зоны)
    public float maxDistance = 9f;              // дистанция при maxDots (если не используем зоны)
    public bool autoReleaseAtMax = false;       // бросок ТОЛЬКО при отпускании ЛКМ (оставь false)

    [Header("Sprites")]
    public Sprite idleSprite;                   // обычный idle (твой базовый)
    public Sprite windupSprite;                 // witch_3_3 (замах/бросок)

    [Header("Direction")]
    public bool shootAlwaysUp = true;           // летит строго вверх (как по лейну)
    public bool useFacingForHorizontal = false; // если false — тоже вверх; если true — вправо/влево по flipX

    [Header("Cooldown")]
    public float fireCooldown = 0.6f;           // задержка между бросками (сек)
    private float _cooldownUntil = 0f;          // время, когда можно снова стрелять

    [Header("Clamp to Enemy Zone")]
    public SpriteRenderer enemyZone;            // перетащи EnemyZone SpriteRenderer
    public float zoneTopPadding = 0.05f;        // небольшой зазор до верхней границы зоны

    [Tooltip("Дробит высоту EnemyZone на равные ступени, и каждая точка = одна ступень")]
    public bool useZoneSteps = true;
    [Min(1)] public int zoneSteps = 5;          // сколько «клеток» по высоте зоны
    [Tooltip("Если включено — целимся в центр ступени (k-0.5)/steps, иначе точно на границу k/steps")]
    public bool snapToCenters = false;          // выключи, чтобы 5-я точка попадала в самый верх

    // внутренние поля
    private SpriteRenderer _sr;
    private Coroutine _chargeRoutine;
    private int _currentDots;
    private bool _isCharging;
    private bool _isMouseHeld;                  // удерживаем ли ЛКМ прямо сейчас

    private PlayerHealth hp;
    private Rigidbody2D rb;
    private bool _lockWindupSpriteWhileCharging = true; // удерживаем кадр замаха во время зарядки

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        hp = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (chargeUI != null) chargeUI.Clear();
        if (idleSprite == null && _sr != null) idleSprite = _sr.sprite; // авто-берём стартовый
        // ВАЖНО: больше НЕ подрезаем maxDots по количеству иконок в UI.
    }

    private void Update()
    {
        if (hp != null && hp.IsDead) return;

        HandleInput();
    }

    public void CancelAllImmediate()
    {
        CancelCharge(false); // без смены спрайта (чтобы не перетирать спрайт смерти)
    }

    private void LateUpdate()
    {
        if (hp != null && hp.IsDead)
        {
            if (_isCharging) CancelCharge(false);
            return;
        }

        if (_isCharging && _lockWindupSpriteWhileCharging && _sr != null && windupSprite != null)
        {
            if (_sr.sprite != windupSprite)
                _sr.sprite = windupSprite;
        }
    }

    private void HandleInput()
    {
        // нажали ЛКМ — начинаем заряд
        if (Input.GetMouseButtonDown(0))
        {
            _isMouseHeld = true;
            StartCharging();
        }

        // отпустили ЛКМ — бросаем
        if (Input.GetMouseButtonUp(0))
        {
            _isMouseHeld = false;
            ReleaseIfCharging();
        }
    }

    // на будущее
    private bool IsStandingNow()
    {
        float axis = Input.GetAxisRaw("Horizontal");
        return Mathf.Abs(axis) < 0.01f;
    }

    private bool IsCooldownReady() => Time.time >= _cooldownUntil;

    private void StartCharging()
    {
        if (hp != null && hp.IsDead) return;
        if (!IsCooldownReady()) return;
        if (_isCharging) return;

        _isCharging = true;

        // сразу зажигаем 1-ю точку
        _currentDots = 1;
        if (chargeUI != null)
        {
            chargeUI.Clear();
            chargeUI.SetCount(_currentDots);
        }

        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        _chargeRoutine = StartCoroutine(ChargeTick());
    }

    private IEnumerator ChargeTick()
    {
        // Копим точки до maxDots; дальше просто ждём отпускания ЛКМ.
        while (_isCharging)
        {
            yield return new WaitForSeconds(secondsPerDot);
            if (!_isCharging) yield break;

            if (_currentDots < maxDots)
            {
                _currentDots++;
                if (chargeUI != null) chargeUI.SetCount(_currentDots);

                // Автосброс только если ТЫ сам включишь флаг в инспекторе
                if (autoReleaseAtMax && _currentDots >= maxDots)
                {
                    // дать UI кадр отрисоваться
                    yield return null;
                    // но всё равно стрелять только если кнопка отпущена
                    if (!_isMouseHeld)
                        ReleaseThrow();
                    // если ЛКМ всё ещё зажата — ждём отпускания; НИКАКОГО авто-выстрела
                }
            }
            // если уже на максимуме — ничего не делаем (ждём отпускания)
        }
    }

    private void ReleaseIfCharging()
    {
        if (!_isCharging) return;

        if (hp != null && hp.IsDead)
        {
            CancelCharge(false);
            return;
        }

        ReleaseThrow();
    }

    private void ReleaseThrow()
    {
        if (!IsCooldownReady())
        {
            CancelCharge();
            return;
        }

        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);

        int dots = Mathf.Clamp(_currentDots, 1, Mathf.Max(1, maxDots));

        float distance;

float ignoreFirstMeters = 0f; // новая переменная

if (shootAlwaysUp && enemyZone != null && useZoneSteps)
{
    // Равные ступени от firePoint до верха EnemyZone
    float top = enemyZone.bounds.max.y - zoneTopPadding;
    float allowed = Mathf.Max(0.05f, top - firePoint.position.y);

    int steps = Mathf.Max(1, zoneSteps);
    float step = allowed / steps;

    int k = Mathf.Clamp(dots, 1, steps);

    // куда целимся
    distance = snapToCenters ? (k - 0.5f) * step : k * step;

    // хотим «перелёт» ближайшего врага: игнорируем примерно ОДНУ ступень ниже целевой
    // - если цель на границе ступени (snapToCenters=false) → игнорируем (k-1)*step
    // - если цель в центре ступени → игнорируем (k-0.5 - 0.5)*step = (k-1)*step (чуть мягче)
    float skip = distance - step; // одна ступень меньше целевой
    // небольшая страховка, чтобы не игнорировать ВСЮ дистанцию
    float bias = 0.02f;
    ignoreFirstMeters = Mathf.Clamp(skip, 0f, Mathf.Max(0f, distance - bias));
}
else
{
    // старая линейка min..max
    float t = (maxDots == 1) ? 1f : (dots - 1) / (float)(maxDots - 1);
    distance = Mathf.Lerp(minDistance, maxDistance, t);

    if (shootAlwaysUp && enemyZone != null)
    {
        float top = enemyZone.bounds.max.y - zoneTopPadding;
        float allowed = Mathf.Max(0.05f, top - firePoint.position.y);
        distance = Mathf.Min(distance, allowed);
    }

    // оценочный "шаг" на одну точку заряда
    float estStep = (maxDots > 1) ? (maxDistance - minDistance) / (maxDots - 1) : minDistance;
    float skip = distance - estStep; // игнорируем одну «ступень» ниже целевой
    float bias = 0.02f;
    ignoreFirstMeters = Mathf.Clamp(skip, 0f, Mathf.Max(0f, distance - bias));
}

// В Ы С Т Р Е Л
if (playerFireballPrefab != null && firePoint != null)
{
    var go = Instantiate(playerFireballPrefab, firePoint.position, Quaternion.identity);
    var pf = go.GetComponent<PlayerFireball>();
    if (pf != null)
    {
        var cam = Camera.main;
        Vector2 dir = Vector2.up; // дефолт

        if (!shootAlwaysUp)
        {
            if (cam != null)
            {
                Vector3 mp = Input.mousePosition;
                float depth = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
                if (depth < cam.nearClipPlane + 0.01f) depth = cam.nearClipPlane + 0.01f;
                mp.z = depth;

                Vector3 mouseWorld = cam.ScreenToWorldPoint(mp);
                mouseWorld.z = firePoint.position.z;

                dir = (mouseWorld - firePoint.position).normalized;
            }
        }
        else if (useFacingForHorizontal && _sr != null)
        {
            dir = _sr.flipX ? Vector2.left : Vector2.right;
        }

        // ⬇️ передаём новую «зону игнора»
        pf.Init(dir, distance, pf.speed, ignoreFirstMeters);
    }
}

        _cooldownUntil = Time.time + fireCooldown;

        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;

        if (_sr != null && idleSprite != null) _sr.sprite = idleSprite;
    }

    private void CancelCharge(bool changeSprite = true)
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;

        if (changeSprite && _sr != null && idleSprite != null)
            _sr.sprite = idleSprite;
    }
}
