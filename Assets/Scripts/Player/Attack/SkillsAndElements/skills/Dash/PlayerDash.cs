using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerHealth hp;
    [SerializeField] private PlayerSkillShooter shooter;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Animator anim;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.Space;

    [Header("Dash distances (cells) by level")]
    public float cellSize = 1f;
    public float cellsLevel1 = 4f;
    public float cellsLevel2 = 5f;
    public float cellsLevel3 = 6f;

    [Header("Dash motion")]
    public float dashDuration = 0.16f;
    public bool disableMovementWhileDashing = true;

    [Header("Dash energy (cooldown resource)")]
    public float maxEnergy = 50f;
    public float dashCost = 35f;
    public float regenPerSecond = 18f;

    [Header("Visuals")]
    public Sprite broomDashSprite;
    public bool restoreSpriteAfterDash = true;

    [Header("Dash scale")]
    [Tooltip("Во сколько раз увеличить спрайт во время дэша.")]
    public float dashSpriteScale = 1.5f;

    public Color dashTintColor = new Color(0.35f, 0.85f, 1f, 1f);
    public float blinkSpeed = 22f;
    [Range(0f, 1f)] public float blinkIntensity = 0.65f;

    [Header("Reflect hook (later)")]
    public string enemyProjectileTag = "EnemyProjectile";

    public bool IsDashing { get; private set; }
    public float EnergyNormalized => maxEnergy <= 0 ? 0 : Mathf.Clamp01(currentEnergy / maxEnergy);

    private float currentEnergy;
    [SerializeField] private float baseMaxEnergy;
    [SerializeField] private float permanentMaxEnergyBonus;

    // UI: минимальные геттеры для текста 35/50 (или округления)
    public float CurrentEnergy => currentEnergy; // UI
    public float MaxEnergy => maxEnergy;          // UI

    // UI: событие "попытался дэшнуться без энергии"
    public event Action OnDashNoEnergy; // UI

    private Sprite originalSprite;
    private Color originalColor;
    private Vector3 originalScale;
    private bool originalAnimEnabled;

    private Coroutine dashRoutine;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        hp = GetComponent<PlayerHealth>();
        shooter = GetComponent<PlayerSkillShooter>();
        sr = GetComponentInChildren<SpriteRenderer>(true);
        anim = sr ? sr.GetComponent<Animator>() : null;
    }

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!movement) movement = GetComponent<PlayerMovement>();
        if (!hp) hp = GetComponent<PlayerHealth>();
        if (!shooter) shooter = GetComponent<PlayerSkillShooter>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>(true);
        if (!anim && sr) anim = sr.GetComponent<Animator>();

        if (sr)
        {
            originalSprite = sr.sprite;
            originalColor = sr.color;
            originalScale = sr.transform.localScale;
        }

        if (baseMaxEnergy <= 0f)
            baseMaxEnergy = Mathf.Max(1f, maxEnergy);

        currentEnergy = maxEnergy;
    }

    private void Update()
    {
        if (hp != null && hp.IsDead) return;

        if (RunLevelManager.Instance != null &&
            !RunLevelManager.Instance.CanProcessGameplayInput())
            return;

        RegenEnergy();

        if (IsDashing) return;

        if (Input.GetKeyDown(dashKey))
            TryDash();
    }

    private void RegenEnergy()
    {
        if (maxEnergy <= 0f) return;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + regenPerSecond * Time.deltaTime);
    }

    public bool CanDashNow()
    {
        if (IsDashing) return false;
        if (hp != null && hp.IsDead) return false;
        if (RunLevelManager.Instance != null &&
            !RunLevelManager.Instance.CanProcessGameplayInput())
            return false;

        return currentEnergy >= dashCost;
    }

    public void TryDash()
    {
        // UI: если нажали, но дэша нельзя (в т.ч. из-за энергии) — дернём событие только для "не хватило энергии"
        if (!CanDashNow())
        {
            // Сигналим UI только когда причина — именно нехватка энергии.
            // (если пауза/смерть/уже дэшится — флэш не нужен)
            bool blockedByGameplay = (hp != null && hp.IsDead) ||
                                     (RunLevelManager.Instance != null && !RunLevelManager.Instance.CanProcessGameplayInput()) ||
                                     IsDashing;

            if (!blockedByGameplay && currentEnergy < dashCost)
                OnDashNoEnergy?.Invoke();

            return;
        }

        // Сбрасываем визуал/состояние замаха перед дэшем, чтобы
        // после рывка не остаться в windup-кадре.
        if (shooter != null)
            shooter.CancelAllImmediate(resetToIdleSprite: true);

        currentEnergy = Mathf.Max(0f, currentEnergy - dashCost);

        float dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir = 1f;

        if (Mathf.Approximately(dir, 0f))
            dir = (movement != null && movement.FacingLeft) ? -1f : 1f;

        float cells = GetCellsByPerkLevel();
        float worldDistance = cells * Mathf.Max(0.01f, cellSize);

        if (dashRoutine != null)
            StopCoroutine(dashRoutine);

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

        if (hp != null)
            hp.SetInvulnerable(true);

        // ===== VISUAL ENTER =====
        originalSprite = sr.sprite;
        originalColor = sr.color;
        originalScale = sr.transform.localScale;

        if (anim != null)
        {
            originalAnimEnabled = anim.enabled;
            anim.enabled = false;
        }

        if (movement != null)
        {
            bool facingLeft = dir < 0f;
            bool needFlip = movement.baseSpriteFacesRight ? facingLeft : !facingLeft;
            sr.flipX = needFlip;
        }

        if (broomDashSprite != null)
            sr.sprite = broomDashSprite;

        sr.transform.localScale = originalScale * dashSpriteScale;

        // ===== MOVE =====
        Vector2 startPos = rb.position;
        Vector2 targetPos = startPos + new Vector2(dir * worldDistance, 0f);

        float leftLimit = movement != null ? movement.leftLimit : -999f;
        float rightLimit = movement != null ? movement.rightLimit : 999f;

        targetPos.x = Mathf.Clamp(targetPos.x, leftLimit, rightLimit);

        float t = 0f;
        float dur = Mathf.Max(0.001f, dashDuration);

        var prevCd = rb.collisionDetectionMode;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        while (t < 1f)
        {
            if (hp != null && hp.IsDead) break;

            t += Time.deltaTime / dur;
            rb.MovePosition(Vector2.Lerp(startPos, targetPos, Mathf.Clamp01(t)));

            ApplyBlink();
            yield return null;
        }

        rb.collisionDetectionMode = prevCd;

        // ===== VISUAL EXIT =====
        sr.color = originalColor;
        sr.transform.localScale = originalScale;

        if (restoreSpriteAfterDash)
            sr.sprite = originalSprite;

        if (anim != null)
            anim.enabled = originalAnimEnabled;

        if (hp != null)
            hp.SetInvulnerable(false);

        if (disableMovementWhileDashing && movement != null)
            movement.enabled = true;

        IsDashing = false;
    }

    private void ApplyBlink()
    {
        if (sr == null) return;

        float wave = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
        float k = Mathf.Lerp(0f, blinkIntensity, wave);
        sr.color = Color.Lerp(originalColor, dashTintColor, k);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsDashing) return;
        if (other == null) return;

        if (!string.IsNullOrEmpty(enemyProjectileTag) &&
            string.Equals(other.tag, enemyProjectileTag, StringComparison.Ordinal))
        {
            var reflectable = other.GetComponent<IReflectableProjectile>();
            if (reflectable != null)
                reflectable.ReflectBackToSender(transform.position);
        }
    }

    public void ApplyPermanentMaxEnergyBonus(int bonus)
    {
        permanentMaxEnergyBonus = Mathf.Max(0f, bonus);
        maxEnergy = Mathf.Max(1f, baseMaxEnergy + permanentMaxEnergyBonus);
        currentEnergy = maxEnergy;
    }
}

public interface IReflectableProjectile
{
    void ReflectBackToSender(Vector3 reflectOrigin);
}
