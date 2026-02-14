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
    [Range(0.1f, 1f)]
    public float beamMoveSpeedMultiplier = 0.55f;

    [Header("Beam size")]
    [Tooltip("Толщина луча в клетках.")]
    public float beamWidthInCells = 1f;

    [Header("Beam damage")]
    public int damagePerTick = 4;
    public float tickInterval = 0.12f;
    [Range(0f, 1f)] public float critChance = 0.25f;
    public float critMultiplier = 3f;

    [Header("Beam look")]
    public Color beamTint = new Color(0.9f, 0.2f, 1f, 1f);
    public string sortingLayer = "Effects";
    public int sortingOrder = 3;

    [Header("Vertical bounds (world Y)")]
    [Tooltip("Fallback-низ луча. Если -999 — берём brain.bottomLimit.")]
    public float bottomYOverride = -999f;

    [Header("Start under witch")]
    public float startBelowWitch = 0.05f;

    [Tooltip("Оставлено для совместимости, но геометрию мы теперь строим до ground.")]
    public float goBelowPlayerBy = 0.2f;

    [Header("Ground reach")]
    [Tooltip("Тянуть луч вниз до земли через Raycast.")]
    public bool useGroundRaycast = true;

    [Tooltip("Слой земли/пола. Если не выставишь — будет Raycast по всем слоям.")]
    public LayerMask groundMask;

    [Tooltip("Дальность Raycast вниз.")]
    public float groundRayDistance = 60f;

    [Tooltip("Смещение от точки хит-попадания (обычно 0).")]
    public float groundOffsetY = 0f;

    [Header("Debug")]
    public bool debugLogs = false;

    private Coroutine _routine;
    private BeamSpriteController _beam;

    private float _prevMoveSpeed = -1f;
    private bool _moveSpeedOverridden = false;

    private void Log(string msg)
    {
        if (!debugLogs) return;
    }

    public override void OnBrainAttackTick(int attackIndex, ref bool attackConsumed)
    {
        if (!CanUse(attackIndex, attackConsumed)) return;
        if (beamPrefab == null) { Log("SKIP: beamPrefab is null"); return; }
        if (brain == null || brain.PlayerTransform == null) { Log("SKIP: no brain/player"); return; }
        if (_routine != null) { Log("SKIP: already running"); return; }

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

        brain.SetExternalBusy(preBeamBlinkTime + revealDuration + beamChaseDuration + 0.2f);
        ApplyBeamMoveSpeed();

        yield return TelegraphBlink(preBeamBlinkTime);

        if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead)
        {
            StopAll();
            yield break;
        }

        SpriteRenderer sr = spriteRenderer;
        Sprite prevSprite = null;
        if (sr != null)
        {
            prevSprite = sr.sprite;
            if (brain.attackSprite != null)
                sr.sprite = brain.attackSprite;
        }

        _beam = Instantiate(beamPrefab, brain.transform);
        _beam.name = "BeamSprite_Runtime";

        // ✅ ВАЖНО: компенсируем scale родителя, иначе widthWorld "не ощущается".
        Vector3 parentScale = _beam.transform.parent != null ? _beam.transform.parent.lossyScale : Vector3.one;
        float invX = (Mathf.Abs(parentScale.x) > 0.0001f) ? (1f / parentScale.x) : 1f;
        float invY = (Mathf.Abs(parentScale.y) > 0.0001f) ? (1f / parentScale.y) : 1f;
        _beam.transform.localScale = new Vector3(invX, invY, 1f);

        float bottomY = (bottomYOverride != -999f) ? bottomYOverride : brain.bottomLimit;

        float widthWorld = Mathf.Max(0.05f, beamWidthInCells * Mathf.Max(0.01f, cellSize));
        float life = Mathf.Max(0.05f, revealDuration + beamChaseDuration);

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
            useGroundRaycast: useGroundRaycast,
            groundMask: groundMask,
            groundRayDistance: groundRayDistance,
            groundOffsetY: groundOffsetY
        );

        float t = 0f;
        while (t < beamChaseDuration)
        {
            if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead) break;
            t += Time.deltaTime;
            yield return null;
        }

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
