using System;            // добавлено
using System.Collections;
using UnityEngine;
using UnityEngine.Audio; // для AudioMixerGroup (опционально)

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))] // гарантируем наличие источника звука
public class EnemyHealth : MonoBehaviour
{
    // === Событие: кто угодно может подписаться на смерть врага ===
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

    // ---------- ЗВУКИ ----------
    [Header("Sound / Hit")]
    [Tooltip("Варианты клипов для попадания (выбирается случайный). Можно указать 1 клип.")]
    public AudioClip[] hitClips;
    [Range(0f, 1f)] public float hitVolume = 0.85f;
    [Tooltip("Случайное отклонение pitch для разнообразия звука, напр. 0.08 = ±8%.")]
    [Range(0f, 0.5f)] public float hitPitchJitter = 0.08f;
    [Tooltip("Минимальный интервал между звуками попаданий, чтобы не было 'каши'.")]
    [Range(0f, 0.25f)] public float hitSoundCooldown = 0.05f;

    [Header("Sound / Death")]
    public AudioClip deathClip;
    [Range(0f, 1f)] public float deathVolume = 0.95f;

    [Header("Sound / Advanced")]
    [Tooltip("Если true — звук позиционируется в пространстве (3D). Если false — плоский 2D звук.")]
    public bool spatialize = false;
    [Tooltip("Опционально: назначь группу микшера (SFX), если используешь AudioMixer.")]
    public AudioMixerGroup outputMixerGroup;

    [Header("Hit FX (Particles)")]
    public GameObject hitParticlesPrefab;

    // ---------- Приватные ----------
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

        // Настройка источника звука (общие безопасные значения)
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

        // Визуальный фидбек
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
            Destroy(fx, 0.6f); // убрать после проигрыша
        }

        // Обновление полоски HP
        if (hpBar) hpBar.SetValue(currentHealth);

        // Звук попадания (с защитой от частых срабатываний)
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

        // Выбираем клип
        var clip = hitClips[UnityEngine.Random.Range(0, hitClips.Length)];
        if (clip == null) return;

        // Лёгкая вариативность по питчу
        float pitch = 1f + UnityEngine.Random.Range(-hitPitchJitter, hitPitchJitter);
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, hitVolume);
        // Не сбрасываем pitch сразу — следующий звук снова установит его перед воспроизведением.
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

        // Начисляем души (с защитой от null)
        if (SoulCounter.Instance != null)
            SoulCounter.Instance.AddSouls(10);
        else
            Debug.LogWarning("[EnemyHealth] SoulCounter.Instance is null — душа не начислена.");

        // Отключаем поведение/физику
        var walker = GetComponent<EnemyWalker>();
        if (walker) walker.OnDeathExternal();

        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>(); if (rb) rb.simulated = false;
        if (walker) walker.enabled = false;

        // Анимация/спрайт смерти
        var anim = GetComponent<Animator>();
        var srLocal = GetComponent<SpriteRenderer>();
        if (anim && anim.runtimeAnimatorController) anim.SetTrigger("Die");
        else if (deadSprite) srLocal.sprite = deadSprite;

        // Прячем HP бар
        if (hpBar) hpBar.Hide();

        // Звук смерти
        PlayDeathSound();

        // Сообщаем всем: «враг умер»
        OnAnyEnemyDied?.Invoke(this);

        // Фейд и удаление
        StartCoroutine(FadeAndDestroy(srLocal, destroyAfter, 0.4f));
    }

    private void PlayDeathSound()
    {
        if (deathClip == null || audioSource == null) return;

        // Для звука смерти не делаем кулдаун, но можно добавить небольшую вариативность
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
