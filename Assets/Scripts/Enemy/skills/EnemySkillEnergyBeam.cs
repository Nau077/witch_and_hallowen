// Assets/Scripts/Enemy/skills/EnemySkillEnergyBeam.cs
using System.Collections;
using UnityEngine;

public class EnemySkillEnergyBeam : EnemySkillBase
{
    [Header("Beam Prefab (REQUIRED)")]
    public BeamSpriteController beamPrefab;

    [Header("Cell size (for width etc.)")]
    [Tooltip("Размер клетки.")]
    public float cellSize = 1f;

    [Header("Telegraph")]
    public float preBeamBlinkTime = 0.6f;
    public float blinkInterval = 0.12f;
    public Color telegraphBlinkColor = new Color(1f, 0.2f, 1f, 1f);

    [Header("Beam timings")]
    public float revealDuration = 0.20f;
    public float beamChaseDuration = 3.5f;

    [Header("Beam movement (speed while beaming)")]
    [Tooltip("Множитель скорости ведьмы на время луча. 1 = без изменений, 0.5 = в 2 раза медленнее.")]
    [Range(0.1f, 1f)]
    public float beamMoveSpeedMultiplier = 0.55f;

    [Header("Beam size")]
    [Tooltip("Толщина луча в клетках. По умолчанию 1.")]
    public float beamWidthInCells = 1f;

    [Header("Beam damage")]
    public int damagePerTick = 4;
    public float tickInterval = 0.12f;
    [Range(0f, 1f)] public float critChance = 0.25f;
    public float critMultiplier = 3f;

    [Header("Beam look")]
    public Color beamTint = new Color(0.9f, 0.2f, 1f, 1f);
    public string sortingLayer = "Effects";
    public int sortingOrder = 250;

    [Header("Vertical bounds (world Y)")]
    [Tooltip("Низ луча. Если -999 — берём brain.bottomLimit.")]
    public float bottomYOverride = -999f;

    [Header("Start under witch")]
    [Tooltip("Насколько ниже нижней границы спрайта ведьмы начинать луч (world units).")]
    public float startBelowWitch = 0.05f;

    [Tooltip("Насколько ниже игрока должен доходить луч (world units).")]
    public float goBelowPlayerBy = 0.2f;

    [Header("Debug")]
    public bool debugLogs = false;

    private Coroutine _routine;
    private BeamSpriteController _beam;

    // speed override
    private float _prevMoveSpeed = -1f;
    private bool _moveSpeedOverridden = false;

    private void Log(string msg)
    {
        if (!debugLogs) return;
        Debug.Log($"[EnemySkillEnergyBeam] {msg}", this);
    }

    public override void OnBrainAttackTick(int attackIndex, ref bool attackConsumed)
    {
        if (!CanUse(attackIndex, attackConsumed)) return;
        if (beamPrefab == null) { Log("SKIP: beamPrefab is null"); return; }
        if (brain == null || brain.PlayerTransform == null) { Log("SKIP: no brain/player"); return; }
        if (_routine != null) { Log("SKIP: already running"); return; }

        // ❌ УБРАНО: ограничение по расстоянию. Beam может стартовать всегда, если CanUse разрешил.

        attackConsumed = true;
        _routine = StartCoroutine(BeamRoutine());
    }

    public override void OnBrainAttackInterrupted()
    {
        StopAll();
    }

    private void ApplyBeamMoveSpeed()
    {
        if (brain == null) return;
        if (_moveSpeedOverridden) return;

        _prevMoveSpeed = brain.moveSpeed;
        float mult = Mathf.Clamp(beamMoveSpeedMultiplier, 0.1f, 1f);
        brain.moveSpeed = _prevMoveSpeed * mult;

        _moveSpeedOverridden = true;
    }

    private void RestoreMoveSpeed()
    {
        if (brain == null) return;
        if (!_moveSpeedOverridden) return;

        if (_prevMoveSpeed > 0f)
            brain.moveSpeed = _prevMoveSpeed;

        _prevMoveSpeed = -1f;
        _moveSpeedOverridden = false;
    }

