// Assets/Scripts/Enemy/skills/BeamSpriteController.cs
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BeamSpriteController : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer sr;
    public BoxCollider2D col;

    [Header("Timings")]
    public float revealDuration = 0.2f;

    [Header("Damage")]
    public int damagePerTick = 4;
    public float tickInterval = 0.12f;
    [Range(0f, 1f)] public float critChance = 0.25f;
    public float critMultiplier = 3f;

    [Header("Size (Visual)")]
    [Tooltip("Толщина (ширина) СПРАЙТА луча в мире (world units).")]
    public float widthWorld = 1f;

    [Header("Size (Damage)")]
    [Tooltip("Толщина (ширина) ЗОНЫ УРОНА по X в мире (world units). Если 0 — берём widthWorld.")]
    public float damageWidthWorld = 0f;

    [Header("Y Padding (extra height)")]
    [Tooltip("Добавить сверху к высоте луча (world units). Может быть отрицательным (чтобы укоротить).")]
    public float extraTopY = 0f;

    [Tooltip("Добавить снизу к высоте луча (world units). Может быть отрицательным (чтобы укоротить).")]
    public float extraBottomY = 0f;

    [Header("Beam flicker (energy look)")]
    public bool enableFlicker = true;
    [Range(0f, 60f)] public float flickerFrequency = 14f;
    [Range(0f, 1f)] public float flickerIntensity = 0.35f;
    [Range(0f, 1f)] public float flickerNoise = 0.15f;

    [Tooltip("Пульсация ширины (доля от widthWorld). Влияет ТОЛЬКО на визуал, не на дамаг.")]
    [Range(0f, 0.5f)]
    public float widthPulseAmount = 0.12f;

    // runtime
    private Transform _owner;
    private Transform _player;

    private float _bottomYFallback;
    private float _aliveUntil;
    private float _revealT;
    private float _nextTickTime;

    private float _startBelowOwner = 0.05f;

    // cached
    private SpriteRenderer _ownerSR;
    private PlayerHealth _playerHP;

    // flicker runtime
    private Color _baseTint;
    private float _flickerSeed;

    // ground raycast (minimal)
    private bool _useGroundRaycast = false;
    private LayerMask _groundMask;
    private float _groundRayDistance = 60f;
    private float _groundOffsetY = 0f;

    // ========= BACKWARD-COMPAT SETUP (как у тебя сейчас) =========
    public void Setup(
        Transform owner,
        Transform player,
        float bottomY,
        float lifetime,
        float revealDuration,
        float widthWorld,
        Color tint,
        int damagePerTick,
        float tickInterval,
        float critChance,
        float critMultiplier,
        string sortingLayer,
        int sortingOrder,
        float startBelowOwner,
        float goBelowPlayerBy // оставили, но урон теперь X-only, а низ берём из ground
    )
    {
        // по умолчанию без ground raycast
        Setup(
            owner, player, bottomY, lifetime, revealDuration, widthWorld, tint,
            damagePerTick, tickInterval, critChance, critMultiplier,
            sortingLayer, sortingOrder, startBelowOwner,
            useGroundRaycast: false,
            groundMask: default,
            groundRayDistance: 60f,
            groundOffsetY: 0f
        );
    }

    // ========= NEW SETUP (для ground) =========
    public void Setup(
        Transform owner,
        Transform player,
        float bottomY,
        float lifetime,
        float revealDuration,
        float widthWorld,
        Color tint,
        int damagePerTick,
        float tickInterval,
        float critChance,
        float critMultiplier,
        string sortingLayer,
        int sortingOrder,
        float startBelowOwner,
        bool useGroundRaycast,
        LayerMask groundMask,
        float groundRayDistance,
        float groundOffsetY
    )
    {
        _owner = owner;
        _player = player;

        _bottomYFallback = bottomY;
        _aliveUntil = Time.time + Mathf.Max(0.05f, lifetime);

        this.revealDuration = Mathf.Max(0.01f, revealDuration);
        this.widthWorld = Mathf.Max(0.05f, widthWorld);

        this.damagePerTick = Mathf.Max(1, damagePerTick);
        this.tickInterval = Mathf.Max(0.02f, tickInterval);
        this.critChance = Mathf.Clamp01(critChance);
        this.critMultiplier = Mathf.Max(1f, critMultiplier);

        _startBelowOwner = Mathf.Max(0f, startBelowOwner);

        _useGroundRaycast = useGroundRaycast;
        _groundMask = groundMask;
        _groundRayDistance = Mathf.Max(0.1f, groundRayDistance);
        _groundOffsetY = groundOffsetY;

        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!col) col = GetComponent<BoxCollider2D>();

        sr.color = tint;
        _baseTint = tint;
        _flickerSeed = Random.Range(0f, 9999f);

        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        // Важно для sr.size
        sr.drawMode = SpriteDrawMode.Tiled;

        col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        _revealT = 0f;
        _nextTickTime = Time.time;

        if (_owner != null)
            _ownerSR = _owner.GetComponent<SpriteRenderer>();

        if (_player != null)
            _playerHP = _player.GetComponent<PlayerHealth>();

        UpdateBeamGeometry(instant: true);
        ApplyDamageColliderWidth();
    }

    private void Update()
    {
        if (_owner == null)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= _aliveUntil)
        {
            Destroy(gameObject);
            return;
        }

        UpdateBeamGeometry(instant: false);
        ApplyFlickerVisual();
        TryDamageTick_XOnly();
    }

    private float ResolveBottomY()
    {
        if (_useGroundRaycast)
        {
            // если маска = 0, значит юзер не выставил — считаем "всё"
            int mask = (_groundMask.value == 0) ? ~0 : _groundMask.value;

            Vector2 origin = _owner.position;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, _groundRayDistance, mask);
            if (hit.collider != null)
                return hit.point.y + _groundOffsetY;
        }

        return _bottomYFallback;
    }

    private void UpdateBeamGeometry(bool instant)
    {
        // X всегда по ведьме
        float x = _owner.position.x;

        float top;
        if (_ownerSR != null)
            top = _ownerSR.bounds.min.y - _startBelowOwner;
        else
            top = _owner.position.y - _startBelowOwner;

        float bottom = ResolveBottomY();

        if (bottom > top - 0.01f)
            bottom = top - 0.01f;

        if (instant) _revealT = revealDuration;
        else _revealT += Time.deltaTime;

        float reveal01 = Mathf.Clamp01(_revealT / Mathf.Max(0.0001f, revealDuration));

        float curBottom = Mathf.Lerp(top, bottom, reveal01);

        float topPadded = top + extraTopY;
        float bottomPadded = curBottom - extraBottomY;

        if (bottomPadded > topPadded - 0.01f)
            bottomPadded = topPadded - 0.01f;

        float centerY = (topPadded + bottomPadded) * 0.5f;
        transform.position = new Vector3(x, centerY, 0f);

        float visibleLen = Mathf.Abs(topPadded - bottomPadded);
        visibleLen = Mathf.Max(0.05f, visibleLen);

        sr.size = new Vector2(widthWorld, visibleLen);

        float dmgW = (damageWidthWorld > 0f) ? damageWidthWorld : widthWorld;
        dmgW = Mathf.Max(0.05f, dmgW);

        col.size = new Vector2(dmgW, visibleLen);
        col.offset = Vector2.zero;
    }

    private void ApplyDamageColliderWidth()
    {
        if (col == null) return;

        float dmgW = (damageWidthWorld > 0f) ? damageWidthWorld : widthWorld;
        dmgW = Mathf.Max(0.05f, dmgW);

        col.size = new Vector2(dmgW, col.size.y);
    }

    private void ApplyFlickerVisual()
    {
        if (!enableFlicker || sr == null)
        {
            if (sr != null) sr.color = _baseTint;
            return;
        }

        float freq = Mathf.Max(0f, flickerFrequency);
        float t = Time.time;

        float s01 = 0.5f + 0.5f * Mathf.Sin((t + _flickerSeed) * Mathf.PI * 2f * freq);
        float n01 = Mathf.PerlinNoise(_flickerSeed, t * (freq * 0.35f + 0.01f));

        float k = 1f + (s01 - 0.5f) * 2f * flickerIntensity;
        k += (n01 - 0.5f) * 2f * flickerNoise * 0.5f;
        k = Mathf.Clamp(k, 0.2f, 2.0f);

        Color c = _baseTint;
        c.r = Mathf.Clamp01(_baseTint.r * k);
        c.g = Mathf.Clamp01(_baseTint.g * k);
        c.b = Mathf.Clamp01(_baseTint.b * k);
        sr.color = c;

        if (widthPulseAmount > 0f)
        {
            float pulse = 1f + (s01 - 0.5f) * 2f * widthPulseAmount;
            float w = Mathf.Max(0.05f, widthWorld * pulse);
            sr.size = new Vector2(w, sr.size.y);
        }
    }

    private void TryDamageTick_XOnly()
    {
        if (_playerHP == null || _playerHP.IsDead) return;
        if (_player == null) return;
        if (Time.time < _nextTickTime) return;

        float dmgW = (damageWidthWorld > 0f) ? damageWidthWorld : widthWorld;
        dmgW = Mathf.Max(0.05f, dmgW);

        float half = dmgW * 0.5f;

        float beamX = _owner.position.x;
        float playerX = _player.position.x;

        if (Mathf.Abs(playerX - beamX) > half) return;

        _nextTickTime = Time.time + tickInterval;

        int dmg = damagePerTick;
        if (Random.value < critChance)
            dmg = Mathf.RoundToInt(dmg * critMultiplier);

        _playerHP.TakeDamage(dmg);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<BoxCollider2D>();

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(col.size.x, 8f, 0.01f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(sr.size.x, sr.size.y, 0.01f));
    }
#endif
}
