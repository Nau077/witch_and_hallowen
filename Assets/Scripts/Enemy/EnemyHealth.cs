using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth;

    [Header("Hit Effect")]
    public Color fireTint = new Color(1f, 0.45f, 0.05f, 1f); // огненно-оранжевый
    public float flashDuration = 0.3f;                      // длительность вспышки
    public ParticleSystem burnParticles;                     // (опц.) партиклы огн€/искорок

    [Header("Death")]
    public Sprite deadSprite;         // (опц.) спрайт смерти
    public float destroyAfter = 2.2f; // задержка перед удалением

    [Header("HP Bar (sprite)")]
    public HealthBar2D hpBar;         // см. скрипт HealthBar2D

    private SpriteRenderer sr;
    private Color baseColor;
    private bool isDead;
    public bool IsDead => isDead;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        currentHealth = maxHealth;
        if (hpBar) hpBar.SetMax(maxHealth);
    }

    // === ¬ј∆Ќќ: совместимо с твоим PlayerFireball Ч он вызывает именно TakeDamage ===
    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // визуальный отклик Ђогненный флэшї
        StopAllCoroutines();
        StartCoroutine(HitFlash());
        if (burnParticles) { burnParticles.Play(); StartCoroutine(StopParticlesSoon(0.25f)); }

        if (hpBar) hpBar.SetValue(currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator HitFlash()
    {
        float half = flashDuration * 0.5f;
        sr.color = fireTint;
        yield return new WaitForSeconds(half);

        float t = 0f;
        while (t < half)
        {
            sr.color = Color.Lerp(fireTint, baseColor, t / half);
            t += Time.deltaTime;
            yield return null;
        }
        sr.color = baseColor;
    }

    private IEnumerator StopParticlesSoon(float t)
    {
        yield return new WaitForSeconds(t);
        if (burnParticles) burnParticles.Stop();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>(); if (rb) rb.simulated = false;

        var walker = GetComponent<EnemyWalker>();
        if (walker) walker.enabled = false;

        var anim = GetComponent<Animator>();
        var sr = GetComponent<SpriteRenderer>();
        if (anim && anim.runtimeAnimatorController) anim.SetTrigger("Die");
        else if (deadSprite) sr.sprite = deadSprite;

        if (hpBar) hpBar.Hide();

        // опци€: плавный fade-out за последние 0.4 c
        StartCoroutine(FadeAndDestroy(sr, destroyAfter, 0.4f));
    }

    IEnumerator FadeAndDestroy(SpriteRenderer sr, float delay, float fadeTime)
    {
        float wait = Mathf.Max(0f, delay - fadeTime);
        yield return new WaitForSeconds(wait);

        if (sr)
        {
            Color c = sr.color;
            float t = 0f;
            while (t < fadeTime)
            {
                float a = Mathf.Lerp(1f, 0f, t / fadeTime);
                sr.color = new Color(c.r, c.g, c.b, a);
                t += Time.deltaTime;
                yield return null;
            }
            sr.color = new Color(c.r, c.g, c.b, 0f);
        }
        Destroy(gameObject);
    }
}