    private void StopAll()
    {
        // ✅ всегда откатываем скорость
        RestoreMoveSpeed();

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (_beam != null)
        {
            Destroy(_beam.gameObject);
            _beam = null;
        }

        if (brain != null)
            brain.ClearExternalBusy();
    }

    private IEnumerator BeamRoutine()
    {
        if (brain == null || brain.PlayerTransform == null)
        {
            _routine = null;
            yield break;
        }

        Log("START BeamRoutine");

        // во время луча ведьма может двигаться (на EnemyWalker у ведьмы allowMoveWhileExternallyBusy = true)
        brain.SetExternalBusy(preBeamBlinkTime + revealDuration + beamChaseDuration + 0.2f);

        // ✅ замедляем ведьму на всё время луча (включая телеграф)
        ApplyBeamMoveSpeed();

        // 1) Telegraph
        yield return TelegraphBlink(preBeamBlinkTime);

        if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead)
        {
            StopAll();
            yield break;
        }

        // ✅ держим спрайт атаки на время луча (если задан)
        SpriteRenderer sr = spriteRenderer;
        Sprite prevSprite = null;
        if (sr != null)
        {
            prevSprite = sr.sprite;
            if (brain.attackSprite != null)
                sr.sprite = brain.attackSprite;
        }

        // 2) Спавним луч child-объектом ведьмы (X будет следовать за ведьмой)
        _beam = Instantiate(beamPrefab, brain.transform);
        _beam.name = "BeamSprite_Runtime";

        float bottomY = (bottomYOverride != -999f) ? bottomYOverride : brain.bottomLimit;

        float widthWorld = Mathf.Max(0.05f, beamWidthInCells * Mathf.Max(0.01f, cellSize));
        float life = Mathf.Max(0.05f, revealDuration + beamChaseDuration);

        // ВАЖНО: теперь луч строится от нижней границы спрайта ведьмы вниз до игрока.
        _beam.Setup(
            owner: brain.transform,
            player: brain.PlayerTransform,
            bottomY: bottomY,
            lifetime: life,
            revealDuration: revealDuration,
            widthWorld: widthWorld,
            tint: beamTint,
            damagePerTick: damagePerTick,
            tickInterval: tickInterval,
            critChance: critChance,
            critMultiplier: critMultiplier,
            sortingLayer: sortingLayer,
            sortingOrder: sortingOrder,
            startBelowOwner: Mathf.Max(0f, startBelowWitch),
            goBelowPlayerBy: Mathf.Max(0f, goBelowPlayerBy)
        );

        // 3) Пока горит — EnemyMoveBrainWitch будет выравниваться по X (по IsExternallyBusy)
        float t = 0f;
        while (t < beamChaseDuration)
        {
            if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead) break;
            t += Time.deltaTime;
            yield return null;
        }

        // 4) cleanup
        if (_beam != null)
        {
            Destroy(_beam.gameObject);
            _beam = null;
        }

        if (sr != null)
        {
            if (brain != null && brain.idleSprite != null) sr.sprite = brain.idleSprite;
            else if (prevSprite != null) sr.sprite = prevSprite;
        }

        // ✅ откат скорости в норму
        RestoreMoveSpeed();

        _routine = null;
        Log("END BeamRoutine");
    }

    private IEnumerator TelegraphBlink(float totalTime)
    {
        if (spriteRenderer == null || totalTime <= 0f) yield break;

        Color baseCol = spriteRenderer.color;
        float elapsed = 0f;
        bool on = false;

        float interval = Mathf.Max(0.02f, blinkInterval);

        while (elapsed < totalTime)
        {
            if (selfHP != null && selfHP.IsDead) break;
            if (brain != null && brain.PlayerIsDead) break;

            on = !on;
            spriteRenderer.color = on ? telegraphBlinkColor : baseCol;

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        spriteRenderer.color = baseCol;
    }
}
