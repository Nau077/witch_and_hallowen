using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFireballShooter : MonoBehaviour
{
    // ===================== REFS =====================
    [Header("Refs")]
    public Transform firePoint;
    public GameObject playerFireballPrefab;
    public ChargeDotsUI chargeUI;
    public FireballChargeFX chargeFX;

    // ===================== CHARGE =====================
    [Header("Charge")]
    [Min(1)] public int maxDots = 3;
    [Min(0.01f)] public float secondsPerDot = 1f;
    public float minDistance = 2.5f;
    public float maxDistance = 9f;
    public bool autoReleaseAtMax = false;

    // ===================== SPRITES =====================
    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite windupSprite;
    public Sprite throwSprite;
    [Min(0f)] public float throwSpriteTime = 0.2f;

    [Header("Throw Flash (overlay)")]
    public SpriteRenderer throwFlashRenderer; // можно оставить None — будет фолбэк

    // ===================== DIRECTION =====================
    [Header("Direction")]
    public bool shootAlwaysUp = true;
    public bool useFacingForHorizontal = false;

    // ===================== COOLDOWN =====================
    [Header("Cooldown")]
    [Min(0f)] public float fireCooldown = 0.6f;
    private float _cooldownUntil = 0f;

    // ===================== ENEMY ZONE =====================
    [Header("Clamp to Enemy Zone")]
    public SpriteRenderer enemyZone;
    public float zoneTopPadding = 0.05f;
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
    public bool useStepRanges = true;

    // ===================== MANA =====================
    [Header("Mana")]
    [Min(0)] public int manaCostPerShot = 5;

    // ===================== DEBUG =====================
    [Header("Debug")]
    [SerializeField] private bool debugLogThrow = false;

    // ===================== STATE =====================
    public bool IsCharging => _isCharging;
    public bool IsOnCooldown => Time.time < _cooldownUntil;

    private SpriteRenderer _sr;
    private Animator _anim;
    private Rigidbody2D _rb;
    private PlayerMovement _movement;
    private PlayerHealth _hp;
    private PlayerMana _mana;

    private Coroutine _chargeRoutine;
    private int _currentDots;
    private bool _isCharging;
    private bool _isMouseHeld;
    private float _chargeElapsed;
    private bool _lockWindupSpriteWhileCharging = true;

    private float _animPrevSpeed = 1f;
    private bool _animFrozen = false;

    private float _blockClicksUntil = 0f;
    private const float FocusClickBlockSecs = 0.12f;

    // фейсинг, зафиксированный при начале замаха
    private bool _facingLeftAtCharge;
    private Vector3 _scaleBeforeCharge;

    // ===================== UNITY =====================
    private void Awake()
    {
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer best = null;
        int bestOrder = int.MinValue;
        foreach (var sr in srs)
        {
            var an = sr.GetComponent<Animator>();
            if (an) { best = sr; break; }
            if (sr.sortingOrder > bestOrder) { bestOrder = sr.sortingOrder; best = sr; }
        }
        _sr = best;
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

        _blockClicksUntil = Time.unscaledTime + FocusClickBlockSecs;

        if (_anim && !_anim.enabled) _anim.enabled = true;
        _animFrozen = false;

        if (throwFlashRenderer) throwFlashRenderer.enabled = false;

        EnsureThrowFlashRenderer();
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

        // фикс: если замах активен и кнопка отпущена — всегда бросаем
        if (_isCharging && !Input.GetMouseButton(0))
        {
            ReleaseIfCharging();
        }
    }

    private void LateUpdate()
    {
        if (_hp != null && _hp.IsDead)
        {
            if (_isCharging) CancelCharge(false, true);
            return;
        }

        if (_isCharging && _lockWindupSpriteWhileCharging && _sr != null && windupSprite != null)
        {
            if (_sr.sprite != windupSprite)
                _sr.sprite = windupSprite;
        }
    }

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
            if (!_anim.enabled) _anim.enabled = true;
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

        FreezeAnimator();
        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        // фикс фейсинга
        _facingLeftAtCharge = (_movement != null) ? _movement.FacingLeft : (_sr && _sr.flipX);
        _scaleBeforeCharge = _sr ? _sr.transform.localScale : Vector3.one;

        if (_sr) _sr.flipX = _facingLeftAtCharge;

        if (firePoint)
        {
            var lp = firePoint.localPosition;
            lp.x = Mathf.Abs(lp.x) * (_facingLeftAtCharge ? -1f : 1f);
            firePoint.localPosition = lp;
        }

        // остановка по горизонтали, чтобы не маскировал бросок
        if (_rb) _rb.velocity = new Vector2(0f, _rb.velocity.y);

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
                    yield return null;
                    if (!Input.GetMouseButton(0)) ReleaseThrow();
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

    // ===================== THROW =====================
    private void ReleaseThrow()
    {
        if (_mana != null && manaCostPerShot > 0 && !_mana.CanSpend(manaCostPerShot))
        {
            CancelCharge(true, false);
            return;
        }
        if (!IsCooldownReady()) { CancelCharge(true, false); return; }

        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);

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
                    toStep = dot3ReachStep;
                }

                float fromY = (fromStep - 1) * step;
                float toY = toStep * step;

                distance = Mathf.Clamp(toY, 0f, allowed);
                ignoreFirstMeters = Mathf.Clamp(fromY, 0f, allowed - 0.001f);
            }
            else
            {
                distance = Mathf.Lerp(minDistance, maxDistance, (dots - 1f) / (maxDots - 1f));
            }
        }
        else
        {
            float t = (maxDots == 1) ? 1f : (dots - 1f) / (float)(maxDots - 1);
            distance = Mathf.Lerp(minDistance, maxDistance, t);
        }

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
                        mp.z = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
                        Vector3 mw = cam.ScreenToWorldPoint(mp);
                        mw.z = firePoint.position.z;
                        dir = (mw - firePoint.position).normalized;
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

    // ===================== THROW FLASH =====================
    private void EnsureThrowFlashRenderer()
    {
        // безопасно: если None, просто не создаём — фолбэк работает
        if (!_sr || throwFlashRenderer == null) return;

        throwFlashRenderer.sortingLayerID = _sr.sortingLayerID;
        throwFlashRenderer.sortingLayerName = _sr.sortingLayerName;
        throwFlashRenderer.sortingOrder = _sr.sortingOrder + 100;
        throwFlashRenderer.renderingLayerMask = _sr.renderingLayerMask;
        throwFlashRenderer.sharedMaterial = _sr.sharedMaterial;

        throwFlashRenderer.sprite = null;
        throwFlashRenderer.color = Color.white;
        throwFlashRenderer.enabled = false;

        throwFlashRenderer.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        throwFlashRenderer.transform.localRotation = Quaternion.identity;
        throwFlashRenderer.transform.localScale = Vector3.one;
    }

    private IEnumerator PlayThrowFlashAndBack()
    {
        _lockWindupSpriteWhileCharging = false;
        if (_anim && _anim.enabled) _anim.enabled = false;

        throwFlashRenderer.sprite = throwSprite;
        throwFlashRenderer.enabled = true;

        yield return new WaitForSeconds(throwSpriteTime);

        throwFlashRenderer.enabled = false;
        BackToIdle();

        if (_anim) _anim.enabled = true;
        _lockWindupSpriteWhileCharging = true;
    }

    private IEnumerator PlayThrowAndBack()
    {
        _lockWindupSpriteWhileCharging = false;

        if (_anim && _anim.enabled)
            _anim.enabled = false;

        if (_sr && throwSprite)
            _sr.sprite = throwSprite;

        yield return new WaitForSeconds(throwSpriteTime);

        BackToIdle();

        if (_anim)
            _anim.enabled = true;

        _lockWindupSpriteWhileCharging = true;
    }

    private void BackToIdle()
    {
        if (_sr && idleSprite) _sr.sprite = idleSprite;
        if (throwFlashRenderer) throwFlashRenderer.enabled = false;

        // возвращаем фейсинг
        if (_sr) _sr.flipX = _movement ? _movement.FacingLeft : _sr.flipX;

        UnfreezeAnimator();
    }

    // ===================== CANCEL =====================
    private void CancelCharge(bool changeSprite = true, bool keepAnimatorDisabled = false)
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
        if (chargeUI != null) chargeUI.Clear();

        _currentDots = 0;
        _chargeElapsed = 0f;

        if (changeSprite && _sr && idleSprite) _sr.sprite = idleSprite;
        if (throwFlashRenderer) throwFlashRenderer.enabled = false;

        UnfreezeAnimator();
        if (chargeFX != null) chargeFX.Cancel();
    }

    public void CancelAllImmediate(bool keepAnimatorDisabled = false)
        => CancelCharge(false, keepAnimatorDisabled);

    // ===================== COOLDOWN NORMALIZED =====================
    public float CooldownNormalized
    {
        get
        {
            if (fireCooldown <= 0f) return 0f;
            float remain = _cooldownUntil - Time.time;
            return Mathf.Clamp01(remain / fireCooldown);
        }
    }
}
