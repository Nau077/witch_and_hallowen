// Assets/Scripts/Enemy/skills/EnemySkillEnergyBeam.cs
using System.Collections;
using UnityEngine;

public class EnemySkillEnergyBeam : EnemySkillBase
{
    [Header("Trigger Distance (X)")]
    public int desiredCellsFromPlayerX = 2;
    public float cellsTolerance = 0.6f;
    public float cellSize = 1f;

    [Header("Telegraph (Blink before beam)")]
    public float preBeamBlinkTime = 1.0f;
    public float blinkInterval = 0.12f;
    public Color telegraphBlinkColor = new Color(1f, 0.2f, 1f, 1f);

    [Header("Reveal + Channeling")]
    public float revealDuration = 0.35f;
    public float beamDuration = 4.0f;
    public float followSpeed = 12f;

    [Header("Beam Size (Grid-based)")]
    public float beamWidthInCells = 1f;

    [Header("Beam Damage")]
    public int damagePerTick = 4;
    public float tickInterval = 0.12f;

    [Header("Beam Crit (player)")]
    [Range(0f, 1f)] public float critChance = 0.25f;
    public float critMultiplier = 3.0f;

    [Header("Beam Look")]
    public Color beamTint = new Color(0.9f, 0.2f, 1f, 1f);

    [Header("Vertical Bounds")]
    public float beamTopYOverride = 999f;
    public float beamBottomYOverride = -999f;

    private Coroutine _routine;
    private EnergyBeamController _beam;

    public override void OnBrainAttackTick(int attackIndex, ref bool attackConsumed)
    {
        if (!CanUse(attackIndex, attackConsumed)) return;
        if (brain == null || brain.PlayerTransform == null) return;
        if (_routine != null) return;

        float dx = Mathf.Abs(brain.PlayerTransform.position.x - brain.transform.position.x);
        float desired = Mathf.Max(0.01f, desiredCellsFromPlayerX * cellSize);
        float tol = Mathf.Max(0f, cellsTolerance * cellSize);

        if (dx < desired - tol) return;
        if (dx > desired + tol) return;

        attackConsumed = true;
        _routine = StartCoroutine(BeamRoutine());
    }

    public override void OnBrainAttackInterrupted()
    {
        StopAll();
    }

    private void StopAll()
    {
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

        // держим мозг "занятым" (не стартуем другие атаки)
        brain.SetExternalBusy(preBeamBlinkTime + revealDuration + beamDuration + 0.15f);

        // 1) telegraph
        yield return TelegraphBlink(preBeamBlinkTime);

        if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead)
        {
            StopAll();
            yield break;
        }

        // 2) bounds
        float topY = (beamTopYOverride != 999f) ? beamTopYOverride : brain.topLimit;
        float bottomY = (beamBottomYOverride != -999f) ? beamBottomYOverride : brain.bottomLimit;

        // 3) spawn runtime beam
        GameObject go = new GameObject("EnergyBeam_Runtime");
        _beam = go.AddComponent<EnergyBeamController>();

        _beam.Setup(
            owner: brain.transform,
            player: brain.PlayerTransform,
            topY: topY,
            bottomYFinal: bottomY,
            startX: brain.PlayerTransform.position.x,

            cellSize: cellSize,
            beamCellsWidth: beamWidthInCells,

            revealDuration: revealDuration,
            followSpeed: followSpeed,

            damagePerTick: damagePerTick,
            tickInterval: tickInterval,
            critChance: critChance,
            critMultiplier: critMultiplier,

            color: beamTint
        );

        // 4) wait total lifetime
        float total = Mathf.Max(0.01f, revealDuration + beamDuration);
        float t = 0f;
        while (t < total)
        {
            if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead) break;
            t += Time.deltaTime;
            yield return null;
        }

        // 5) cleanup
        if (_beam != null)
        {
            Destroy(_beam.gameObject);
            _beam = null;
        }

        _routine = null;
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
