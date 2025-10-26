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
    public float secondsPerDot = 1f;            // каждые N секунд добавл€етс€ точка
    public float minDistance = 2.5f;            // дистанци€ при 1 точке
    public float maxDistance = 9f;              // дистанци€ при maxDots
    public bool autoReleaseAtMax = true;        // автоматически бросать на полном зар€де

    [Header("Sprites")]
    public Sprite idleSprite;                   // обычный idle (твой базовый)
    public Sprite windupSprite;                 // witch_3_3 (замах/бросок)

    [Header("Direction")]
    public bool shootAlwaysUp = true;           // летит строго вверх (как по лейну)
    public bool useFacingForHorizontal = false; // если false Ч тоже вверх; если true Ч вправо/влево по flipX

    [Header("Cooldown")]
    public float fireCooldown = 0.6f;           // задержка между бросками (сек)
    private float _cooldownUntil = 0f;          // врем€, когда можно снова стрел€ть

    [Header("Clamp to Enemy Zone")]
    public SpriteRenderer enemyZone;            // перетащи EnemyZone SpriteRenderer
    public float zoneTopPadding = 0.05f;        // небольшой зазор до верхней границы зоны

    // внутренние пол€
    private SpriteRenderer _sr;
    private Coroutine _chargeRoutine;
    private int _currentDots;
    private bool _isCharging;

    private PlayerHealth hp;
    private Rigidbody2D rb;
    private bool _lockWindupSpriteWhileCharging = true; // удерживаем кадр замаха во врем€ зар€дки

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        hp = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (chargeUI != null) chargeUI.Clear();
        if (idleSprite == null && _sr != null) idleSprite = _sr.sprite; // авто-берЄм стартовый
    }

    private void Update()
    {
        // нельз€ атаковать, если персонаж мЄртв
        if (hp != null && hp.IsDead) return;

        HandleInput();
    }

    private void LateUpdate()
    {
        // во врем€ зар€дки, даже если ведьма двигаетс€ или что-то ещЄ мен€ет спрайт Ч
        // насильно держим кадр замаха
        if (_isCharging && _lockWindupSpriteWhileCharging && _sr != null && windupSprite != null)
        {
            if (_sr.sprite != windupSprite)
                _sr.sprite = windupSprite;
        }
    }

    private void HandleInput()
    {
        // нажали Ћ ћ Ч начинаем зар€д, если кулдаун готов (движение не мешает)
        if (Input.GetMouseButtonDown(0))
        {
            StartCharging();
        }

        // отпустили Ћ ћ Ч бросаем, если стоим и кулдаун готов
        if (Input.GetMouseButtonUp(0))
        {
            ReleaseIfCharging();
        }
    }

    // проверка: ведьма сейчас не нажимает стрелки / A-D
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
        if (!IsCooldownReady()) return; // кулдаун всЄ ещЄ в силе

        if (_isCharging) return;
        _isCharging = true;
        _currentDots = 0;
        if (chargeUI != null) chargeUI.Clear();

        // переключаем спрайт на witch_3_3 (замах)
        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        _chargeRoutine = StartCoroutine(ChargeTick());
    }

    private IEnumerator ChargeTick()
    {
        // каждые secondsPerDot добавл€ем точку, максимум maxDots
        while (_isCharging && _currentDots < maxDots)
        {
            yield return new WaitForSeconds(secondsPerDot);
            _currentDots++;
            if (chargeUI != null) chargeUI.SetCount(_currentDots);

            if (autoReleaseAtMax && _currentDots >= maxDots)
            {
                // автоматически бросаем на полном зар€де
                ReleaseThrow();
                yield break;
            }
        }
    }

    private void ReleaseIfCharging()
    {
        if (!_isCharging) return;

        // если умерли во врем€ зар€дки Ч просто сбросить без смены спрайта на idle
        if (hp != null && hp.IsDead)
        {
            CancelCharge();
            return;
        }

        ReleaseThrow();
    }

    private void ReleaseThrow()
    {
        // нельз€ стрел€ть, если кулдаун активен или ведьма двигаетс€
        if (!IsCooldownReady() || !IsStandingNow())
        {
            // не стрел€ть Ч просто сбросить зар€д
            CancelCharge();
            return;
        }

        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);

        int dots = Mathf.Clamp(_currentDots, 1, Mathf.Max(1, maxDots));
        float t = (maxDots == 1) ? 1f : (dots - 1) / (float)(maxDots - 1); // 1 точка = min, maxDots = max
        float distance = Mathf.Lerp(minDistance, maxDistance, t);

        // ограничиваем дальность по верхней границе EnemyZone
        if (shootAlwaysUp && enemyZone != null)
        {
            float top = enemyZone.bounds.max.y - zoneTopPadding;
            float allowed = Mathf.Max(0.05f, top - firePoint.position.y);
            distance = Mathf.Min(distance, allowed);
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
                        // рассто€ние от камеры до плоскости выстрела (firePoint)
                        float depth = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
                        if (depth < cam.nearClipPlane + 0.01f) depth = cam.nearClipPlane + 0.01f;

                        mp.z = depth; // ¬ј∆Ќќ: z должен быть > nearClipPlane
                        Vector3 mouseWorld = cam.ScreenToWorldPoint(mp);
                        mouseWorld.z = firePoint.position.z;

                        dir = (mouseWorld - firePoint.position).normalized;
                    }
                }
                else if (useFacingForHorizontal && _sr != null)
                {
                    dir = _sr.flipX ? Vector2.left : Vector2.right;
                }
                // дальше: pf.Init(dir, distance, pf.speed);

                pf.Init(dir, distance, pf.speed); // lifetime подстроитс€ под distance
            }
        }

        // запускаем кулдаун
        _cooldownUntil = Time.time + fireCooldown;

        // сброс UI + возврат idle
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;

        if (_sr != null && idleSprite != null) _sr.sprite = idleSprite;
    }

    // общий метод дл€ сброса зар€дки (без выстрела)
    private void CancelCharge()
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;
        if (_sr != null && idleSprite != null) _sr.sprite = idleSprite;
    }
}
