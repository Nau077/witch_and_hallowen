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
    [Tooltip("Замедлять ли движение во время замаха (зарядки выстрела).")]
    public bool slowWhileCharging = true;
    [Tooltip("Множитель скорости при замахе. 1 = без замедления, 0.5 = вдвое медленнее и т.д.")]
    [Range(0.05f, 1f)] public float chargeMoveMultiplier = 0.4f;

    [Header("Sprite Facing")]
    [Tooltip("True, если базовый (неотражённый) кадр смотрит ВПРАВО.")]
    public bool baseSpriteFacesRight = true;

    [Header("Input")]
    [Range(0f, 0.3f)] public float inputDeadZone = 0.05f;

    [Header("Animation")]
    [Tooltip("Не менять flipX во время зарядки, чтобы поза замаха не прыгала.")]
    public bool blockFlipWhileCharging = true;

    [Header("Stun Settings")]
    public int blinkCount = 4;
    public float blinkInterval = 0.10f;

    // runtime refs
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;   // на ребёнке (witch_runner_2_1)
    private Animator anim;                   // там же
    private PlayerFireballShooter shooter;
    private PlayerHealth hp;

    // state
    private bool isStunned = false;
    private Coroutine blinkRoutine;

    public bool FacingLeft { get; private set; } = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        anim = spriteRenderer ? spriteRenderer.GetComponent<Animator>() : null;
        shooter = GetComponent<PlayerFireballShooter>();
        hp = GetComponent<PlayerHealth>();

        // страховка: если вдруг Animator выключен в сцене — включим
        if (anim && !anim.enabled) anim.enabled = true;
    }

    void Update()
    {
        if (isStunned) return;

        // Если мертвы — стопаем аниматор и выходим
        if (hp != null && hp.IsDead)
        {
            if (anim && anim.enabled) anim.enabled = false; // страховка при смерти
            return;
        }

        float moveInput = Input.GetAxisRaw("Horizontal"); // -1..1

        // === скорость с учётом замаха ===
        float speedFactor = 1f;
        if (slowWhileCharging && shooter != null && shooter.IsCharging)
            speedFactor = chargeMoveMultiplier;
        float currentSpeed = moveSpeed * Mathf.Clamp(speedFactor, 0.05f, 1f);

        // Движение
        Vector2 pos = rb.position;
        pos.x += moveInput * currentSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);
        rb.MovePosition(pos);

        // Направление
        if (Mathf.Abs(moveInput) > inputDeadZone)
            FacingLeft = moveInput < 0f;

        // Флип (если не заряжаем/не блокируем)
        bool lockFlip = blockFlipWhileCharging && shooter != null && shooter.IsCharging;
        if (spriteRenderer && !lockFlip)
        {
            // базовый кадр вправо? Тогда при взгляде влево — flipX=true.
            bool needFlip = baseSpriteFacesRight ? FacingLeft : !FacingLeft;
            spriteRenderer.flipX = needFlip;
        }

        // Animator: Idle/Run
        if (anim && anim.enabled)
        {
            bool isRunning = Mathf.Abs(moveInput) > inputDeadZone && !isStunned;
            anim.SetBool("isRunning", isRunning);
        }
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
