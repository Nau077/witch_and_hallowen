using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 10f;   // важно оставить
    public float leftLimit = -9.5f; // важно оставить
    public float rightLimit = 9.5f; // важно оставить

    [Header("Charge Slowdown")]
    public bool slowWhileCharging = true;
    [Range(0.05f, 1f)] public float chargeMoveMultiplier = 0.4f;

    [Header("Sprite Facing")]
    public bool baseSpriteFacesRight = true;

    [Header("Input")]
    [Range(0f, 0.3f)] public float inputDeadZone = 0.05f;

    [Header("Animation")]
    public bool blockFlipWhileCharging = true;

    [Header("Stun Settings")]
    public int blinkCount = 4;
    public float blinkInterval = 0.10f;

    // runtime refs
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private PlayerFireballShooter shooter;
    private PlayerHealth hp;

    // state
    private bool isStunned = false;
    private Coroutine blinkRoutine;
    private float moveInput;            // читаем в Update, применяем в FixedUpdate

    public bool FacingLeft { get; private set; } = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        anim = spriteRenderer ? spriteRenderer.GetComponent<Animator>() : null;
        shooter = GetComponent<PlayerFireballShooter>();
        hp = GetComponent<PlayerHealth>();

        if (anim && !anim.enabled) anim.enabled = true;

        // Рекомендованные настройки Rigidbody2D для стабильности:
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true; // 2D персонажу обычно не нужна Z-ротация
    }

    void Update()
    {
        if (isStunned) return;

        if (hp != null && hp.IsDead)
        {
            if (anim && anim.enabled) anim.enabled = false;
            return;
        }

        // 1) Считываем ввод в Update
        moveInput = Input.GetAxisRaw("Horizontal"); // -1..1

        // 2) Обновляем направление (для флипа/анимации)
        if (Mathf.Abs(moveInput) > inputDeadZone)
            FacingLeft = moveInput < 0f;

        bool lockFlip = blockFlipWhileCharging && shooter != null && shooter.IsCharging;
        if (spriteRenderer && !lockFlip)
        {
            bool needFlip = baseSpriteFacesRight ? FacingLeft : !FacingLeft;
            spriteRenderer.flipX = needFlip;
        }

        if (anim && anim.enabled)
        {
            bool isRunning = Mathf.Abs(moveInput) > inputDeadZone && !isStunned;
            anim.SetBool("isRunning", isRunning);
        }
    }

    void FixedUpdate()
    {
        if (isStunned) return;
        if (hp != null && hp.IsDead) return;

        // === скорость с учётом замаха ===
        float speedFactor = 1f;
        if (slowWhileCharging && shooter != null && shooter.IsCharging)
            speedFactor = chargeMoveMultiplier;
        float currentSpeed = moveSpeed * Mathf.Clamp(speedFactor, 0.05f, 1f);

        // === Перемещение: ТОЛЬКО в FixedUpdate + fixedDeltaTime ===
        Vector2 pos = rb.position;
        pos.x += moveInput * currentSpeed * Time.fixedDeltaTime;
        pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        rb.MovePosition(pos);
    }

    // === оглушение ===
    public void OnHit(float stunDuration, int blinkCnt, float blinkInt)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(StunAndBlink(stunDuration, blinkCnt, blinkInt));
    }

    private IEnumerator StunAndBlink(float duration, int blinks, float interval)
    {
        isStunned = true;

        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = StartCoroutine(Blink(blinks, interval));

        yield return new WaitForSeconds(duration);

        isStunned = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    private IEnumerator Blink(int blinks, float interval)
    {
        for (int i = 0; i < blinks; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(interval);
        }
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }
}
