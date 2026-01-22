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

    [Header("Size")]
    [Tooltip("Ширина луча в мире (world units).")]
    public float widthWorld = 1f;

    // runtime
    private Transform _owner;
    private Transform _player;

    private float _bottomY;
    private float _aliveUntil;
    private float _revealT;
    private float _nextTickTime;

    // config from skill
    private float _startBelowOwner = 0.05f;
    private float _goBelowPlayerBy = 0.2f;

    // cached
    private SpriteRenderer _ownerSR;
    private PlayerHealth _playerHP;
    private Collider2D _playerCol;

    // ======= NEW SIGNATURE (same file, no new files) =======
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
        float goBelowPlayerBy
    )
    {
        _owner = owner;
        _player = player;

        _bottomY = bottomY;

        _aliveUntil = Time.time + Mathf.Max(0.05f, lifetime);

        this.revealDuration = Mathf.Max(0.01f, revealDuration);
        this.widthWorld = Mathf.Max(0.05f, widthWorld);

        this.damagePerTick = Mathf.Max(1, damagePerTick);
        this.tickInterval = Mathf.Max(0.02f, tickInterval);
        this.critChance = Mathf.Clamp01(critChance);
        this.critMultiplier = Mathf.Max(1f, critMultiplier);

        _startBelowOwner = Mathf.Max(0f, startBelowOwner);
        _goBelowPlayerBy = Mathf.Max(0f, goBelowPlayerBy);

        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!col) col = GetComponent<BoxCollider2D>();

        sr.color = tint;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        // ✅ рисуем "полосой" предсказуемо
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
        {
            _playerHP = _player.GetComponent<PlayerHealth>();
            _playerCol = _player.GetComponent<Collider2D>();
        }

        UpdateBeamGeometry(instant: true);
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
        TryDamageTick();
    }

    private void UpdateBeamGeometry(bool instant)
    {
        // X всегда по ведьме
        float x = _owner.position.x;

        // ✅ старт луча: чуть ниже спрайта ведьмы (или ниже позиции, если SR не найден)
        float top = _owner.position.y;
        if (_ownerSR != null)
            top = _ownerSR.bounds.min.y - _startBelowOwner;
        else
            top = _owner.position.y - _startBelowOwner;

        // ✅ низ: минимум до игрока (чуть ниже), но НЕ выше заданного bottomY (clamp)
        float bottom = _bottomY;
        if (_player != null)
        {
            float want = _player.position.y - _goBelowPlayerBy;
            bottom = Mathf.Min(bottom, want);
        }

        // гарантируем направление сверху вниз
        if (bottom > top - 0.01f)
            bottom = top - 0.01f;

        // reveal 0..1
        if (instant) _revealT = revealDuration;
        else _revealT += Time.deltaTime;

        float reveal01 = Mathf.Clamp01(_revealT / Mathf.Max(0.0001f, revealDuration));

        float fullLen = Mathf.Abs(top - bottom);
        float curLen = Mathf.Max(0.05f, fullLen * reveal01);

        // рост сверху вниз: текущий "низ" приближается к bottom
        float curBottom = Mathf.Lerp(top, bottom, reveal01);

        // но если из-за clamp/ленты нужен строго по длине:
        // (оставляем Lerp — визуально правильнее, без скачков)
        float centerY = (top + curBottom) * 0.5f;

        transform.position = new Vector3(x, centerY, 0f);

        // размер спрайта и коллайдера совпадает
        float visibleLen = Mathf.Abs(top - curBottom);
        visibleLen = Mathf.Max(0.05f, visibleLen);

        sr.size = new Vector2(widthWorld, visibleLen);
        col.size = sr.size;
        col.offset = Vector2.zero;
    }

    private void TryDamageTick()
    {
        if (_playerHP == null || _playerHP.IsDead) return;
        if (Time.time < _nextTickTime) return;

        Vector2 center = transform.position;
        Vector2 size = sr.size;

        bool hit = false;

        if (_playerCol != null)
        {
            // bounds пересечение — стабильно (не зависит от слоёв/матрицы коллизий)
            hit = _playerCol.bounds.Intersects(new Bounds(center, new Vector3(size.x, size.y, 0.01f)));
        }
        else
        {
            // fallback
            var c = Physics2D.OverlapBox(center, size, 0f);
            hit = (c != null);
        }

        if (!hit) return;

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
        Gizmos.DrawWireCube(transform.position, new Vector3(sr.size.x, sr.size.y, 0.01f));
    }
#endif
}
