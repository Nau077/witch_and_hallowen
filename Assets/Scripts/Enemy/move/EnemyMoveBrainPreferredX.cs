using UnityEngine;

public class EnemyMoveBrainPreferredX : EnemyMoveBrainBase
{
    [Header("Preferred X distance")]
    public float preferredCellsFromPlayerX = 2f;
    public float toleranceCellsX = 0.35f;

    [Header("Vertical drift (optional)")]
    public bool addVerticalDrift = true;
    [Range(0f, 1f)] public float driftChanceOnDecide = 0.35f;

    private float _yDrift = 0f;

    public override void OnDecideTick()
    {
        if (!addVerticalDrift)
        {
            _yDrift = 0f;
            return;
        }

        // лёгкий дрейф по Y, чтобы ведьма "жила"
        if (Random.value < driftChanceOnDecide)
            _yDrift = (Random.value < 0.5f) ? 1f : -1f;
        else
            _yDrift = 0f;
    }

    public override Vector2 GetDesiredMoveDir()
    {
        if (brain == null || brain.PlayerTransform == null) return Vector2.zero;

        float cell = Mathf.Max(0.01f, brain.cellSize);
        float desired = Mathf.Max(0.01f, preferredCellsFromPlayerX * cell);
        float tol = Mathf.Max(0f, toleranceCellsX * cell);

        float dx = brain.PlayerTransform.position.x - brain.transform.position.x;
        float absDx = Mathf.Abs(dx);

        float xDir = 0f;
        if (absDx > desired + tol) xDir = Mathf.Sign(dx);          // подходим
        else if (absDx < desired - tol) xDir = -Mathf.Sign(dx);   // отходим
        else xDir = 0f;                                           // держим

        Vector2 d = new Vector2(xDir, _yDrift);
        if (d.sqrMagnitude < 0.000001f) return Vector2.zero;
        return d.normalized;
    }
}
