// Assets/Scripts/Enemy/move/EnemyMoveBrainWitch.cs
using UnityEngine;

public class EnemyMoveBrainWitch : EnemyMoveBrainBase
{
    [Header("Normal movement (when NOT beaming)")]
    [Tooltip("В обычном режиме ведьма ходит как остальные: рандомно.")]
    public bool respectSnapToGrid = true;

    [Tooltip("Смещение от границ по Y, чтобы не упираться.")]
    public float edgeBias = 0.2f;

    private Vector2 _normalDir = Vector2.zero;

    [Header("Beam movement (when beaming / externally busy)")]
    [Tooltip("Во время луча ведьма выравнивается по X с игроком (только X).")]
    public float beamXWeight = 2.0f;

    [Tooltip("Мёртвая зона по X (в клетках): если |dx| меньше — по X не двигаемся.")]
    public float beamXDeadzoneCells = 0.35f;

    [Tooltip("Гистерезис по X (в клетках), чтобы знак не щёлкал около deadzone.")]
    public float beamXHysteresisCells = 0.10f;

    [Tooltip("Размер клетки (для deadzone).")]
    public float cellSize = 1f;

    private float _cachedBeamXSign = 0f;

    public override void OnDecideTick()
    {
        if (brain == null) return;

        // --- NORMAL: рандомное направление как у остальных врагов ---
        int r = Random.Range(0, 4);
        Vector2 dir = r switch
        {
            0 => Vector2.right,
            1 => Vector2.left,
            2 => Vector2.up,
            _ => Vector2.down
        };

        // не упираемся в верх/низ
        float y = brain.transform.position.y;
        if (y > brain.topLimit - edgeBias) dir = Vector2.down;
        else if (y < brain.bottomLimit + edgeBias) dir = Vector2.up;

        _normalDir = dir;

        // если включён snap-to-grid — направляемся к “центру” следующей клетки
        if (respectSnapToGrid && brain.snapToGrid)
        {
            float step = brain.cellSize;
            Vector2 target = (Vector2)brain.transform.position + dir * step;
            target = brain.DebugClampToBounds(target);
            Vector2 d = target - (Vector2)brain.transform.position;
            _normalDir = d.sqrMagnitude > 0.000001f ? d.normalized : Vector2.zero;
        }
    }

    public override Vector2 GetDesiredMoveDir()
    {
        if (brain == null || brain.PlayerTransform == null)
            return Vector2.zero;

        bool beamingNow = brain.IsExternallyBusy;

        if (!beamingNow)
        {
            // NORMAL: просто отдаём закешированное рандом-направление
            return _normalDir;
        }

        // BEAM: только X к игроку, устойчиво (deadzone + hysteresis)
        Vector2 pos = brain.transform.position;
        Vector2 p = brain.PlayerTransform.position;
        float dx = p.x - pos.x;

        float dz = Mathf.Abs(beamXDeadzoneCells) * Mathf.Max(0.01f, cellSize);
        float hyst = Mathf.Abs(beamXHysteresisCells) * Mathf.Max(0.01f, cellSize);

        if (_cachedBeamXSign == 0f)
        {
            if (Mathf.Abs(dx) > (dz + hyst))
                _cachedBeamXSign = Mathf.Sign(dx);
        }
        else
        {
            if (Mathf.Abs(dx) < Mathf.Max(0f, dz - hyst))
                _cachedBeamXSign = 0f;
            else
                _cachedBeamXSign = Mathf.Sign(dx);
        }

        Vector2 dir = new Vector2(_cachedBeamXSign * beamXWeight, 0f);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
    }
}
