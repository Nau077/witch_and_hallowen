using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerHealth hp;
    [SerializeField] private SpriteRenderer sr; // обычно child renderer

    [Header("Input")]
    public KeyCode dashKey = KeyCode.Space;

    [Header("Dash distances (cells) by level")]
    public float cellSize = 1f;
    public float cellsLevel1 = 4f; // дефолт 4 клетки
    public float cellsLevel2 = 5f;
    public float cellsLevel3 = 6f;

    [Header("Dash motion")]
    [Tooltip("Длительность рывка (сек).")]
    public float dashDuration = 0.16f;

    [Tooltip("Если true — во время дэша блокируем обычное движение PlayerMovement.")]
    public bool disableMovementWhileDashing = true;

    [Header("Dash energy (cooldown resource)")]
    public float maxEnergy = 50f;
    public float dashCost = 35f;
    public float regenPerSecond = 18f;

    [Header("Visuals")]
    public Sprite broomDashSprite; // спрайт ведьмы на метле
    public bool restoreSpriteAfterDash = true;

    public Color dashTintColor = new Color(0.35f, 0.85f, 1f, 1f);
    public float blinkSpeed = 22f;
    [Range(0f, 1f)] public float blinkIntensity = 0.65f;

    [Header("Reflect hook (later)")]
    public string enemyProjectileTag = "EnemyProjectile";

    public bool IsDashing { get; private set; }
    public float EnergyNormalized => maxEnergy <= 0 ? 0 : Mathf.Clamp01(currentEnergy / maxEnergy);
    public float CurrentEnergy => currentEnergy;

    private float currentEnergy;

    private Sprite originalSprite;
    private Color originalColor;

    private Coroutine dashRoutine;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        hp = GetComponent<PlayerHealth>();
        sr = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!movement) movement = GetComponent<PlayerMovement>();
        if (!hp) hp = GetComponent<PlayerHealth>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);

        if (sr)
        {
            originalSprite = sr.sprite;
            originalColor = sr.color;
        }

        currentEnergy = maxEnergy;
    }

    private void Update()
    {
        if (hp != null && hp.IsDead) return;

        if (RunLevelManager.Instance != null && !RunLevelManager.Instance.CanProcessGameplayInput())
            return;

        RegenEnergy();

        if (IsDashing) return;

        if (Input.GetKeyDown(dashKey))
        {
            TryDash();
        }
    }

    private void RegenEnergy()
    {
        if (maxEnergy <= 0f) return;
        if (regenPerSecond <= 0f) return;

        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + regenPerSecond * Time.deltaTime);
    }

    public bool CanDashNow()
    {
        if (IsDashing) return false;
        if (hp != null && hp.IsDead) return false;

        if (RunLevelManager.Instance != null && !RunLevelManager.Instance.CanProcessGameplayInput())
            return false;

        return currentEnergy >= dashCost;
    }

    public void TryDash()
    {
        if (!CanDashNow()) return;

        currentEnergy = Mathf.Max(0f, currentEnergy - dashCost);

        float dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir = 1f;

        if (Mathf.Approximately(dir, 0f))
        {
            bool facingLeft = movement != null && movement.FacingLeft;
            dir = facingLeft ? -1f : 1f;
        }

        float cells = GetCellsByPerkLevel();
        float worldDistance = cells * Mathf.Max(0.01f, cellSize);

        if (dashRoutine != null) StopCoroutine(dashRoutine);
        dashRoutine = StartCoroutine(DashRoutine(dir, worldDistance));
    }

    private float GetCellsByPerkLevel()
    {
        int lvl = 1;
        var perks = SoulPerksManager.Instance;
        if (perks != null) lvl = perks.GetDashRealLevel();

        return lvl switch
        {
            1 => cellsLevel1,
            2 => cellsLevel2,
            3 => cellsLevel3,
            _ => cellsLevel1
        };
    }

    private IEnumerator DashRoutine(float dir, float worldDistance)
    {
        IsDashing = true;

        if (disableMovementWhileDashing && movement != null)
            movement.enabled = false;

        if (hp != null) hp.SetInvulnerable(true);

        if (sr != null)
        {
            originalSprite = sr.sprite;
            originalColor = sr.color;

            // фикс: флип по направлению дэша (чтобы метла смотрела корректно)
            if (movement != null)
            {
                bool facingLeft = dir < 0f;
                bool lockFlip = false; // movement выключен, так что флип не меняется сам
                if (!lockFlip)
                {
                    bool needFlip = movement.baseSpriteFacesRight ? facingLeft : !facingLeft;
                    sr.flipX = needFlip;
                }
            }

            if (broomDashSprite != null)
                sr.sprite = broomDashSprite;
        }

        Vector2 startPos = rb.position;
        Vector2 targetPos = startPos + new Vector2(dir * worldDistance, 0f);

        float leftLimit = movement != null ? movement.leftLimit : -999f;
        float rightLimit = movement != null ? movement.rightLimit : 999f;

        targetPos.x = Mathf.Clamp(targetPos.x, leftLimit, rightLimit);

        float t = 0f;

        var prevCd = rb.collisionDetectionMode;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        while (t < 1f)
        {
            if (hp != null && hp.IsDead) break;

            t += (dashDuration <= 0.01f ? 999f : Time.deltaTime / dashDuration);

            Vector2 p = Vector2.Lerp(startPos, targetPos, Mathf.Clamp01(t));
            rb.MovePosition(p);

            ApplyBlink();
            yield return null;
        }

        if (sr != null)
        {
            sr.color = originalColor;
            if (restoreSpriteAfterDash)
                sr.sprite = originalSprite;
        }

        rb.collisionDetectionMode = prevCd;

        if (hp != null) hp.SetInvulnerable(false);

        if (disableMovementWhileDashing && movement != null)
            movement.enabled = true;

        IsDashing = false;
    }

    private void ApplyBlink()
    {
        if (sr == null) return;

        float wave = (Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f); // 0..1
        float k = Mathf.Lerp(0f, blinkIntensity, wave);
        sr.color = Color.Lerp(originalColor, dashTintColor, k);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsDashing) return;
        if (other == null) return;

        if (!string.IsNullOrEmpty(enemyProjectileTag) && other.CompareTag(enemyProjectileTag))
        {
            var reflectable = other.GetComponent<IReflectableProjectile>();
            if (reflectable != null)
            {
                reflectable.ReflectBackToSender(transform.position);
            }
        }
    }
}

public interface IReflectableProjectile
{
    void ReflectBackToSender(Vector3 reflectOrigin);
}
