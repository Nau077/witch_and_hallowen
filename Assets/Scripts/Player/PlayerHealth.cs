using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth;

    [Header("UI")]
    public Image barFill;

    [Header("Sprites / Visual")]
    [SerializeField] private SpriteRenderer sr;   // ребёнок witch_runner_2_1
    [SerializeField] private Animator anim;       // там же
    public Sprite aliveSprite;
    public Sprite deadSprite;

    [Header("Death")]
    public bool disableMovementOnDeath = true;

    [Header("Audio")]
    public AudioSource audioSource;   // AudioSource на ведьме
    public AudioClip hitSfx;          // звук попадания по ведьме

    [Header("Debug / State")]
    public bool isDead = false;

    private Rigidbody2D rb;
    private PlayerMovement movement;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;

    // Процент хп для UI
    public float Normalized => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
        if (anim == null && sr != null) anim = sr.GetComponent<Animator>();

        isDead = false;
        if (anim) anim.enabled = true;

        currentHealth = maxHealth;
        UpdateBar();
    }

    private void Start()
    {
        if (currentHealth <= 0) Die();
    }

    public int TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return 0;

        int prev = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateBar();

        // ВАЖНО: звук только если реально что-то сняли
        if (currentHealth < prev)
        {
            PlayHitSound();
        }

        if (currentHealth <= 0) Die();

        return prev - currentHealth;
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (barFill != null)
        {
            float t = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;
            barFill.fillAmount = t;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        ApplyDeathVisual();
    }

    private void ApplyDeathVisual()
    {
        if (anim) { anim.enabled = false; anim.Update(0f); }

        if (deadSprite != null && sr != null)
            sr.sprite = deadSprite;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (disableMovementOnDeath && movement != null)
            movement.enabled = false;

        var shooter = GetComponent<PlayerSkillShooter>();
        if (shooter != null)
        {
            shooter.CancelAllImmediate(keepAnimatorDisabled: true);
            shooter.enabled = false;
        }
    }

    private void PlayHitSound()
    {
        if (audioSource != null && hitSfx != null)
        {
            // PlayOneShot, чтобы не сбивать другие звуки игрока
            audioSource.PlayOneShot(hitSfx, 1f);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
            if (anim == null && sr != null) anim = sr.GetComponent<Animator>();

            if (sr != null)
            {
                if (isDead && deadSprite != null) sr.sprite = deadSprite;
                else if (!isDead && aliveSprite != null) sr.sprite = aliveSprite;
            }
        }
    }
#endif
}
