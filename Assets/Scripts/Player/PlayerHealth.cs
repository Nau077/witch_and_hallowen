using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth;

    [Header("UI")]
    public Image barFill; // перетащи сюда UI Image (BarFill) из Canvas

    [Header("Sprites")]
    public Sprite aliveSprite; // перетащи сюда базовый стоячий спрайт (живой)
    public Sprite deadSprite;  // перетащи сюда лежачий спрайт (смерть)

    [Header("Death")]
    public bool disableMovementOnDeath = true;

    [Header("Debug / State")]
    public bool isDead = false; // можно включать/выключать в редакторе для предпросмотра

    // refs
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PlayerMovement movement;

    // удобные свойства
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();

        // ✅ Всегда стартуем живыми — игнорируем значение isDead из Scene
        ForceStartAlive();

        // инициализируем здоровье/полоску
        currentHealth = maxHealth;
        UpdateBar();
    }

    /// <summary>
    /// Насильно переводит персонажа в живое состояние (для старта игры).
    /// </summary>
    private void ForceStartAlive()
    {
        isDead = false;

        // визуально — живой спрайт
        if (aliveSprite != null && sr != null)
            sr.sprite = aliveSprite;

        // физика — динамическая
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // обратно включаем физику
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f; // если у тебя 0 в проекте
        }

        // управление включено
        if (movement != null)
            movement.enabled = true;
    }

    /// <summary>
    /// Нанести урон игроку. Возвращает фактический урон (может быть 0).
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (isDead) return 0;
        if (amount <= 0) return 0;

        int prev = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateBar();

        if (currentHealth <= 0)
            Die();

        return prev - currentHealth;
    }

    /// <summary>
    /// Лечение (на будущее).
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

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
        // спрайт смерти
        if (deadSprite != null && sr != null)
            sr.sprite = deadSprite;

        // остановить физику и зафиксировать тело
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic; // чтобы не толкался другими объектами
        }

        // отключить управление
        if (disableMovementOnDeath && movement != null)
        {
            movement.enabled = false;
        }

        // NEW: запретить стрельбу/замахи после смерти
        var shooter = GetComponent<PlayerFireballShooter>();
        if (shooter != null)
        {
            shooter.CancelAllImmediate(); // сбросить зарядку и UI, БЕЗ смены спрайта
            shooter.enabled = false;      // полностью выключаем компонент
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// В редакторе: моментально переключает спрайт при смене чекбокса isDead.
    /// Это работает только вне Play Mode и нужно чтобы удобно подгонять размеры.
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();

            if (isDead)
            {
                if (deadSprite != null && sr != null)
                    sr.sprite = deadSprite;
            }
            else
            {
                if (aliveSprite != null && sr != null)
                    sr.sprite = aliveSprite;
            }
        }
    }
#endif
}
