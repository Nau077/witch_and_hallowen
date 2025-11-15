using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class EnemyHealth : MonoBehaviour
{
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

    [Header("Sound / Hit")]
    public AudioClip[] hitClips;
    [Range(0f, 1f)] public float hitVolume = 0.85f;
    [Range(0f, 0.5f)] public float hitPitchJitter = 0.08f;
    [Range(0f, 0.25f)] public float hitSoundCooldown = 0.05f;

    [Header("Sound / Death")]
    public AudioClip deathClip;
    [Range(0f, 1f)] public float deathVolume = 0.95f;

    [Header("Sound / Advanced")]
    public bool spatialize = false;
    public AudioMixerGroup outputMixerGroup;

    [Header("Hit FX (Particles)")]
    public GameObject hitParticlesPrefab;

    [Header("Rewards")]
    public int cursedGoldOnDeath = 10;

    // ---------- ICE / FREEZE ----------
    [Header("Ice Freeze")]
    [Tooltip("Можно ли этого врага вообще замораживать.")]
    public bool canBeFrozen = true;

    [Tooltip("Префаб льда (SpriteRenderer), который будет появляться поверх врага при заморозке.")]
    public GameObject freezeVfxPrefab;
    [Tooltip("Смещение льда относительно центра врага (например, 0, 0.3).")]
    public Vector2 freezeVfxOffset = new Vector2(0f, 0.3f);

    private bool isFrozen;
    public bool IsFrozen => isFrozen;

    private GameObject _currentFreezeVfx;
    private Coroutine _freezeRoutine;

    // ---------- INTERNAL ----------
    private SpriteRenderer sr;
    private Color baseColor;
    private bool isDead;
    public bool IsDead => isDead;

    private AudioSource audioSource;
    private float lastHitSoundTime = -999f;

    private Coroutine _hitFlashRoutine;
    private Coroutine _stopParticlesRoutine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = spatialize ? 1f : 0f;
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

        // не трогаем заморозку, только эффекты удара
        if (_hitFlashRoutine != null)
            StopCoroutine(_hitFlashRoutine);
        _hitFlashRoutine = StartCoroutine(HitFlash());

        if (burnParticles)
        {
            burnParticles.Play();
            if (_stopParticlesRoutine != null)
                StopCoroutine(_stopParticlesRoutine);
            _stopParticlesRoutine = StartCoroutine(StopParticlesSoon(0.25f));
        }

        if (hitParticlesPrefab)
        {
            var fx = Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 0.6f);
        }

        if (hpBar) hpBar.SetValue(currentHealth);

        TryPlayHitSound();

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Снаружи (скил) говорит врагу: "замёрзни на duration секунд".
    /// </summary>
    public void ApplyFreeze(float duration)
    {
        if (!canBeFrozen || isDead) return;

        if (_freezeRoutine != null)
            StopCoroutine(_freezeRoutine);

        _freezeRoutine = StartCoroutine(FreezeCoroutine(duration));
    }

    private IEnumerator FreezeCoroutine(float duration)
    {
        isFrozen = true;

        if (freezeVfxPrefab != null && _currentFreezeVfx == null)
        {
            Vector3 pos = transform.position + new Vector3(freezeVfxOffset.x, freezeVfxOffset.y, 0f);
            _currentFreezeVfx = Instantiate(freezeVfxPrefab, pos, Quaternion.identity, transform);
        }

        float t = 0f;
        float d = Mathf.Max(0.05f, duration);

        while (t < d)
        {
            if (isDead) break;
            t += Time.deltaTime;
            yield return null;
        }

        isFrozen = false;

        if (_currentFreezeVfx != null)
        {
            Destroy(_currentFreezeVfx);
            _currentFreezeVfx = null;
        }

        _freezeRoutine = null;
    }

    private void TryPlayHitSound()
    {
        if (hitClips == null || hitClips.Length == 0 || audioSource == null) return;
        if (Time.time - lastHitSoundTime < hitSoundCooldown) return;

        lastHitSoundTime = Time.time;

        var clip = hitClips[UnityEngine.Random.Range(0, hitClips.Length)];
        if (!clip) return;

        audioSource.pitch = 1f + UnityEngine.Random.Range(-hitPitchJitter, hitPitchJitter);
        audioSource.PlayOneShot(clip, hitVolume);
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

        _hitFlashRoutine = null;
    }

    private IEnumerator StopParticlesSoon(float t)
    {
        yield return new WaitForSeconds(t);
        if (burnParticles) burnParticles.Stop();
        _stopParticlesRoutine = null;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // снимаем заморозку
        if (_freezeRoutine != null) StopCoroutine(_freezeRoutine);
        isFrozen = false;
        if (_currentFreezeVfx != null)
        {
            Destroy(_currentFreezeVfx);
            _currentFreezeVfx = null;
        }

        var walker = GetComponent<EnemyWalker>();
        if (walker) walker.OnDeathExternal();

        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>(); if (rb) rb.simulated = false;
        if (walker) walker.enabled = false;

        var anim = GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController) anim.SetTrigger("Die");
        else if (deadSprite) sr.sprite = deadSprite;

        if (hpBar) hpBar.Hide();

        PlayDeathSound();

        OnAnyEnemyDied?.Invoke(this);

        StartCoroutine(FadeAndDestroy(sr, destroyAfter, 0.4f));
    }

    private void PlayDeathSound()
    {
        if (!deathClip || !audioSource) return;
        audioSource.pitch = 1f + UnityEngine.Random.Range(-hitPitchJitter * 0.5f, hitPitchJitter * 0.5f);
        audioSource.PlayOneShot(deathClip, deathVolume);
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer srRen, float delay, float fadeTime)
    {
        float wait = Mathf.Max(0f, delay - fadeTime);
        yield return new WaitForSeconds(wait);

        if (srRen)
        {
            Color c = srRen.color;
            float t = 0f;
            while (t < fadeTime)
            {
                float a = Mathf.Lerp(1f, 0f, t / fadeTime);
                srRen.color = new Color(c.r, c.g, c.b, a);
                t += Time.deltaTime;
                yield return null;
            }
            srRen.color = new Color(c.r, c.g, c.b, 0f);
        }
        Destroy(gameObject);
    }
}
