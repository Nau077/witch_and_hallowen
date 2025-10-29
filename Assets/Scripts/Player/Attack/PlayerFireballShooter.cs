using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFireballShooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;
    public GameObject playerFireballPrefab;
    public ChargeDotsUI chargeUI;
    public FireballChargeFX chargeFX;

    [Header("Charge")]
    public int maxDots = 3;
    public float secondsPerDot = 1f;
    public float minDistance = 2.5f;
    public float maxDistance = 9f;
    public bool autoReleaseAtMax = false;

    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite windupSprite;

    [Header("Direction")]
    public bool shootAlwaysUp = true;
    public bool useFacingForHorizontal = false;

    [Header("Cooldown")]
    public float fireCooldown = 0.6f;
    private float _cooldownUntil = 0f;

    [Header("Clamp to Enemy Zone")]
    public SpriteRenderer enemyZone;
    public float zoneTopPadding = 0.05f;

    [Tooltip("Автоматическая дискретизация зоны (старыми полями)")]
    public bool useZoneSteps = true;
    [Min(1)] public int zoneSteps = 3;
    [Min(0)] public int ignoredZoneSteps = 0;
    [Tooltip("Если включено — целимся в центр ступени")]
    public bool snapToCenters = false;

    // ====== НОВЫЙ РУЧНОЙ РЕЖИМ ======
    [Header("Manual Step Tuning (NEW)")]
    public bool manualStepTuning = true;
    [Min(1)] public int totalZoneSteps = 12;
    [Min(0)] public int ignoredStepsCommon = 4;
    public int dot1ReachStep = 7;
    public int dot2ReachStep = 9;
    public int dot3ReachStep = 12;
    public bool manualAimCenters = false;

    public bool IsCharging => _isCharging;

    private SpriteRenderer _sr;
    private Coroutine _chargeRoutine;
    private int _currentDots;
    private bool _isCharging;
    private bool _isMouseHeld;
    private PlayerHealth hp;
    private Rigidbody2D rb;
    private bool _lockWindupSpriteWhileCharging = true;
    private float _chargeElapsed;

    [Tooltip("Если включено — каждая точка имеет свой диапазон (1-5, 5-9, 9-12) вместо общей мёртвой зоны.")]
    public bool useStepRanges = true;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        hp = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (chargeUI != null) chargeUI.Clear();
        if (idleSprite == null && _sr != null) idleSprite = _sr.sprite;
    }

    private void Update()
    {
        if (hp != null && hp.IsDead) return;

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
    }

    public void CancelAllImmediate() => CancelCharge(false);

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

    private void StartCharging()
    {
        if (hp != null && hp.IsDead) return;
        if (!IsCooldownReady()) return;
        if (_isCharging) return;

        _isCharging = true;
        _currentDots = 1;
        _chargeElapsed = 0f;
        if (chargeUI != null) { chargeUI.Clear(); chargeUI.SetCount(_currentDots); }
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
                    yield return null;
                    if (!_isMouseHeld)
                        ReleaseThrow();
                }
            }
        }
    }

    private void ReleaseIfCharging()
    {
        if (!_isCharging) return;
        if (hp != null && hp.IsDead) { CancelCharge(false); return; }
        ReleaseThrow();
    }

    private int GetManualReachStepForDot(int dots)
    {
        if (dots <= 1) return dot1ReachStep;
        if (dots == 2) return dot2ReachStep;
        return dot3ReachStep;
    }

    private void ReleaseThrow()
    {
        if (!IsCooldownReady()) { CancelCharge(); return; }

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

                int fromStep = 1;
                int toStep = 1;

                // --- новая логика диапазонов ---
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
                    // старый вариант — просто "до какой клетки"
                    fromStep = ignoredStepsCommon;
                    toStep = GetManualReachStepForDot(dots);
                }

                // переводим в мировые метры
                float fromY = (fromStep - 1) * step;
                float toY = toStep * step;

                // если прицеливаемся в центр диапазона — берём середину
                float aimUnits = manualAimCenters ? ((fromY + toY) / 2f / step) : toStep;
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

        // === ВЫСТРЕЛ ===
        if (playerFireballPrefab != null && firePoint != null)
        {
            var go = Instantiate(playerFireballPrefab, firePoint.position, Quaternion.identity);
            var pf = go.GetComponent<PlayerFireball>();
            if (pf != null)
            {
                var cam = Camera.main;
                Vector2 dir = Vector2.up;
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
                    dir = _sr.flipX ? Vector2.left : Vector2.right;

                // теперь передаем расстояние, и где начинать наносить урон
                pf.Init(dir, distance, pf.speed, ignoreFirstMeters);
                pf.ignoreEnemiesFirstMeters = ignoreFirstMeters;
            }
        }

        _cooldownUntil = Time.time + fireCooldown;
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;
        _chargeElapsed = 0f;
        if (_sr != null && idleSprite != null) _sr.sprite = idleSprite;
        if (chargeFX != null) chargeFX.Release();
    }

    private void CancelCharge(bool changeSprite = true)
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;
        _chargeElapsed = 0f;
        if (changeSprite && _sr != null && idleSprite != null)
            _sr.sprite = idleSprite;
        if (chargeFX != null) chargeFX.Cancel();
    }
}
