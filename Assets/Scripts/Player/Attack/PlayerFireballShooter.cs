using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFireballShooter : MonoBehaviour
{
    // ===================== REFS =====================
    [Header("Refs")]
    public Transform firePoint;                 // Точка, из которой вылетает фаербол (пивот выстрела)
    public GameObject playerFireballPrefab;     // Префаб с компонентом PlayerFireball (сам снаряд)
    public ChargeDotsUI chargeUI;               // (опционально) UI-точки заряда
    public FireballChargeFX chargeFX;           // (опционально) визуальные FX в момент заряда

    // ===================== CHARGE =====================
    [Header("Charge")]
    [Min(1)] public int maxDots = 3;           // Максимум «точек» заряда (уровней силы)
    [Min(0.01f)] public float secondsPerDot = 1f; // Сколько секунд до следующей точки заряда
    public float minDistance = 2.5f;           // Минимальная дальность (для расчёта без зоны)
    public float maxDistance = 9f;             // Максимальная дальность (для расчёта без зоны)
    public bool autoReleaseAtMax = false;      // Если true — отпускаем выстрел, как только набрали максимум

    // ===================== SPRITES =====================
    [Header("Sprites")]
    public Sprite idleSprite;                   // «Поза покоя»
    public Sprite windupSprite;                 // «Замах» — показывается во время удержания кнопки
    public Sprite throwSprite;                  // «Кадр броска» — коротко поверх анимации
    [Min(0f)] public float throwSpriteTime = 0.2f; // Как долго показывать кадр броска

    [Header("Throw Flash (overlay)")]
    public SpriteRenderer throwFlashRenderer;   // Отдельный SpriteRenderer-оверлей. Можно оставить None: тогда будет фолбэк — замена основного спрайта

    // ===================== DIRECTION =====================
    [Header("Direction")]
    public bool shootAlwaysUp = true;           // Если true — стреляем жёстко вверх (или строго влево/вправо, если useFacingForHorizontal)
    public bool useFacingForHorizontal = false; // Работает при shootAlwaysUp: вместо «вверх» — влево/вправо по Facing персонажа

    // ===================== COOLDOWN =====================
    [Header("Cooldown")]
    [Min(0f)] public float fireCooldown = 0.6f; // Время перезарядки между выстрелами
    private float _cooldownUntil = 0f;           // Временная отметка (Time.time), когда можно снова стрелять

    // ===================== ENEMY ZONE =====================
    [Header("Clamp to Enemy Zone")]
    public SpriteRenderer enemyZone;           // Зона, до «верха» которой нельзя выстрелить дальше
    public float zoneTopPadding = 0.05f;       // Отступ сверху зоны, чтобы снаряды не упирались в пиксель
    public bool useZoneSteps = true;           // Дискретные шаги по высоте
    [Min(1)] public int zoneSteps = 3;         // Рабочие шаги (старый режим)
    [Min(0)] public int ignoredZoneSteps = 0;  // «Глухая» зона снизу (старый режим)
    public bool snapToCenters = false;         // Если true — целимся в центр ступени

    [Header("Manual Step Tuning (NEW)")]
    public bool manualStepTuning = true;       // Включает новый ручной режим ступеней
    [Min(1)] public int totalZoneSteps = 12;   // Сколько всего дискретных уровней от огневой точки до верха
    [Min(0)] public int ignoredStepsCommon = 4;// Сколько шагов снизу считаем «глухими» (новый режим)
    public int dot1ReachStep = 2;              // До какого шага долетает 1-я точка
    public int dot2ReachStep = 7;              // До какого шага долетает 2-я точка
    public int dot3ReachStep = 12;             // До какого шага долетает 3-я точка
    public bool manualAimCenters = false;      // (зарезервировано) прицельные центры ступеней
    public bool useStepRanges = true;          // Если true — каждая точка имеет свой диапазон (1:[1..dot1], 2:[dot1..dot2], 3:[dot2..dot3])

    // ===================== ELEMENTS =====================
    [Header("Element")]
    public ElementDefinition currentElement;
    public ElementDefinition[] availableElements; // заполни Fire и Ice из инспектора

    // ===================== MANA =====================
    [Header("Mana")]
    [Min(0)] public int manaCostPerShot = 5;   // Стоимость выстрела в очках маны

    // ===================== DEBUG =====================
    [Header("Debug")]
    [SerializeField] private bool debugLogThrow = false; // Включить простые Debug.Log по броску

    // ===================== STATE =====================
    public bool IsCharging => _isCharging;     // Публичный флаг «идёт заряд»
    public bool IsOnCooldown => Time.time < _cooldownUntil; // На перезарядке ли сейчас оружие

    // — ссылки на компоненты
    private SpriteRenderer _sr;                // Главный SpriteRenderer персонажа (ищем среди детей)
    private Animator _anim;                    // Animator на том же объекте, что _sr
    private Rigidbody2D _rb;                   // Физика персонажа (для остановки по X в замахе)
    private PlayerMovement _movement;          // Ваш скрипт движения (для определения FacingLeft)
    private PlayerHealth _hp;                  // Здоровье персонажа (нельзя стрелять, если мёртв)
    private PlayerMana _mana;                  // Мана (проверка и списание)

    // — временные поля процесса
    private Coroutine _chargeRoutine;          // Корутина накопления точек заряда
    private int _currentDots;                  // Сколько точек заряда набрано (1..maxDots)
    private bool _isCharging;                  // Идёт ли процесс заряда
    private bool _isMouseHeld;                 // Удерживается ли кнопка мыши
    private float _chargeElapsed;              // Сколько прошло времени с последней «ступени» заряда
    private bool _lockWindupSpriteWhileCharging = true; // Держать ли кадр замаха жёстко на экране

    // — «заморозка» аниматора (вместо отключения)
    private float _animPrevSpeed = 1f;         // Какая была скорость аниматора до фриза
    private bool _animFrozen = false;          // Сейчас аниматор «заморожен» (speed=0)?

    // — защита от «ложного клика» при переключении окна
    private float _blockClicksUntil = 0f;
    private const float FocusClickBlockSecs = 0.12f;

    // — фикс фейсинга (в какую сторону был повёрнут персонаж при старте замаха)
    private bool _facingLeftAtCharge;          // Истинный «фейсинг» на момент начала замаха
    private Vector3 _scaleBeforeCharge;        // На случай, если где-то используете scale для разворота
    // skills and elements compatibility
    private SkillDefinition ActiveSkill => currentElement ? currentElement.basicSkill : null;
    private int ManaCost => ActiveSkill ? ActiveSkill.manaCost : manaCostPerShot;      // совместимость со старым полем
    private float Cooldown => ActiveSkill ? ActiveSkill.cooldown : fireCooldown;       // совместимость со старым полем
    private GameObject ActiveProjectilePrefab => ActiveSkill ? ActiveSkill.projectilePrefab : playerFireballPrefab;

    // ===================== UNITY: поиск зависимостей =====================
    /// <summary>
    /// Вызывается Unity один раз при создании объекта.
    /// Находим основные компоненты и выбираем «главный» SpriteRenderer для замены спрайтов.
    /// </summary>
    private void Awake()
    {
        // Ищем среди детей SpriteRenderer. Предпочтение — тому, у кого есть Animator.
        // Иначе — с наибольшим sortingOrder (чтобы он был «над всеми», если Animator нет).
        var srs = GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer best = null;
        int bestOrder = int.MinValue;
        foreach (var sr in srs)
        {
            var an = sr.GetComponent<Animator>();
            if (an) { best = sr; break; }                   // нашёлся SR с Animator — берём его
            if (sr.sortingOrder > bestOrder)                // иначе ищем самый «верхний» по order
            { bestOrder = sr.sortingOrder; best = sr; }
        }
        _sr = best;
        _anim = _sr ? _sr.GetComponent<Animator>() : null;

        // Остальные компоненты на этом же объекте
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();
        _hp = GetComponent<PlayerHealth>();
        _mana = GetComponent<PlayerMana>();
    }

    // ===================== UNITY: начальная инициализация =====================
    /// <summary>
    /// Unity вызывает после Awake (и перед первым Update).
    /// Проставляем дефолты, включаем аниматор, готовим оверлей-спрайт (если он есть), чистим UI.
    /// </summary>
    private void Start()
    {
        if (chargeUI != null) chargeUI.Clear();
        if (_sr != null && idleSprite == null) idleSprite = _sr.sprite; // запасной idle из текущего кадра

        _blockClicksUntil = Time.unscaledTime + FocusClickBlockSecs;     // блок «ложного клика»

        if (_anim && !_anim.enabled) _anim.enabled = true;               // убеждаемся, что Animator включён
        _animFrozen = false;

        if (throwFlashRenderer) throwFlashRenderer.enabled = false;      // оверлей выключен по умолчанию

        EnsureThrowFlashRenderer();                                       // настроить сортировку/материал оверлея (если не None)
    }

    // ===================== UNITY: каждый кадр =====================
    /// <summary>
    /// Главный игровой цикл.
    /// Обрабатывает ввод, поднимает визуальные эффекты заряда и страхует потерю MouseUp.
    /// </summary>
    private void Update()
    {
        if (_hp != null && _hp.IsDead) return;                            // нельзя стрелять, если персонаж мёртв

        HandleInput();                                                    // 1) ввод

        // 2) визуальная «заливка» прогресса заряда (между точками)
        if (_isCharging && chargeFX != null)
        {
            bool canGrow = (_currentDots < maxDots);
            if (canGrow) _chargeElapsed += Time.deltaTime;

            // frac = [0..1] — прогресс к следующей точке
            float dotSpan = Mathf.Max(0.0001f, secondsPerDot);
            float frac = canGrow ? Mathf.Clamp01(_chargeElapsed / dotSpan) : 1f;

            // t ∈ [0..1] — общее значение для ауры (0 — 1 точка, 1 — полный заряд)
            float t;
            if (maxDots <= 1) t = 1f;
            else
            {
                float baseDots = Mathf.Clamp(_currentDots - 1, 0, maxDots - 1);
                t = Mathf.Clamp01((baseDots + frac) / (maxDots - 1));
            }
            chargeFX.UpdateCharge(t);                                     // качаем интенсивность FX
        }

        // 3) фикс: если замах активен и кнопка отпущена, но по каким-то причинам OnMouseUp «потерялся» — бросаем
        if (_isCharging && !Input.GetMouseButton(0))
        {
            ReleaseIfCharging();
        }
    }

    // ===================== UNITY: «после» всех Update =====================
    /// <summary>
    /// Из LateUpdate удобно насильно удерживать спрайт замаха поверх Animator (если тот что-то хочет менять).
    /// </summary>
    private void LateUpdate()
    {
        if (_hp != null && _hp.IsDead)
        {
            if (_isCharging) CancelCharge(false, true);
            return;
        }

        // Пока держим кнопку — жёстко показываем windupSprite (замах), чтобы аниматор не перебил кадр
        if (_isCharging && _lockWindupSpriteWhileCharging && _sr != null && windupSprite != null)
        {
            if (_sr.sprite != windupSprite)
                _sr.sprite = windupSprite;
        }
    }
    // Метод смены стихии (публичный, для UI):
    public void SetElement(ElementDefinition elem, ChargeDotsCooldownUI cooldownUi = null)
    {
        currentElement = elem;
        // Обновим оверлей кулдауна (ледяные «осколки» вместо огненных)
        if (cooldownUi) cooldownUi.ApplyElement(elem);
    }

    // ===================== UNITY: смена фокуса окна =====================
    /// <summary>
    /// Если теряем фокус — отменяем удержание мыши и отменяем заряд (чтобы не залипло).
    /// При получении фокуса — ставим небольшой блок на клики.
    /// </summary>
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

    // ===================== UNITY: пауза приложения =====================
    /// <summary>
    /// Аналогично фокусу: при паузе сбрасываем удержание и отменяем заряд, чтобы состояние не зависло.
    /// </summary>
    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _isMouseHeld = false;
            if (_isCharging) CancelCharge(true, true);
        }
    }

    // ===================== INPUT =====================
    /// <summary>
    /// Обрабатывает только нажатие/отжатие мыши.
    /// Важно: реальный «выстрел» всегда делаем в ReleaseIfCharging() (при отпускании).
    /// </summary>
    private void HandleInput()
    {
        // Защита от «ложного клика» (после переключения окна Windows может послать MouseUp/Down)
        if (Time.unscaledTime < _blockClicksUntil) return;

        if (Input.GetMouseButtonDown(0))
        {
            _isMouseHeld = true;
            StartCharging();                        // Начали держать — переходим в замах/заряд
        }
        if (Input.GetMouseButtonUp(0))
        {
            _isMouseHeld = false;
            ReleaseIfCharging();                    // Отпустили — пытаемся бросить
        }
    }

    /// <summary> True, если прошёл кулдаун. </summary>
    private bool IsCooldownReady() => Time.time >= _cooldownUntil;

    // ===================== ANIMATOR FREEZE =====================
    /// <summary>
    /// «Замораживает» аниматор: speed=0. Это лучше, чем отключать компонент,
    /// потому что Animator с disabled=true иногда теряет состояние/триггеры и даёт побочки.
    /// </summary>
    private void FreezeAnimator()
    {
        if (_anim && !_animFrozen)
        {
            if (!_anim.enabled) _anim.enabled = true; // если кто-то случайно выключил Animator — включим
            _animPrevSpeed = _anim.speed;
            _anim.speed = 0f;                          // «фризим»
            _animFrozen = true;
        }
    }

    /// <summary>
    /// Снимает «заморозку» с Animator — возвращает прежнюю скорость.
    /// </summary>
    private void UnfreezeAnimator()
    {
        if (_anim && _animFrozen)
        {
            _anim.speed = _animPrevSpeed;
            _animFrozen = false;
        }
    }

    // ===================== CHARGE FLOW =====================
    /// <summary>
    /// Переход в режим «замаха»: обнуляем прогресс, ставим кадр windup, фиксируем фейсинг, останавливаем горизонтальную скорость.
    /// </summary>
    private void StartCharging()
    {
        if (_hp != null && _hp.IsDead) return;       // мёртвые не стреляют
        if (!IsCooldownReady()) return;              // кулдаун ещё идёт
        if (_isCharging) return;                     // уже заряжаем — повторный вызов не нужен

        _isCharging = true;
        _currentDots = 1;                            // стартуем с 1 «точки силы»
        _chargeElapsed = 0f;

        if (chargeUI != null) { chargeUI.Clear(); chargeUI.SetCount(_currentDots); }

        FreezeAnimator();                            // замораживаем Animator, чтобы не мешал кадрам замаха/броска
        if (_sr != null && windupSprite != null) _sr.sprite = windupSprite;

        // Фиксируем «в какую сторону смотрели» на момент начала замаха.
        // Это важно, чтобы Windup и Throw были одной и той же стороной, даже если игрок развернулся во время удержания.
        _facingLeftAtCharge = (_movement != null) ? _movement.FacingLeft : (_sr && _sr.flipX);
        _scaleBeforeCharge = _sr ? _sr.transform.localScale : Vector3.one;

        if (_sr) _sr.flipX = _facingLeftAtCharge;    // жёстко выставляем нужный flipX

        // Важный момент: если firePoint зеркалится по X вместе с персонажем,
        // то при развороте он должен менять знак localPosition.x.
        if (firePoint)
        {
            var lp = firePoint.localPosition;
            lp.x = Mathf.Abs(lp.x) * (_facingLeftAtCharge ? -1f : 1f);
            firePoint.localPosition = lp;
        }

        // Останавливаем горизонтальное движение, чтобы «бег» не маскировал последующий кадр броска
        if (_rb) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        if (chargeFX != null) chargeFX.BeginCharge(); // визуальный старт FX
        _chargeRoutine = StartCoroutine(ChargeTick()); // корутина «накапливает» точки через равные интервалы
    }

    /// <summary>
    /// Корутина, которая каждые secondsPerDot добавляет «точку» заряда, пока удерживается кнопка.
    /// Может автосбрасывать выстрел (autoReleaseAtMax).
    /// </summary>
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

                // Если включён автосброс — стреляем сразу как достигли max
                if (autoReleaseAtMax && _currentDots >= maxDots)
                {
                    // подождём кадр — чтобы не конфликтовать с механизмом отпуска мыши
                    yield return null;
                    if (!Input.GetMouseButton(0)) ReleaseThrow();
                }
            }
        }
    }

    /// <summary>
    /// Безопасный «выстрел, если сейчас в замахе». Разделено на метод, чтобы вызывать из разных мест.
    /// </summary>
    private void ReleaseIfCharging()
    {
        if (!_isCharging) return;
        if (_hp != null && _hp.IsDead) { CancelCharge(false, true); return; }
        ReleaseThrow();
    }

    /// <summary>
    /// Финальный выстрел: проверка маны/КД, расчёт дальности (с учётом ступеней зоны),
    /// спавн префаба из активного скилла и короткая "вспышка" броска.
    /// Работает для любых стихий, если projectilePrefab содержит компонент IProjectile.
    /// </summary>
    private void ReleaseThrow()
    {
        // === 1) Проверка маны ДО списания (не уходим "в минус")
        if (_mana != null && ManaCost > 0 && !_mana.CanSpend(ManaCost))
        {
            CancelCharge(changeSprite: true, keepAnimatorDisabled: false);
            return;
        }

        // === 2) Проверка кулдауна
        if (!IsCooldownReady())
        {
            CancelCharge(true, false);
            return;
        }

        // === 3) Завершаем режим заряда
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);

        // Защита от NRE: без firePoint не можем корректно считать дистанцию/спавнить
        if (firePoint == null)
        {
            Debug.LogWarning("PlayerFireballShooter: FirePoint is not assigned.");
            CancelCharge(true, false);
            return;
        }

        // === 4) Считаем дальность/игнор первых метров (как у тебя было)
        int dots = Mathf.Clamp(_currentDots, 1, Mathf.Max(1, maxDots));
        float distance;
        float ignoreFirstMeters = 0f;

        if (shootAlwaysUp && enemyZone != null && useZoneSteps)
        {
            // Ступени внутри зоны
            if (manualStepTuning)
            {
                float top = enemyZone.bounds.max.y - zoneTopPadding; // верх "крыши"
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
                // Старый режим линейной интерполяции
                distance = Mathf.Lerp(minDistance, maxDistance, (dots - 1f) / (maxDots - 1f));
            }
        }
        else
        {
            // Без зоны/ступеней
            float t = (maxDots == 1) ? 1f : (dots - 1f) / (float)(maxDots - 1);
            distance = Mathf.Lerp(minDistance, maxDistance, t);
        }

        // === 5) Спавн снаряда активного скилла
        var prefab = ActiveProjectilePrefab;                   // ← берём из Element/Skill
        if (prefab != null && firePoint != null)
        {
            // Фактическое списание маны — здесь, перед самим спавном
            if (_mana != null && ManaCost > 0 && !_mana.TrySpend(ManaCost))
            {
                CancelCharge(true, false);
                return;
            }

            var go = Instantiate(prefab, firePoint.position, Quaternion.identity);

            // Поддержка любого типа: достаточно, чтобы компонент реализовал IProjectile
            var proj = go.GetComponent<IProjectile>();
            if (proj != null)
            {
                // Направление полёта:
                //  - если shootAlwaysUp == false — летим к курсору
                //  - если shootAlwaysUp == true и useFacingForHorizontal == true — строго влево/вправо
                //  - иначе — строго вверх
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

                // speedOverride = -1f → снаряд сам возьмёт свою скорость из компонента
                proj.Init(dir, distance, -1f, ignoreFirstMeters);
            }
        }

        // === 6) Ставим КД по активному скиллу
        _cooldownUntil = Time.time + Cooldown;

        // === 7) Короткая "вспышка" броска → назад в idle
        if (throwFlashRenderer != null && throwSprite != null)
            StartCoroutine(PlayThrowFlashAndBack());
        else if (_sr != null && throwSprite != null)
            StartCoroutine(PlayThrowAndBack());
        else
            BackToIdle();

        // === 8) Сброс визуала заряда
        if (chargeUI != null) chargeUI.Clear();
        _currentDots = 0;
        _chargeElapsed = 0f;

        if (chargeFX != null) chargeFX.Release();
    }


    // ===================== THROW FLASH =====================
    /// <summary>
    /// Настраиваем существующий throwFlashRenderer (если он задан), чтобы его сортировка/материал совпадали с основным SR.
    /// Если поле в инспекторе None — ничего не создаём (будет фолбэк без оверлея).
    /// </summary>
    private void EnsureThrowFlashRenderer()
    {
        // безопасно: если None, просто не настраиваем (дальше сработает фолбэк)
        if (!_sr || throwFlashRenderer == null) return;

        // Важно: одинаковые sortingLayer/Order/Material, чтобы оверлей реально был поверх и виден
        throwFlashRenderer.sortingLayerID = _sr.sortingLayerID;
        throwFlashRenderer.sortingLayerName = _sr.sortingLayerName;
        throwFlashRenderer.sortingOrder = _sr.sortingOrder + 100;
        throwFlashRenderer.renderingLayerMask = _sr.renderingLayerMask;
        throwFlashRenderer.sharedMaterial = _sr.sharedMaterial;

        throwFlashRenderer.sprite = null;
        throwFlashRenderer.color = Color.white;
        throwFlashRenderer.enabled = false;

        // Чуть ближе к камере (по Z) — на случай, если где-то используется сортировка по Z
        throwFlashRenderer.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        throwFlashRenderer.transform.localRotation = Quaternion.identity;
        throwFlashRenderer.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Вариант №1 (предпочтительный): показываем кадр броска на отдельном оверлейном SpriteRenderer.
    /// Аниматор на время отключаем, чтобы он не перезаписал кадр.
    /// </summary>
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

    /// <summary>
    /// Вариант №2 (фолбэк): если нет оверлея — просто меняем основной спрайт, аниматор временно выключаем.
    /// </summary>
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

    /// <summary>
    /// Возврат в позу idle, выключение оверлея, снятие «фриза» аниматора и возврат корректного flipX.
    /// </summary>
    private void BackToIdle()
    {
        if (_sr && idleSprite) _sr.sprite = idleSprite;
        if (throwFlashRenderer) throwFlashRenderer.enabled = false;

        // Возвращаем flipX согласно текущему реальному фейсингу из движения (если компонент есть)
        if (_sr) _sr.flipX = _movement ? _movement.FacingLeft : _sr.flipX;

        UnfreezeAnimator(); // снимаем «фриз» Animator'а
    }

    // ===================== CANCEL =====================
    /// <summary>
    /// Полная отмена замаха/заряда без выстрела (например, потеря фокуса, пауза, смерть).
    /// </summary>
    private void CancelCharge(bool changeSprite = true, bool keepAnimatorDisabled = false)
    {
        _isCharging = false;
        if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
        if (chargeUI != null) chargeUI.Clear();

        _currentDots = 0;
        _chargeElapsed = 0f;

        if (changeSprite && _sr && idleSprite) _sr.sprite = idleSprite;
        if (throwFlashRenderer) throwFlashRenderer.enabled = false;

        UnfreezeAnimator();              // вне зависимости от причин — снимаем «фриз»
        if (chargeFX != null) chargeFX.Cancel();
    }

    /// <summary>
    /// Служебный алиас: мгновенно отменяет всё, не меняя Animator-состояния (совместимость сигнатуры).
    /// </summary>
    public void CancelAllImmediate(bool keepAnimatorDisabled = false)
        => CancelCharge(false, keepAnimatorDisabled);

    // ===================== COOLDOWN NORMALIZED =====================
    /// <summary>
    /// Нормализованное значение перезарядки: 1 — только что выстрелили, 0 — готовы стрелять.
    /// Удобно для полосок/индикаторов КД.
    /// </summary>
    public float CooldownNormalized
    {
        get
        {
            float cd = Mathf.Max(0f, Cooldown);         // активный КД с учётом стихии/скилла
            if (cd <= 0f) return 0f;
            float remain = _cooldownUntil - Time.time;
            return Mathf.Clamp01(remain / cd);
        }
    }

}
