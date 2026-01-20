using System.Collections;
using UnityEngine;

public class EnemySkillEnergyBeam : EnemySkillBase
{
    [Header("Beam Prefab")]
    public GameObject beamPrefab;

    [Tooltip("Если не задано — будет использоваться brain.topLimit.")]
    public float beamTopYOverride = 999f;

    [Tooltip("Если не задано — будет использоваться brain.bottomLimit.")]
    public float beamBottomYOverride = -999f;

    [Header("Trigger Distance (X)")]
    [Tooltip("Ведьма применяет луч, когда находится примерно на N клеток по X от игрока.")]
    public int desiredCellsFromPlayerX = 2;

    [Tooltip("Допуск по клеткам (например 0.6 = сработает в диапазоне 1.4..2.6 клеток).")]
    public float cellsTolerance = 0.6f;

    [Tooltip("Размер клетки (обычно 1).")]
    public float cellSize = 1f;

    [Header("Telegraph (Blink before beam)")]
    public float preBeamBlinkTime = 1.0f;
    public float blinkInterval = 0.12f;

    [Tooltip("Цвет мигания ведьмы перед выстрелом.")]
    public Color telegraphBlinkColor = new Color(1f, 0.2f, 1f, 1f);

    [Header("Channeling")]
    public float beamDuration = 4.0f;

    [Tooltip("Скорость, с которой луч \"догоняет\" X игрока (юнитов/сек).")]
    public float followSpeed = 12f;

    [Header("Beam Damage")]
    [Tooltip("Урон за тик (не каждый кадр, а раз в tickInterval).")]
    public int damagePerTick = 4;

    [Tooltip("Как часто наносить урон, пока игрок в луче.")]
    public float tickInterval = 0.12f;

    [Header("Beam Crit (player)")]
    [Range(0f, 1f)]
    public float critChance = 0.25f;

    [Tooltip("Множитель крит-урона для игрока.")]
    public float critMultiplier = 3.0f;

    [Header("Beam Look")]
    public Color beamTint = new Color(0.9f, 0.2f, 1f, 1f);
    public float beamWidth = 0.35f;
    public float colliderWidth = 0.45f;

    private Coroutine _routine;
    private EnergyBeamController _beam;

    public override void OnBrainAttackTick(int attackIndex, ref bool attackConsumed)
    {
        if (!CanUse(attackIndex, attackConsumed)) return;
        if (beamPrefab == null || brain == null || brain.PlayerTransform == null) return;
        if (_routine != null) return; // уже каналим

        // Триггер: ведьма на ~2 клетки слева/справа по X
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

        // Блокируем мозг на всю длительность: мигание + луч
        brain.SetExternalBusy(preBeamBlinkTime + beamDuration + 0.05f);

        // 1) Telegraph: мигаем ведьмой
        yield return TelegraphBlink(preBeamBlinkTime);

        if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead)
        {
            StopAll();
            yield break;
        }

        // 2) Спавним луч и инициализируем
        float topY = (beamTopYOverride != 999f) ? beamTopYOverride : brain.topLimit;
        float bottomY = (beamBottomYOverride != -999f) ? beamBottomYOverride : brain.bottomLimit;

        float startX = brain.PlayerTransform.position.x; // начинаем по игроку

        GameObject go = Instantiate(beamPrefab, Vector3.zero, Quaternion.identity);
        _beam = go.GetComponent<EnergyBeamController>();
        if (_beam == null)
        {
            Debug.LogWarning("EnemySkillEnergyBeam: beamPrefab должен иметь EnergyBeamController.");
            Destroy(go);
            _routine = null;
            yield break;
        }

        _beam.Setup(
            owner: brain.transform,
            player: brain.PlayerTransform,
            topY: topY,
            bottomY: bottomY,
            startX: startX,
            followSpeed: followSpeed,
            beamWidth: beamWidth,
            colliderWidth: colliderWidth,
            tint: beamTint,
            damagePerTick: damagePerTick,
            tickInterval: tickInterval,
            critChance: critChance,
            critMultiplier: critMultiplier
        );

        // 3) Каналим луч
        float t = 0f;
        while (t < beamDuration)
        {
            if (brain == null || selfHP == null || selfHP.IsDead || brain.PlayerIsDead) break;
            t += Time.deltaTime;
            yield return null;
        }

        // 4) Выключаем луч
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

        // вернуть базовый цвет
        spriteRenderer.color = baseCol;
    }
}
