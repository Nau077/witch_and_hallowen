using UnityEngine;

public class EnemyMoveBrainRandom : EnemyMoveBrainBase
{
    [Tooltip("Если true — используем snap-to-grid логику мозга.")]
    public bool respectSnapToGrid = true;

    private Vector2 _dir = Vector2.zero;

    public override void OnDecideTick()
    {
        if (brain == null) return;

        int r = Random.Range(0, 4);
        Vector2 dir = r switch
        {
            0 => Vector2.right,
            1 => Vector2.left,
            2 => Vector2.up,
            _ => Vector2.down
        };

        float y = brain.transform.position.y;
        const float edgeBias = 0.2f;
        if (y > brain.topLimit - edgeBias) dir = Vector2.down;
        else if (y < brain.bottomLimit + edgeBias) dir = Vector2.up;

        _dir = dir;

        if (respectSnapToGrid && brain.snapToGrid)
        {
            float step = brain.cellSize;
            Vector2 target = (Vector2)brain.transform.position + dir * step;
            target = brain.DebugClampToBounds(target); // см. ниже про helper
            Vector2 d = target - (Vector2)brain.transform.position;
            _dir = d.sqrMagnitude > 0.000001f ? d.normalized : Vector2.zero;
        }
    }

    public override Vector2 GetDesiredMoveDir()
    {
        return _dir;
    }
}
