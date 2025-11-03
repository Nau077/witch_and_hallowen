using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFireballShooter : MonoBehaviour
{
    // ===================== REFS =====================
    [Header("Refs")]
    public Transform firePoint;
    public GameObject playerFireballPrefab;
    public ChargeDotsUI chargeUI;     // UI с точками заряда (опционально)
    public FireballChargeFX chargeFX; // свечение/аура заряда (опционально)

    // ===================== CHARGE =====================
    [Header("Charge")]
    [Min(1)] public int maxDots = 3;                // 1..N "точек" заряда
    [Min(0.01f)] public float secondsPerDot = 1f;   // сек на получение следующей точки
    public float minDistance = 2.5f;                // для режима без EnemyZone
    public float maxDistance = 9f;                  // для режима без EnemyZone
    public bool autoReleaseAtMax = false;           // автом. бросать при достижении maxDots

    // ===================== SPRITES =====================
    [Header("Sprites")]
    [Tooltip("Базовая стойка (когда ничего не делаем). Если пусто — возьмём из SpriteRenderer на старте.")]
    public Sprite idleSprite;

    [Tooltip("Поза замаха (держим ЛКМ). На время замаха Animator замораживаем.")]
    public Sprite windupSprite;

    [Tooltip("Кадр броска (короткий всплеск при отпускании ЛКМ).")]
    public Sprite throwSprite;
    [Min(0f)] public float throwSpriteTime = 0.08f;

    [Header("Throw Flash (overlay)")]
    [Tooltip("Опционально: отдельный SpriteRenderer поверх базового для вспышки броска.")]
    public SpriteRenderer throwFlashRenderer; // <-- новинка: оверлей, OrderInLayer выше основного

    // ===================== DIRECTION =====================
    [Header("Direction")]
    [Tooltip("Если true — шары летят строго вертикально вверх или влево/вправо по facing (см. useFacingForHorizontal).")]
    public bool shootAlwaysUp = true;

    [Tooltip("Если shootAlwaysUp = true и включено — направление будет по facing (left/right), а не вверх.")]
    public bool useFacingForHorizontal = false;

    // ===================== COOLDOWN =====================
    [Header("Cooldown")]
    [Min(0f)] public float fireCooldown = 0.6f;
    private float _cooldownUntil = 0f;

    // ===================== ENEMY ZONE (ступени по высоте) =====================
    [Header("Clamp to Enemy Zone")]
    public SpriteRenderer enemyZone;
    public float zoneTopPadding = 0.05f;

    [Tooltip("Старый дискретный режим: zoneSteps + ignoredZoneSteps.")]
    public bool useZoneSteps = true;
    [Min(1)] public int zoneSteps = 3;
    [Min(0)] public int ignoredZoneSteps = 0;
    public bool snapToCenters = false;

    [Header("Manual Step Tuning (NEW)")]
    public bool manualStepTuning = true;
    [Min(1)] public int totalZoneSteps = 12;
    [Min(0)] public int ignoredStepsCommon = 4;
    public int dot1ReachStep = 7;
    public int dot2ReachStep = 9;
    public int dot3ReachStep = 12;
    public bool manualAimCenters = false;

    [Tooltip("Если true — каждая точка имеет свой диапазон: 1:[1..dot1], 2:[dot1..dot2], 3:[dot2..dot3].")]
    public bool useStepRanges = true;

    // ===================== MANA =====================
    [Header("Mana")]
    [Min(0)] public int manaCostPerShot = 5;

    // ===================== STATE/PROPS =====================
    public bool IsCharging => _isCharging;
    public bool IsOnCooldown => Time.time < _cooldownUntil;

    // private refs
    private SpriteRenderer _sr;   // SpriteRenderer на ребёнке (witch_runner_2_1)
    private Animator _anim;       // Animator на том же объекте
    private Rigidbody2D _rb;
    private PlayerMovement _movement;
    private PlayerHealth _hp;
    private PlayerMana _mana;

    // runtime
    private Coroutine _chargeRoutine;
    private int _currentDots;
    private bool _isCharging;
    private bool _isMouseHeld;
    private float _chargeElapsed;
    private bool _lockWindupSpriteWhileCharging = true;

    // Animator freeze (вместо enabled=false)
    private float _animPrevSpeed = 1f;
    private bool _animFrozen = false;

    // focus debounce (ложные клики при смене фокуса)
    private float _blockClicksUntil = 0f;
    private const float FocusClickBlockSecs = 0.12f;

    // ===================== UNITY =====================
    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>(true);
        _anim = _sr ? _sr.GetComponent<Animator>() : null;
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();
        _hp = GetComponent<PlayerHealth>();
        _mana = GetComponent<PlayerMana>();
    }

    private void Start()
    {
        if (chargeUI != null) chargeUI.Clear();
        if (_sr != null && idleSprite == null) idleSprite = _sr.sprite;

        // игнорируем «ложный клик» сразу после старта/получения фокуса
        _blockClicksUntil = Time.unscaledTime + FocusClickBlockSecs;

        // страховка: если Animator кто-то выключил в сцене — включим
        if (_anim && !_anim.enabled) _anim.enabled = true;
        _animFrozen = false;

        // убедимся, что вспышка по умолчанию скрыта
        if (throwFlashRenderer) throwFlashRenderer.enabled = false;
    }

    private void Update()
    {
        if (_hp != null && _hp.IsDead) return;

        HandleInput();

        if (_isCharging && chargeFX != null)
        {
            bool canGrow = (_currentDots < maxDots);
            if (canGrow) _chargeElapsed += Time.deltaTime;

            float dotSpan = Mathf.Max(0.0001f, secondsPerDot);
            float frac = canGrow ? Mathf.Clamp01(_chargeElapsed / dotSpan) : 1f;

            float t;
            if (maxDots <= 1) t = 1f;
            else
            {
                float baseDots = Mathf.Clamp(_currentDots - 1, 0, maxDots - 1);
                t = Mathf.Clamp01((baseDots + frac) / (maxDots - 1));
            }
            chargeFX.UpdateCharge(t);
        }

        // Страховка потерянного MouseUp (например, при клике по вкладке Game)
        if (_isCharging && _isMouseHeld && !Input.GetMouseButton(0))
        {
            _isMouseHeld = false;
            ReleaseIfCharging();
        }
    }

    private void LateUpdate()
    {
        if (_hp != null && _hp.IsDead)
        {
            if (_isCharging) CancelCharge(false, true); // ничего не трогаем в Animator
            return;
        }

        // Поддерживаем позу замаха, пока зажата кнопка
        if (_isCharging && _lockWindupSpriteWhileCharging && _sr != null && windupSprite != null)
        {
            if (_sr.sprite != windupSprite)
                _sr.sprite = windupSprite;
        }
    }

    // ===================== FOCUS/PAUSE =====================
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            _blockClicksUntil = Time.unscaledTime + FocusClickBlockSecs;
        }
        else
        {
            _isMouseHeld = false;
            if (_isCharging) CancelCharge(true, true);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _isMouseHeld = false;
            if (_isCharging) CancelCharge(true, true);
        }
    }

    // ===================== INPUT =====================
    private void HandleInput()
    {
        // игнор мнимых кликов сразу после фокуса
        if (Time.unscaledTime < _blockClicksUntil) return;

        if (Input.GetMouseButtonDown(0))
        {
            _isMouseHeld = true;
            StartCharging();
        }
        if (Input.GetMouseButtonUp(0))
        {
            _isMouseHeld = false;
            ReleaseIfCharging();
        }
    }

    private bool IsCooldownReady() => Time.time >= _cooldownUntil;

    // ===================== ANIMATOR FREEZE =====================
    private void FreezeAnimator()
    {
        if (_anim && !_animFrozen)
        {
            if (!_anim.enabled) _anim.enabled = true; // на всякий
            _animPrevSpeed = _anim.speed;
            _anim.speed = 0f;
            _animFrozen = true;
        }
    }

    private void UnfreezeAnimator()
    {
        if (_anim && _animFrozen)
        {
            _anim.speed = _animPrevSpeed;
            _animFrozen = false;
        }
    }

    // ===================== CHARGE FLOW =====================
    private void StartCharging()
    {
        if (_hp != null && _hp.IsDead) return;
        if (!IsCooldownReady()) return;
        if (_isCharging) return;

        _isCharging = true;
        _currentDots = 1;
        _chargeElapsed = 0f;

        if (chargeUI != null) { chargeUI.Clear(); chargeUI.SetCount(_currentDots); }

        // вместо выключения Animator — замораживаем его
        FreezeAnimator();
        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        if (chargeFX != null) chargeFX.BeginCharge();
        _chargeRoutine = StartCoroutine(ChargeTick());
    }

    private IEnumerator ChargeTick()
    {
        while (_isCharging)
        {
            yield return new WaitForSeconds(secondsPerDot);
            if (!_isCharging) yield break;

            if (_currentDots < maxDots)
            {
                _currentDots++;
                _chargeElapsed = 0f;

                if (chargeUI != null) chargeUI.SetCount(_currentDots);

                if (autoReleaseAtMax && _currentDots >= maxDots)
                {
                    yield return null; // один кадр «задержки»
                    if (!_isMouseHeld) ReleaseThrow();
                }
            }
        }
    }

    private void ReleaseIfCharging()
    {
        if (!_isCharging) return;
        if (_hp != null && _hp.IsDead) { CancelCharge(false, true); return; }
        ReleaseThrow();
    }

    // ===================== DISTANCE HELPERS =====================
    private int GetManualReachStepForDot(int dots)
    {
        if (dots <= 1) return dot1ReachStep;
        if (dots == 2) return dot2ReachStep;
        return dot3ReachStep;
    }

    // ===================== THROW =====================
    private void ReleaseThrow()
    {
        // Мана
        if (_mana != null && manaCostPerShot > 0 && !_mana.CanSpend(manaCostPerShot))
        {
            CancelCharge(true, false);
            return;
        }
        if (!IsCooldownReady()) { CancelCharge(true, false); return; }

        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);

        // --- вычисление дистанции ---
        int dots = Mathf.Clamp(_currentDots, 1, Mathf.Max(1, maxDots));
        float distance;
        float ignoreFirstMeters = 0f;

        if (shootAlwaysUp && enemyZone != null && useZoneSteps)
        {
            if (manualStepTuning)
            {
                float top = enemyZone.bounds.max.y - zoneTopPadding;
                float allowed = Mathf.Max(0.05f, top - firePoint.position.y);

                int total = Mathf.Max(1, totalZoneSteps);
                float step = allowed / total;

                int fromStep, toStep;
                if (useStepRanges)
                {
                    if (dots == 1) { fromStep = 1; toStep = dot1ReachStep; }
                    else if (dots == 2) { fromStep = dot1ReachStep; toStep = dot2ReachStep; }
                    else { fromStep = dot2ReachStep; toStep = dot3ReachStep; }

                    fromStep = Mathf.Clamp(fromStep, 1, total);
                    toStep = Mathf.Clamp(toStep, fromStep + 1, total);
                }
                else
                {
                    fromStep = ignoredStepsCommon;
                    toStep = GetManualReachStepForDot(dots);
                }

                float fromY = (fromStep - 1) * step;
                float toY = toStep * step;

                distance = Mathf.Clamp(toY, 0f, allowed);
                ignoreFirstMeters = Mathf.Clamp(fromY, 0f, allowed - 0.001f);
            }
            else
            {
                float top = enemyZone.bounds.max.y - zoneTopPadding;
                float allowed = Mathf.Max(0.05f, top - firePoint.position.y);

                int workSteps = Mathf.Max(1, zoneSteps);
                int ignored = Mathf.Max(0, ignoredZoneSteps);
                int total = ignored + workSteps;
                float step = allowed / total;

                int k = Mathf.Clamp(dots, 1, workSteps);
                float baseUnits = ignored + (snapToCenters ? (k - 0.5f) : k);

                distance = Mathf.Clamp(baseUnits * step, 0f, allowed);
                ignoreFirstMeters = Mathf.Clamp(ignored * step, 0f, Mathf.Max(0f, distance - 0.001f));
            }
        }
        else
        {
            float t = (maxDots == 1) ? 1f : (dots - 1) / (float)(maxDots - 1);
            distance = Mathf.Lerp(minDistance, maxDistance, t);

            if (shootAlwaysUp && enemyZone != null)
            {
                float top = enemyZone.bounds.max.y - zoneTopPadding;
                float allowed = Mathf.Max(0.05f, top - firePoint.position.y);
                distance = Mathf.Min(distance, allowed);
            }
            ignoreFirstMeters = 0f;
        }

        // --- выстрел ---
        if (playerFireballPrefab != null && firePoint != null)
        {
            if (_mana != null && manaCostPerShot > 0 && !_mana.TrySpend(manaCostPerShot))
            {
                CancelCharge(true, false);
                return;
            }

            var go = Instantiate(playerFireballPrefab, firePoint.position, Quaternion.identity);
            var pf = go.GetComponent<PlayerFireball>();

            if (pf != null)
            {
                Vector2 dir = Vector2.up;

                if (!shootAlwaysUp)
                {
                    var cam = Camera.main;
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
                else if (useFacingForHorizontal && _movement != null)
                {
                    dir = _movement.FacingLeft ? Vector2.left : Vector2.right;
                }

                pf.Init(dir, distance, pf.speed, ignoreFirstMeters);
                pf.ignoreEnemiesFirstMeters = ignoreFirstMeters;
            }
        }

        _cooldownUntil = Time.time + fireCooldown;

        // краткий кадр броска (оверлей приоритетно) -> назад в idle + разморозка Animator
        if (throwFlashRenderer != null && throwSprite != null)
            StartCoroutine(PlayThrowFlashAndBack());
        else if (_sr != null && throwSprite != null)
            StartCoroutine(PlayThrowAndBack());
        else
            BackToIdle();

        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;
        _chargeElapsed = 0f;

        if (chargeFX != null) chargeFX.Release();
    }

    // Вариант 1: вспышка поверх (рекомендуется)
    private IEnumerator PlayThrowFlashAndBack()
    {
        _lockWindupSpriteWhileCharging = false;

        // показать вспышку броска поверх Animator
        throwFlashRenderer.sprite = throwSprite;
        throwFlashRenderer.enabled = true;

        yield return new WaitForSeconds(throwSpriteTime);

        // скрыть вспышку и вернуться к Idle (и разморозить Animator)
        throwFlashRenderer.enabled = false;
        BackToIdle();

        _lockWindupSpriteWhileCharging = true;
    }

    // Вариант 2: фолбэк — меняем основной спрайт без оверлея
    private IEnumerator PlayThrowAndBack()
    {
        _lockWindupSpriteWhileCharging = false;

        if (_sr && throwSprite) _sr.sprite = throwSprite;
        yield return new WaitForSeconds(throwSpriteTime);

        BackToIdle();
        _lockWindupSpriteWhileCharging = true;
    }

    private void BackToIdle()
    {
        if (_sr && idleSprite) _sr.sprite = idleSprite;
        if (throwFlashRenderer) throwFlashRenderer.enabled = false; // на всякий
        UnfreezeAnimator(); // ВОЗВРАЩАЕМ скорость Animator
    }

    // ===================== CANCEL =====================
    // keepAnimatorDisabled оставлен для совместимости сигнатуры — теперь игнорируется,
    // потому что мы не выключаем Animator, а фризим его.
    private void CancelCharge(bool changeSprite = true, bool keepAnimatorDisabled = false)
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
        if (chargeUI != null) chargeUI.Clear();

        _currentDots = 0;
        _chargeElapsed = 0f;

        if (changeSprite && _sr && idleSprite) _sr.sprite = idleSprite;

        if (throwFlashRenderer) throwFlashRenderer.enabled = false; // скрыть вспышку
        UnfreezeAnimator(); // всегда снимаем фриз

        if (chargeFX != null) chargeFX.Cancel();
    }

    public void CancelAllImmediate(bool keepAnimatorDisabled = false)
        => CancelCharge(false, keepAnimatorDisabled);

    // ===================== COOLDOWN normalized =====================
    public float CooldownNormalized
    {
        get
        {
            if (fireCooldown <= 0f) return 0f;
            float remain = _cooldownUntil - Time.time;
            return Mathf.Clamp01(remain / fireCooldown); // 1 -> 0 по мере остывания
        }
    }
}
