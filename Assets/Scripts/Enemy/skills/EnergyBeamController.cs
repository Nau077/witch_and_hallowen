using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnergyBeamController : MonoBehaviour
{
    [Header("Visual")]
    public LineRenderer lineRenderer;

    private Transform _owner;
    private Transform _player;

    private float _topY;
    private float _bottomYFinal;

    private float _cellSize;
    private float _beamCellsWidth;

    private float _revealDuration;
    private float _revealT;

    private float _followSpeed;

    private int _damagePerTick;
    private float _tickInterval;
    private float _critChance;
    private float _critMultiplier;

    private BoxCollider2D _col;
    private float _nextTickTime;

    private bool _playerInsideThisFrame;

    private enum State { Revealing, Active }
    private State _state = State.Revealing;

    public void Setup(
        Transform owner,
        Transform player,
        float topY,
        float bottomYFinal,
        float startX,

        float cellSize,
        float beamCellsWidth,

        float revealDuration,
        float followSpeed,

        int damagePerTick,
        float tickInterval,
        float critChance,
        float critMultiplier,

        Color color
    )
    {
        _owner = owner;
        _player = player;

        _topY = topY;
        _bottomYFinal = bottomYFinal;

        _cellSize = Mathf.Max(0.01f, cellSize);
        _beamCellsWidth = Mathf.Max(0.1f, beamCellsWidth);

        _revealDuration = Mathf.Max(0.01f, revealDuration);
        _followSpeed = followSpeed;

        _damagePerTick = Mathf.Max(1, damagePerTick);
        _tickInterval = Mathf.Max(0.02f, tickInterval);
        _critChance = Mathf.Clamp01(critChance);
        _critMultiplier = Mathf.Max(1f, critMultiplier);

        // collider
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;

        // Rigidbody2D for reliable trigger callbacks
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // LineRenderer
        if (!lineRenderer)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        lineRenderer.material = new Material(shader);

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        float widthWorld = _beamCellsWidth * _cellSize;
        lineRenderer.startWidth = widthWorld;
        lineRenderer.endWidth = widthWorld;

        // make sure visible in 2D
        lineRenderer.sortingLayerName = "Effects";
        lineRenderer.sortingOrder = 200;

        // initial position (zero-length beam)
        transform.position = new Vector3(startX, _topY, -1f);
        UpdateBeamVisual(startX, _topY, _topY);
        UpdateCollider(startX, _topY, _topY);

        _state = State.Revealing;
        _revealT = 0f;
        _nextTickTime = Time.time;
    }

    private void Update()
    {
        if (!_player) return;

        // reset per-frame inside flag, will be set by OnTriggerStay2D
        _playerInsideThisFrame = false;

        float targetX = _player.position.x;
        float x = Mathf.MoveTowards(transform.position.x, targetX, _followSpeed * Time.deltaTime);

        switch (_state)
        {
            case State.Revealing:
                UpdateReveal(x);
                break;
            case State.Active:
                UpdateActive(x);
                break;
        }
    }

    private void LateUpdate()
    {
        // after physics callbacks, if player is inside this frame -> damage ticks
        if (_state != State.Active) return;
        if (!_playerInsideThisFrame) return;

        if (Time.time >= _nextTickTime)
        {
            _nextTickTime = Time.time + _tickInterval;
            DealDamage();
        }
    }

    private void UpdateReveal(float x)
    {
        _revealT += Time.deltaTime / _revealDuration;
        float t = Mathf.Clamp01(_revealT);

        float currentBottomY = Mathf.Lerp(_topY, _bottomYFinal, t);

        UpdateBeamVisual(x, _topY, currentBottomY);
        UpdateCollider(x, _topY, currentBottomY);

        // keep transform aligned with top point
        transform.position = new Vector3(x, _topY, -1f);

        if (t >= 1f)
        {
            _state = State.Active;
        }
    }

    private void UpdateActive(float x)
    {
        UpdateBeamVisual(x, _topY, _bottomYFinal);
        UpdateCollider(x, _topY, _bottomYFinal);

        transform.position = new Vector3(x, _topY, -1f);
    }

    private void UpdateBeamVisual(float x, float topY, float bottomY)
    {
        Vector3 top = new Vector3(x, topY, -1f);
        Vector3 bottom = new Vector3(x, bottomY, -1f);

        lineRenderer.SetPosition(0, top);
        lineRenderer.SetPosition(1, bottom);
    }

    private void UpdateCollider(float x, float topY, float bottomY)
    {
        float height = Mathf.Abs(topY - bottomY);
        float centerY = (topY + bottomY) * 0.5f;

        _col.size = new Vector2(_beamCellsWidth * _cellSize, Mathf.Max(0.01f, height));
        // collider local offset относительно transform.position (который стоит в topY)
        _col.offset = new Vector2(0f, centerY - transform.position.y);
    }

    private void DealDamage()
    {
        if (!_player) return;

        var hp = _player.GetComponent<PlayerHealth>();
        if (!hp || hp.IsDead) return;

        int dmg = _damagePerTick;
        if (Random.value < _critChance)
            dmg = Mathf.RoundToInt(dmg * _critMultiplier);

        hp.TakeDamage(dmg);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInsideThisFrame = true;
    }
}
