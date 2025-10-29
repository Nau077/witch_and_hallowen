using System;            // ���������
using System.Collections;
using UnityEngine;
using UnityEngine.Audio; // ��� AudioMixerGroup (�����������)

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))] // ����������� ������� ��������� �����
public class EnemyHealth : MonoBehaviour
{
    // === �������: ��� ������ ����� ����������� �� ������ ����� ===
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

    // ---------- ����� ----------
    [Header("Sound / Hit")]
    [Tooltip("�������� ������ ��� ��������� (���������� ���������). ����� ������� 1 ����.")]
    public AudioClip[] hitClips;
    [Range(0f, 1f)] public float hitVolume = 0.85f;
    [Tooltip("��������� ���������� pitch ��� ������������ �����, ����. 0.08 = �8%.")]
    [Range(0f, 0.5f)] public float hitPitchJitter = 0.08f;
    [Tooltip("����������� �������� ����� ������� ���������, ����� �� ���� '����'.")]
    [Range(0f, 0.25f)] public float hitSoundCooldown = 0.05f;

    [Header("Sound / Death")]
    public AudioClip deathClip;
    [Range(0f, 1f)] public float deathVolume = 0.95f;

    [Header("Sound / Advanced")]
    [Tooltip("���� true � ���� ��������������� � ������������ (3D). ���� false � ������� 2D ����.")]
    public bool spatialize = false;
    [Tooltip("�����������: ������� ������ ������� (SFX), ���� ����������� AudioMixer.")]
    public AudioMixerGroup outputMixerGroup;

    [Header("Hit FX (Particles)")]
    public GameObject hitParticlesPrefab;

    // ---------- ��������� ----------
    private SpriteRenderer sr;
    private Color baseColor;
    private bool isDead;
    public bool IsDead => isDead;

    private AudioSource audioSource;
    private float lastHitSoundTime = -999f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // ��������� ��������� ����� (����� ���������� ��������)
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = spatialize ? 1f : 0f; // 0 = 2D, 1 = 3D
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 25f;
            if (outputMixerGroup != null)
                audioSource.outputAudioMixerGroup = outputMixerGroup;
        }

        baseColor = sr.color;
        currentHealth = maxHealth;
        if (hpBar) hpBar.SetMax(maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // ���������� ������
        StopAllCoroutines();
        StartCoroutine(HitFlash());
        if (burnParticles)
        {
            burnParticles.Play();
            StartCoroutine(StopParticlesSoon(0.25f));
        }

        if (hitParticlesPrefab)
        {
            var fx = Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 0.6f); // ������ ����� ���������
        }

        // ���������� ������� HP
        if (hpBar) hpBar.SetValue(currentHealth);

        // ���� ��������� (� ������� �� ������ ������������)
        TryPlayHitSound();

        if (currentHealth <= 0)
            Die();
    }

    private void TryPlayHitSound()
    {
        if (hitClips == null || hitClips.Length == 0 || audioSource == null) return;

        if (Time.time - lastHitSoundTime < hitSoundCooldown)
            return;

        lastHitSoundTime = Time.time;

        // �������� ����
        var clip = hitClips[UnityEngine.Random.Range(0, hitClips.Length)];
        if (clip == null) return;

        // ˸���� ������������� �� �����
        float pitch = 1f + UnityEngine.Random.Range(-hitPitchJitter, hitPitchJitter);
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, hitVolume);
        // �� ���������� pitch ����� � ��������� ���� ����� ��������� ��� ����� ����������������.
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

        // ��������� ���� (� ������� �� null)
        if (SoulCounter.Instance != null)
            SoulCounter.Instance.AddSouls(10);
        else
            Debug.LogWarning("[EnemyHealth] SoulCounter.Instance is null � ���� �� ���������.");

        // ��������� ���������/������
        var walker = GetComponent<EnemyWalker>();
        if (walker) walker.OnDeathExternal();

        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>(); if (rb) rb.simulated = false;
        if (walker) walker.enabled = false;

        // ��������/������ ������
        var anim = GetComponent<Animator>();
        var srLocal = GetComponent<SpriteRenderer>();
        if (anim && anim.runtimeAnimatorController) anim.SetTrigger("Die");
        else if (deadSprite) srLocal.sprite = deadSprite;

        // ������ HP ���
        if (hpBar) hpBar.Hide();

        // ���� ������
        PlayDeathSound();

        // �������� ����: ����� ����
        OnAnyEnemyDied?.Invoke(this);

        // ���� � ��������
        StartCoroutine(FadeAndDestroy(srLocal, destroyAfter, 0.4f));
    }

    private void PlayDeathSound()
    {
        if (deathClip == null || audioSource == null) return;

        // ��� ����� ������ �� ������ �������, �� ����� �������� ��������� �������������
        audioSource.pitch = 1f + UnityEngine.Random.Range(-hitPitchJitter * 0.5f, hitPitchJitter * 0.5f);
        audioSource.PlayOneShot(deathClip, deathVolume);
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
