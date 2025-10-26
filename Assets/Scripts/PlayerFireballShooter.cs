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
    public int maxDots = 3;                     // макс. точек (3 = по 1 секунде на точку)
    public float secondsPerDot = 1f;            // каждые N секунд добавляется точка
    public float minDistance = 2.5f;            // дистанция при 1 точке
    public float maxDistance = 9f;              // дистанция при maxDots
    public bool autoReleaseAtMax = true;        // автоматически бросать на полном заряде

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

    [Tooltip("Делит расстояние до верха EnemyZone на равные доли по числу точек")]
    public bool equalStepsToZone = true;

    // внутренние поля
    private SpriteRenderer _sr;
    private Coroutine _chargeRoutine;
    private int _currentDots;
    private bool _isCharging;

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
    }

    private void Update()
    {
        // нельзя атаковать, если персонаж мёртв
        if (hp != null && hp.IsDead) return;

        HandleInput();
    }

    // Публичный быстрый сброс (без смены спрайта — важно для смерти)
    public void CancelAllImmediate()
    {
        CancelCharge(false); // ⬅️ не возвращаем idle, чтобы не перетирать deadSprite
    }

    private void LateUpdate()
    {
        // если умерли — не держим кадр замаха и мягко сбрасываем заряд без смены спрайта
        if (hp != null && hp.IsDead)
        {
            if (_isCharging) CancelCharge(false);
            return;
        }

        // во время зарядки, даже если ведьма двигается — насильно держим кадр замаха
        if (_isCharging && _lockWindupSpriteWhileCharging && _sr != null && windupSprite != null)
        {
            if (_sr.sprite != windupSprite)
                _sr.sprite = windupSprite;
        }
    }

    private void HandleInput()
    {
        // нажали ЛКМ — начинаем заряд (движение не мешает), кулдаун проверяем
        if (Input.GetMouseButtonDown(0))
        {
            StartCharging();
        }

        // отпустили ЛКМ — бросаем (движение разрешено)
        if (Input.GetMouseButtonUp(0))
        {
            ReleaseIfCharging();
        }
    }

    // (оставлено на будущее) проверка: ведьма сейчас не нажимает стрелки / A-D
    private bool IsStandingNow()
    {
        float axis = Input.GetAxisRaw("Horizontal");
        return Mathf.Abs(axis) < 0.01f;
    }

    // проверка кулдауна
    private bool IsCooldownReady() => Time.time >= _cooldownUntil;

    private void StartCharging()
    {
        if (hp != null && hp.IsDead) return;
        if (!IsCooldownReady()) return; // кулдаун всё ещё в силе
        if (_isCharging) return;

        _isCharging = true;

        // СРАЗУ зажигаем 1-ю точку
        _currentDots = 1;
        if (chargeUI != null)
        {
            chargeUI.Clear();
            chargeUI.SetCount(_currentDots);
        }

        // переключаем спрайт на witch_3_3 (замах)
        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        _chargeRoutine = StartCoroutine(ChargeTick());
    }

    private IEnumerator ChargeTick()
    {
        // каждые secondsPerDot добавляем точку, максимум maxDots
        while (_isCharging && _currentDots < maxDots)
        {
            yield return new WaitForSeconds(secondsPerDot);

            _currentDots++;
            if (chargeUI != null) chargeUI.SetCount(_currentDots);

            if (autoReleaseAtMax && _currentDots >= maxDots)
            {
                // Дадим UI один кадр отрисоваться, чтобы 3-й огонёк точно «засветился»
                yield return null;
                ReleaseThrow();
                yield break;
            }
        }
    }

    private void ReleaseIfCharging()
    {
        if (!_isCharging) return;

        // если умерли во время зарядки — просто сбросить без смены спрайта на idle
        if (hp != null && hp.IsDead)
        {
            CancelCharge(false);
            return;
        }

        ReleaseThrow();
    }

    private void ReleaseThrow()
    {
        // кулдаун: если не готов — сбрасываем заряд
        if (!IsCooldownReady())
        {
            CancelCharge(); // здесь можно вернуть idle
            return;
        }

        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);

        int dots = Mathf.Clamp(_currentDots, 1, Mathf.Max(1, maxDots));

        float distance;
        if (shootAlwaysUp && enemyZone != null && equalStepsToZone)
        {
            // Равные доли до верхней границы зоны
            float top = enemyZone.bounds.max.y - zoneTopPadding;
            float allowed = Mathf.Max(0.05f, top - firePoint.position.y);
            distance = allowed * (dots / (float)maxDots);   // 1/3, 2/3, 3/3
        }
        else
        {
            // старая линейка min..max
            float t = (maxDots == 1) ? 1f : (dots - 1) / (float)(maxDots - 1); // 1 точка = min, maxDots = max
            distance = Mathf.Lerp(minDistance, maxDistance, t);

            // и всё ещё режем по зоне, если нужно
            if (shootAlwaysUp && enemyZone != null)
            {
                float top = enemyZone.bounds.max.y - zoneTopPadding;
                float allowed = Mathf.Max(0.05f, top - firePoint.position.y);
                distance = Mathf.Min(distance, allowed);
            }
        }

        // выстрел
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
                        // расстояние от камеры до плоскости выстрела (firePoint)
                        float depth = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
                        if (depth < cam.nearClipPlane + 0.01f) depth = cam.nearClipPlane + 0.01f;

                        mp.z = depth; // ВАЖНО: z должен быть > nearClipPlane
                        Vector3 mouseWorld = cam.ScreenToWorldPoint(mp);
                        mouseWorld.z = firePoint.position.z;

                        dir = (mouseWorld - firePoint.position).normalized;
                    }
                }
                else if (useFacingForHorizontal && _sr != null)
                {
                    dir = _sr.flipX ? Vector2.left : Vector2.right;
                }

                pf.Init(dir, distance, pf.speed); // lifetime подстроится под distance
            }
        }

        // запускаем кулдаун
        _cooldownUntil = Time.time + fireCooldown;

        // сброс UI + возврат idle
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;

        if (_sr != null && idleSprite != null) _sr.sprite = idleSprite;
    }

    // общий метод для сброса зарядки (без выстрела)
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
