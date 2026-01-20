using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnergyBeamController : MonoBehaviour
{
    [Header("Refs (optional visuals)")]
    public SpriteRenderer spriteRenderer; // можно пустым, но удобно для тинта/ширины
    public LineRenderer lineRenderer;     // если хочешь вместо спрайта

    [Header("Runtime")]
    private Transform _owner;
    private Transform _player;

    private float _topY;
    private float _bottomY;

    private float _followSpeed;
    private float _beamWidth;
    private float _colliderWidth;

    private int _damagePerTick;
    private float _tickInterval;

    private float _critChance;
    private float _critMultiplier;

    private BoxCollider2D _col;
    private float _nextTickTime = -999f;

    private bool _playerInside;

    public void Setup(
        Transform owner,
        Transform player,
        float topY,
        float bottomY,
        float startX,
        float followSpeed,
        float beamWidth,
        float colliderWidth,
        Color tint,
        int damagePerTick,
        float tickInterval,
        float critChance,
        float critMultiplier
    )
    {
        _owner = owner;
        _player = player;

        _topY = Mathf.Max(topY, bottomY);
        _bottomY = Mathf.Min(topY, bottomY);

        _followSpeed = Mathf.Max(0.01f, followSpeed);
        _beamWidth = Mathf.Max(0.02f, beamWidth);
        _colliderWidth = Mathf.Max(0.02f, colliderWidth);

        _damagePerTick = Mathf.Max(1, damagePerTick);
        _tickInterval = Mathf.Max(0.02f, tickInterval);

        _critChance = Mathf.Clamp01(critChance);
        _critMultiplier = Mathf.Max(1f, critMultiplier);

        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true;

        // Позиция/размер
        float height = Mathf.Abs(_topY - _bottomY);
        float centerY = (_topY + _bottomY) * 0.5f;

        transform.position = new Vector3(startX, centerY, 0f);

        // Коллайдер = вертикальная полоса
        _col.size = new Vector2(_colliderWidth, height);
        _col.offset = Vector2.zero;

        // Визуал 1: SpriteRenderer (рекомендуется: Draw Mode = Tiled или Sliced)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = tint;

            // Если у спрайта включен Draw Mode != Simple — можно менять size.
            // Если Simple — size не работает, но это не сломает игру.
            try
            {
                spriteRenderer.size = new Vector2(_beamWidth, height);
            }
            catch { /* ignore */ }
        }

        // Визуал 2: LineRenderer (если используешь)
        if (lineRenderer != null)
        {
            lineRenderer.startColor = tint;
            lineRenderer.endColor = tint;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = _beamWidth;
            lineRenderer.endWidth = _beamWidth;

            lineRenderer.SetPosition(0, new Vector3(startX, _topY, 0f));
            lineRenderer.SetPosition(1, new Vector3(startX, _bottomY, 0f));
        }
    }

    private void Update()
    {
        if (_player == null) return;

        // Плавно ведём X к игроку
        float targetX = _player.position.x;
        float x = Mathf.MoveTowards(transform.position.x, targetX, _followSpeed * Time.deltaTime);

        float centerY = (_topY + _bottomY) * 0.5f;
        transform.position = new Vector3(x, centerY, 0f);

        // Обновляем линию, если она используется
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, new Vector3(x, _topY, 0f));
            lineRenderer.SetPosition(1, new Vector3(x, _bottomY, 0f));
        }

        // Дамаг тиками, пока игрок внутри луча
        if (_playerInside && Time.time >= _nextTickTime)
        {
            _nextTickTime = Time.time + _tickInterval;
            ApplyDamageTick();
        }
    }

    private void ApplyDamageTick()
    {
        if (_player == null) return;

        var hp = _player.GetComponent<PlayerHealth>();
        if (hp == null || hp.IsDead) return;

        int dmg = _damagePerTick;

        // "Крит" для игрока (у PlayerHealth нет встроенного крит-стаггера как у EnemyHealth)
        if (_critChance > 0f && Random.value <= _critChance)
        {
            dmg = Mathf.RoundToInt(dmg * _critMultiplier);
        }

        hp.TakeDamage(dmg);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;

        // чтобы урон был почти сразу при входе
        _nextTickTime = Mathf.Min(_nextTickTime, Time.time + 0.02f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;
    }
}
