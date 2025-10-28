using System;            // <-- добавь
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyHealth : MonoBehaviour
{
    // === ƒќЅј¬№: статическое событие, кто угодно может подписатьс€ ===
    public static event Action<EnemyHealth> OnAnyEnemyDied;

    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth;

    [Header("Hit Effect")]
    public Color fireTint = new Color(1f, 0.45f, 0.05f, 1f);
    public float flashDuration = 0.3f;
    public ParticleSystem burnParticles;

    [Header("Death")]
    public Sprite deadSprite;
    public float destroyAfter = 2.2f;

    [Header("HP Bar (sprite)")]
    public HealthBar2D hpBar;

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

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

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

        if (SoulCounter.Instance != null)
            SoulCounter.Instance.AddSouls(10);
        else
            Debug.LogWarning("[EnemyHealth] SoulCounter.Instance is null Ч душа не начислена.");

        var walker = GetComponent<EnemyWalker>();
        if (walker) walker.OnDeathExternal();

        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>(); if (rb) rb.simulated = false;
        if (walker) walker.enabled = false;

        var anim = GetComponent<Animator>();
        var sr = GetComponent<SpriteRenderer>();
        if (anim && anim.runtimeAnimatorController) anim.SetTrigger("Die");
        else if (deadSprite) sr.sprite = deadSprite;

        if (hpBar) hpBar.Hide();

        // === ƒќЅј¬№: сообщаем всем Ђвраг умерї ===
        OnAnyEnemyDied?.Invoke(this);

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
