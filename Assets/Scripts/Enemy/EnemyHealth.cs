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
    public Color fireTint = new Color(1f, 0.45f, 0.05f, 1f); // �������-���������
    public float flashDuration = 0.15f;                      // ������������ �������
    public ParticleSystem burnParticles;                     // (���.) �������� ����/�������

    [Header("Death")]
    public Sprite deadSprite;         // (���.) ������ ������
    public float destroyAfter = 1.2f; // �������� ����� ���������

    [Header("HP Bar (sprite)")]
    public HealthBar2D hpBar;         // ��. ������ HealthBar2D

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

    // === �����: ���������� � ����� PlayerFireball � �� �������� ������ TakeDamage ===
    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // ���������� ������ ��������� �����
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

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // ��������� ������������ � ������
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;

        // ��������� ���������
        var walker = GetComponent<EnemyWalker>();
        if (walker) walker.enabled = false;

        // ��������/������ ������
        var anim = GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController)
            anim.SetTrigger("Die");
        else if (deadSprite)
            sr.sprite = deadSprite;

        if (hpBar) hpBar.Hide();

        Destroy(gameObject, destroyAfter);
    }
}
