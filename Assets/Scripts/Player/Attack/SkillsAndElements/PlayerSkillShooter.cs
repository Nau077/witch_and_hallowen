using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerSkillShooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;
    public SkillLoadout loadout;       // 5 слотов скиллов
    public FireballChargeFX chargeFX;  // визуал ауры заряда
    public ChargeDotsUI chargeUI;      // визуал «точек»

    [Header("Sprites / Animator Targets")]
    [Tooltip("Основной SpriteRenderer тела персонажа (НЕ ThrowFlash). Задай сюда ребёнка со спрайтом ведьмы.")]
    public SpriteRenderer bodyRenderer;
    [Tooltip("Animator, который крутит анимации ведьмы. Обычно на том же объекте, где bodyRenderer.")]
    public Animator bodyAnimator;
    [Tooltip("Отдельный оверлейный рендерер для кадра броска (опционально).")]
    public SpriteRenderer throwFlashRenderer;

    [Header("Throw Frames")]
    public Sprite idleSprite;
    public Sprite windupSprite;
    public Sprite throwSprite;
    [Min(0f)] public float throwSpriteTime = 0.20f;

    [Header("Charge")]
    [Min(1)] public int maxDots = 3;
    [Min(0.01f)] public float secondsPerDot = 1f;
    public float minDistance = 2.5f;
    public float maxDistance = 9f;
    public bool autoReleaseAtMax = false;

    [Header("Direction")]
    public bool shootAlwaysUp = true;
    public bool useFacingForHorizontal = false;

    [Header("Mana")]
    public PlayerMana playerMana;
    public ManaBarUI manaBarUI;

    [Header("Enemy Zone")]
    public SpriteRenderer enemyZone;
    public float zoneTopPadding = 0.05f;
    public bool manualStepTuning = true;
    [Min(1)] public int totalZoneSteps = 12;
    [Min(0)] public int ignoredStepsCommon = 4;
    public int dot1ReachStep = 2;
    public int dot2ReachStep = 7;
    public int dot3ReachStep = 12;
    public bool useStepRanges = true;

    public bool IsChargingPublic => _isCharging;
    public bool IsCharging => _isCharging;

    private Rigidbody2D _rb;
    private PlayerMovement _movement;

    private bool _isCharging;
    private int _currentDots;
    private float _chargeElapsed;
    private Coroutine _chargeRoutine;
    private bool _lockWindupSpriteWhileCharging = true;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();

        if (!loadout)
            loadout = GetComponent<SkillLoadout>();
        if (!loadout)
            loadout = FindObjectOfType<SkillLoadout>();

        if (!playerMana)
            playerMana = GetComponent<PlayerMana>();
        if (!playerMana)
            playerMana = FindObjectOfType<PlayerMana>();

        if (bodyRenderer == null)
        {
            var allSR = GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer candidate = null;
            foreach (var sr in allSR)
            {
                var an = sr.GetComponent<Animator>();
                if (an != null) { candidate = sr; break; }
            }
            if (candidate == null && allSR.Length > 0) candidate = allSR[0];
            bodyRenderer = candidate;
        }

        if (bodyAnimator == null && bodyRenderer != null)
            bodyAnimator = bodyRenderer.GetComponent<Animator>();

        if (throwFlashRenderer != null)
        {
            throwFlashRenderer.enabled = false;
            EnsureThrowFlashRenderer();
        }

        if (bodyRenderer != null && idleSprite == null)
            idleSprite = bodyRenderer.sprite;
    }

    void Start()
    {
        if (chargeUI) chargeUI.Clear();
    }

    void Update()
    {
        HandleInput();
        HandleWheel();

        if (_isCharging && chargeFX != null)
        {
            bool canGrow = (_currentDots < maxDots);
            if (canGrow) _chargeElapsed += Time.deltaTime;

            float frac = canGrow ? Mathf.Clamp01(_chargeElapsed / Mathf.Max(0.0001f, secondsPerDot)) : 1f;
            float t;
            if (maxDots <= 1) t = 1f;
            else
            {
                float baseDots = Mathf.Clamp(_currentDots - 1, 0, maxDots - 1);
                t = Mathf.Clamp01((baseDots + frac) / (maxDots - 1));
            }
            chargeFX.UpdateCharge(t);
        }

        if (_isCharging && !Input.GetMouseButton(0))
            ReleaseIfCharging();
    }

    void LateUpdate()
    {
        if (_isCharging && _lockWindupSpriteWhileCharging && bodyRenderer && windupSprite)
            if (bodyRenderer.sprite != windupSprite) bodyRenderer.sprite = windupSprite;
    }

    private bool _skipNextClickFromUI;

    public void SkipNextClickFromUI()
    {
        _skipNextClickFromUI = true;
    }

    void HandleInput()
    {
        // 1) Если курсор сейчас над UI — не стреляем
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            // просто игнорируем нажатия мыши в этом кадре
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
                return;
        }

        // 2) Обычная логика
        if (Input.GetMouseButtonDown(0))
        {
            if (_skipNextClickFromUI)
            {
                _skipNextClickFromUI = false;
            }
            else
            {
                StartCharging();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseIfCharging();
        }
    }


    void HandleWheel()
    {
        if (loadout == null) return;
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        if (_isCharging) CancelCharge(changeSprite: true);

        if (scroll > 0f) loadout.SelectPrev();
        else loadout.SelectNext();
    }

    void StartCharging()
    {
        if (!loadout) return;
        var s = loadout.Active;
        if (s == null || s.def == null) return;
        if (!loadout.IsActiveReadyToUse()) return;

        int manaCost = s.def.manaCostPerShot;
        if (manaBarUI != null)
            manaBarUI.minCastCost = manaCost;

        if (playerMana && manaCost > 0 && !playerMana.CanSpend(manaCost))
        {
            if (manaBarUI != null)
                manaBarUI.FlashNoMana();
            return;
        }

        _isCharging = true;
        _currentDots = 1;
        _chargeElapsed = 0f;

        if (chargeUI) { chargeUI.Clear(); chargeUI.SetCount(_currentDots); }

        if (bodyAnimator != null) bodyAnimator.speed = 0f;
        if (bodyRenderer && windupSprite) bodyRenderer.sprite = windupSprite;

        if (_rb) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        // определяем, ледяной ли это скилл
        bool isIceSkill =
            s.def.projectilePrefab != null &&
            s.def.projectilePrefab.GetComponent<PlayerIceShard>() != null;

        if (chargeFX) chargeFX.BeginCharge(isIceSkill);

        if (_movement && firePoint)
        {
            var lp = firePoint.localPosition;
            lp.x = Mathf.Abs(lp.x) * (_movement.FacingLeft ? -1f : 1f);
            firePoint.localPosition = lp;
        }

        _chargeRoutine = StartCoroutine(ChargeTick());
    }

    IEnumerator ChargeTick()
    {
        while (_isCharging)
        {
            yield return new WaitForSeconds(secondsPerDot);
            if (!_isCharging) yield break;

            if (_currentDots < maxDots)
            {
                _currentDots++;
                _chargeElapsed = 0f;
                if (chargeUI) chargeUI.SetCount(_currentDots);

                if (autoReleaseAtMax && _currentDots >= maxDots)
                {
                    yield return null;
                    if (!Input.GetMouseButton(0)) ReleaseThrow();
                }
            }
        }
    }

    void ReleaseIfCharging()
    {
        if (_isCharging) ReleaseThrow();
    }

    void ReleaseThrow()
    {
        _isCharging = false;

        if (_chargeRoutine != null)
            StopCoroutine(_chargeRoutine);

        var slot = loadout.Active;

        if (slot == null || slot.def == null)
        {
            CancelCharge(true);
            return;
        }

        if (slot.IsOnCooldown || !slot.HasCharges)
        {
            CancelCharge(true);
            return;
        }

        int manaCost = slot.def.manaCostPerShot;
        if (playerMana && manaCost > 0)
        {
            if (!playerMana.TrySpend(manaCost))
            {
                CancelCharge(true);
                return;
            }
        }

        int dots = Mathf.Clamp(_currentDots, 1, Mathf.Max(1, maxDots));
        float distance = ComputeDistance(dots, out float ignoreFirstMeters);

        if (firePoint && slot.def.projectilePrefab)
        {
            var go = Object.Instantiate(slot.def.projectilePrefab, firePoint.position, Quaternion.identity);
            var proj = go.GetComponent<IProjectile>();
            if (proj != null)
            {
                Vector2 dir = Vector2.up;

                if (!shootAlwaysUp)
                {
                    var cam = Camera.main;
                    if (cam)
                    {
                        var mp = Input.mousePosition;
                        mp.z = Mathf.Abs(cam.transform.position.z - firePoint.position.z);
                        var mw = cam.ScreenToWorldPoint(mp);
                        mw.z = firePoint.position.z;
                        dir = (mw - firePoint.position).normalized;
                    }
                }
                else if (useFacingForHorizontal && _movement != null)
                {
                    dir = _movement.FacingLeft ? Vector2.left : Vector2.right;
                }

                proj.Init(dir, distance, -1f, ignoreFirstMeters);
            }
        }

        int usedSlotIndex = loadout.ActiveIndex;

        loadout.TrySpendOneCharge();
        loadout.StartCooldownNow();

        if (!slot.def.infiniteCharges && slot.charges <= 0)
        {
            // 1) Убираем скилл из слота → он исчезнет из SkillBarUI
            loadout.ClearSkillAtIndex(usedSlotIndex);

            // 2) Переключаемся на следующий доступный
            loadout.SwitchToNextAvailable();
        }

        PlayThrowThenIdle();
        if (chargeUI) chargeUI.SetCount(0);
        if (chargeFX) chargeFX.Release();

        _currentDots = 0;
        _chargeElapsed = 0f;
    }

    void PlayThrowThenIdle()
    {
        if (bodyAnimator) bodyAnimator.enabled = false;
        if (bodyRenderer && throwSprite) bodyRenderer.sprite = throwSprite;

        if (throwFlashRenderer && throwSprite)
            StartCoroutine(FlashRoutine());
        else
            StartCoroutine(ThrowRoutine());
    }

    float ComputeDistance(int dots, out float ignoreFirstMeters)
    {
        ignoreFirstMeters = 0f;
        if (shootAlwaysUp && enemyZone)
        {
            float top = enemyZone.bounds.max.y - zoneTopPadding;
            float allowed = Mathf.Max(0.05f, top - firePoint.position.y);

            if (!manualStepTuning)
                return Mathf.Lerp(minDistance, maxDistance, (dots - 1f) / (float)(maxDots - 1));

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
            else { fromStep = ignoredStepsCommon; toStep = dot3ReachStep; }

            float fromY = (fromStep - 1) * step;
            float toY = toStep * step;
            ignoreFirstMeters = Mathf.Clamp(fromY, 0f, allowed - 0.001f);
            return Mathf.Clamp(toY, 0f, allowed);
        }

        float t = (maxDots == 1) ? 1f : (dots - 1f) / (float)(maxDots - 1);
        return Mathf.Lerp(minDistance, maxDistance, t);
    }

    IEnumerator FlashRoutine()
    {
        _lockWindupSpriteWhileCharging = false;

        throwFlashRenderer.sprite = throwSprite;
        throwFlashRenderer.enabled = true;

        yield return new WaitForSeconds(throwSpriteTime);

        throwFlashRenderer.enabled = false;
        BackToIdle();

        if (bodyAnimator) bodyAnimator.enabled = true;
        _lockWindupSpriteWhileCharging = true;
    }

    IEnumerator ThrowRoutine()
    {
        _lockWindupSpriteWhileCharging = false;

        yield return new WaitForSeconds(throwSpriteTime);

        BackToIdle();

        if (bodyAnimator) bodyAnimator.enabled = true;
        _lockWindupSpriteWhileCharging = true;
    }

    void BackToIdle()
    {
        if (bodyRenderer && idleSprite) bodyRenderer.sprite = idleSprite;
        if (throwFlashRenderer) throwFlashRenderer.enabled = false;
        if (bodyAnimator) bodyAnimator.speed = 1f;
    }

    void CancelCharge(bool changeSprite)
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
        if (chargeUI) chargeUI.Clear();

        if (changeSprite && bodyRenderer && idleSprite) bodyRenderer.sprite = idleSprite;
        if (throwFlashRenderer) throwFlashRenderer.enabled = false;

        if (bodyAnimator) bodyAnimator.speed = 1f;
        if (chargeFX) chargeFX.Cancel();

        _currentDots = 0;
        _chargeElapsed = 0f;
    }

    public void CancelAllImmediate(bool keepAnimatorDisabled = false)
    {
        CancelCharge(changeSprite: false);
        if (!keepAnimatorDisabled && bodyAnimator) bodyAnimator.enabled = true;
    }

    void EnsureThrowFlashRenderer()
    {
        if (!bodyRenderer || !throwFlashRenderer) return;
        throwFlashRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        throwFlashRenderer.sortingLayerName = bodyRenderer.sortingLayerName;
        throwFlashRenderer.sortingOrder = bodyRenderer.sortingOrder + 100;
        throwFlashRenderer.renderingLayerMask = bodyRenderer.renderingLayerMask;
        throwFlashRenderer.sharedMaterial = bodyRenderer.sharedMaterial;
        throwFlashRenderer.sprite = null;
        throwFlashRenderer.color = Color.white;
        throwFlashRenderer.enabled = false;
        throwFlashRenderer.transform.localPosition = new Vector3(0f, 0f, -0.01f);
    }
}
