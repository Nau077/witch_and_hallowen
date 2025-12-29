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

    [Header("Damage Text")]
    [Tooltip("Префаб DamageTextPopup / PlayerDamageText (с TextMeshPro-Text UI).")]
    public DamageTextPopup damageTextPrefab;

    [Tooltip("Canvas / Transform, в котором живёт весь UI (HealthBar, SkillBar и т.д.).")]
    public Transform damageTextParent;

    [Tooltip("Смещение над головой в мировых координатах.")]
    public Vector3 damageTextOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Debug / State")]
    public bool isDead = false;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private PlayerSkillShooter shooter;

    private RigidbodyType2D defaultBodyType;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;

    // Процент хп для UI
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
        if (anim) anim.enabled = true;

        currentHealth = maxHealth;
        UpdateBar();

        // визуал "живая ведьма" на старте
        if (aliveSprite != null && sr != null)
            sr.sprite = aliveSprite;
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

        // Запускаем кат-сцену смерти
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

    /// <summary>
    /// Кат-сцена смерти игрока.
    /// </summary>
    private System.Collections.IEnumerator DeathSequence()
    {
        // --- GRAYSCALE FADE ---
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

        // подождать 1 секунду
        yield return new WaitForSeconds(1f);

        // показать попап
        if (WitchIsDeadPopup.Instance)
            WitchIsDeadPopup.Instance.Show("Witch is dead");

        // подождать ещё немного
        yield return new WaitForSeconds(1.2f);

        // выключить попап
        if (WitchIsDeadPopup.Instance)
            WitchIsDeadPopup.Instance.HideImmediate();

        // вернуть цвет
        if (camFx)
            camFx.intensity = 0f;

        // вернуть игрока на базу
        if (RunLevelManager.Instance != null)
        {
            RunLevelManager.Instance.ReturnToBaseAfterDeath();
        }
        else if (GameFlow.Instance != null)
        {
            GameFlow.Instance.OnPlayerDied();
        }
    }

    /// <summary>
    /// Полный респавн ведьмы (для возврата на базу).
    /// Вызывается из RunLevelManager.
    /// </summary>
    public void RespawnFull()
    {
        isDead = false;
        currentHealth = maxHealth;
        UpdateBar();

        // Восстановить физику
        if (rb != null)
        {
            rb.bodyType = defaultBodyType;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Восстановить движение
        if (disableMovementOnDeath && movement != null)
            movement.enabled = true;

        // Вернуть анимации
        if (anim)
            anim.enabled = true;

        // Вернуть живой спрайт
        if (aliveSprite != null && sr != null)
            sr.sprite = aliveSprite;

        // Включить стрельбу
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
        maxHealth = baseMaxHealth + Mathf.Max(0, bonus);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        // если у тебя есть UI-обновление — дерни его тут
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
