// Assets/Scripts/Enemy/move/EnemyMoveBrainWitch.cs
using UnityEngine;

public class EnemyMoveBrainWitch : EnemyMoveBrainBase
{
    [Header("Normal movement (when NOT beaming)")]
    [Tooltip("Насколько ведьма тянется к игроку по X в обычном режиме.")]
    public float normalXWeight = 1.0f;

    [Tooltip("Случайный вертикальный дрейф (чтобы не стояла колом).")]
    public float normalYNoise = 0.35f;

    [Tooltip("Как часто менять случайное направление Y (сек).")]
    public float yRecalcEvery = 0.6f;

    [Header("Beam movement (when beaming / externally busy)")]
    [Tooltip("Во время луча ведьма выравнивается по X с игроком (только по оси X).")]
    public float beamXWeight = 2.0f;

    [Tooltip("Мёртвая зона по X (в клетках): если |dx| меньше — по X не двигаемся.")]
    public float beamXDeadzoneCells = 0.35f;

    [Tooltip("Размер клетки (для deadzone и опциональной stop-дистанции).")]
    public float cellSize = 1f;

    [Tooltip("Если игрок очень близко, можно полностью остановиться (0 = не останавливать). В клетках.")]
    public float stopDistanceCells = 0f;

    private float _nextYTime;
    private float _ySign = 1f;

    public override void OnDecideTick()
    {
        // обновляем “знак” вертикального дрейфа (для обычного режима)
        if (Time.time >= _nextYTime)
        {
            _nextYTime = Time.time + Mathf.Max(0.1f, yRecalcEvery);
            _ySign = (Random.value < 0.5f) ? -1f : 1f;
        }
    }

    public override Vector2 GetDesiredMoveDir()
    {
        if (brain == null || brain.PlayerTransform == null) return Vector2.zero;

        Vector2 pos = brain.transform.position;
        Vector2 p = brain.PlayerTransform.position;

        // опциональная общая стоп-дистанция
        if (stopDistanceCells > 0f)
        {
            float stopDist = stopDistanceCells * Mathf.Max(0.01f, cellSize);
            if (Vector2.Distance(pos, p) <= stopDist)
                return Vector2.zero;
        }

        bool beamingNow = brain.IsExternallyBusy; // true во время луча (SetExternalBusy)

        if (beamingNow)
        {
            // ВАЖНО: только по X. Y не используем для "погони".
            float dx = p.x - pos.x;

            float deadzone = Mathf.Abs(beamXDeadzoneCells) * Mathf.Max(0.01f, cellSize);
            float xSign = 0f;

            if (Mathf.Abs(dx) > deadzone)
                xSign = Mathf.Sign(dx);

            Vector2 dir = new Vector2(xSign * beamXWeight, 0f);
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        }
        else
        {
            // Обычный режим: немного тянемся к игроку по X + лёгкий случайный Y-дрейф
            float dx = Mathf.Sign(p.x - pos.x);
            Vector2 dir = new Vector2(dx * normalXWeight, _ySign * normalYNoise);

            // не упираемся в верх/низ (только для обычного режима, когда есть Y)
            float edgeBias = 0.15f;
            if (pos.y > brain.topLimit - edgeBias) dir.y = -Mathf.Abs(dir.y);
            if (pos.y < brain.bottomLimit + edgeBias) dir.y = Mathf.Abs(dir.y);

            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        }
    }
}
