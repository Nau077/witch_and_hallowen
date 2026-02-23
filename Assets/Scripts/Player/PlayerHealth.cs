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
    [SerializeField] private SpriteRenderer sr;   // Ñ€ÐµÐ±Ñ‘Ð½Ð¾Ðº witch_runner_2_1
    [SerializeField] private Animator anim;       // Ñ‚Ð°Ð¼ Ð¶Ðµ
    public Sprite aliveSprite;
    public Sprite deadSprite;

    [Header("Death")]
    public bool disableMovementOnDeath = true;

    [Header("Audio")]
    public AudioSource audioSource;   // AudioSource Ð½Ð° Ð²ÐµÐ´ÑŒÐ¼Ðµ
    public AudioClip hitSfx;          // Ð·Ð²ÑƒÐº Ð¿Ð¾Ð¿Ð°Ð´Ð°Ð½Ð¸Ñ Ð¿Ð¾ Ð²ÐµÐ´ÑŒÐ¼Ðµ

    [Header("Damage Text")]
    [Tooltip("ÐŸÑ€ÐµÑ„Ð°Ð± DamageTextPopup / PlayerDamageText (Ñ TextMeshPro-Text UI).")]
    public DamageTextPopup damageTextPrefab;

    [Tooltip("Canvas / Transform, Ð² ÐºÐ¾Ñ‚Ð¾Ñ€Ð¾Ð¼ Ð¶Ð¸Ð²Ñ‘Ñ‚ Ð²ÐµÑÑŒ UI (HealthBar, SkillBar Ð¸ Ñ‚.Ð´.).")]
    public Transform damageTextParent;

    [Tooltip("Ð¡Ð¼ÐµÑ‰ÐµÐ½Ð¸Ðµ Ð½Ð°Ð´ Ð³Ð¾Ð»Ð¾Ð²Ð¾Ð¹ Ð² Ð¼Ð¸Ñ€Ð¾Ð²Ñ‹Ñ… ÐºÐ¾Ð¾Ñ€Ð´Ð¸Ð½Ð°Ñ‚Ð°Ñ….")]
    public Vector3 damageTextOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Debug / State")]
    public bool isDead = false;

    [Header("Invulnerability")]
    [SerializeField] private bool invulnerable = false;
    public bool IsInvulnerable => invulnerable;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private PlayerSkillShooter shooter;

    private RigidbodyType2D defaultBodyType;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;

    public float Normalized => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    [SerializeField] private int baseMaxHealth;
    [SerializeField] private int permanentMaxHealthBonus;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        shooter = GetComponent<PlayerSkillShooter>();

        if (rb != null)
            defaultBodyType = rb.bodyType;

        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
        if (anim == null && sr != null) anim = sr.GetComponent<Animator>();

        isDead = false;
        invulnerable = false;

        if (anim) anim.enabled = true;

        currentHealth = maxHealth;
        if (baseMaxHealth <= 0)
            baseMaxHealth = maxHealth;

        UpdateBar();

        if (aliveSprite != null && sr != null)
            sr.sprite = aliveSprite;
    }

    private void Start()
    {
        if (currentHealth <= 0) Die();
    }

    public void SetInvulnerable(bool v)
    {
        invulnerable = v;
    }

    public int TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return 0;
        if (invulnerable) return 0;

        int prev = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateBar();

        int taken = prev - currentHealth;

        if (taken > 0)
        {
            PlayHitSound();
            ShowDamagePopup(taken);
        }

        if (currentHealth <= 0) Die();

        return taken;
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateBar();
    }

    public void SetCurrentHealthClamped(int value)
    {
        if (maxHealth <= 0) return;

        currentHealth = Mathf.Clamp(value, 1, maxHealth);
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

        NoDeathStreakRecord.RegisterDeath();

        ApplyDeathVisual();
        StartCoroutine(DeathSequence());
    }

    private void ApplyDeathVisual()
    {
        if (anim)
        {
            anim.enabled = false;
            anim.Update(0f);
        }

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

        if (shooter != null)
        {
            shooter.CancelAllImmediate(keepAnimatorDisabled: true);
            shooter.enabled = false;
        }
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        var camFx = Camera.main ? Camera.main.GetComponent<GrayscaleEffect>() : null;

        if (camFx)
        {
            float t = 0f;
            while (t < 1f)
            {
                camFx.intensity = Mathf.Lerp(0f, 1f, t);
                t += Time.deltaTime;
                yield return null;
            }
            camFx.intensity = 1f;
        }

        yield return new WaitForSeconds(1f);

        if (WitchIsDeadPopup.Instance)
            WitchIsDeadPopup.Instance.Show("Witch is dead");

        yield return new WaitForSeconds(1.2f);

        if (WitchIsDeadPopup.Instance)
            WitchIsDeadPopup.Instance.HideImmediate();

        if (camFx)
            camFx.intensity = 0f;

        PlayerWallet.Instance?.ResetRunCoins();

        if (RunLevelManager.Instance != null)
        {
            RunLevelManager.Instance.ReturnToBaseAfterDeath();
        }
        else if (GameFlow.Instance != null)
        {
            GameFlow.Instance.OnPlayerDied();
        }
    }

    public void RespawnFull()
    {
        isDead = false;
        invulnerable = false;

        currentHealth = maxHealth;
        UpdateBar();

        if (rb != null)
        {
            rb.bodyType = defaultBodyType;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (disableMovementOnDeath && movement != null)
            movement.enabled = true;

        if (anim)
            anim.enabled = true;

        if (aliveSprite != null && sr != null)
            sr.sprite = aliveSprite;

        if (shooter != null)
            shooter.enabled = true;
    }

    private void PlayHitSound()
    {
        if (audioSource != null && hitSfx != null)
        {
            audioSource.PlayOneShot(hitSfx, 1f);
        }
    }

    private void ShowDamagePopup(int amount)
    {
        if (damageTextPrefab == null) return;
        if (damageTextParent == null)
        {
            Debug.LogWarning("PlayerHealth: damageTextParent (Canvas) is not assigned");
            return;
        }

        Vector3 worldPos = transform.position + damageTextOffset;

        Camera cam = Camera.main;
        if (!cam) cam = Camera.current;
        if (!cam) return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = damageTextParent as RectTransform;
        if (canvasRect == null)
        {
            Debug.LogWarning("PlayerHealth: damageTextParent is not RectTransform (Canvas)");
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            cam,
            out localPoint
        );

        DamageTextPopup popup = Instantiate(damageTextPrefab, canvasRect);

        RectTransform rect = popup.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = localPoint;
        }
        else
        {
            popup.transform.position = screenPos;
        }

        popup.Setup(amount);
    }

    public void ApplyPermanentMaxHpBonus(int bonus)
    {
        permanentMaxHealthBonus = Mathf.Max(0, bonus);

        int newMax = Mathf.Max(1, baseMaxHealth + permanentMaxHealthBonus);
        maxHealth = newMax;

        currentHealth = maxHealth;
        UpdateBar();
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


