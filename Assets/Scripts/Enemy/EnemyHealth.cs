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

    // ---------- CRIT / STAGGER (BLINK) ----------
    [Header("Crit (Blink + Micro Stagger)")]
    [Tooltip("Если true — враг может входить в крит-мигание и микро-стаггер от попаданий.")]
    public bool canBeCritStaggered = true;

    [Tooltip("Дефолтный интервал мигания (сек). Снаряд может переопределить.")]
    public float defaultCritBlinkInterval = 0.12f;

    [Tooltip("Если крит уже активен и прилетает новый — продлеваем.")]
    public bool extendCritOnRehit = true;

    [Header("Micro Stagger")]
    [Tooltip("Какая доля от critDuration идёт в микро-стаггер. 0.35 = 35% от critDuration.")]
    [Range(0f, 1f)]
    public float staggerFractionOfCritDuration = 0.35f;

    [Tooltip("Минимальная длительность стаггера (сек), чтобы был заметен даже при коротком крите.")]
    public float staggerMinDuration = 0.06f;

    [Tooltip("Максимальная длительность стаггера (сек), чтобы не превращалось в стан.")]
    public float staggerMaxDuration = 0.25f;

    private float _staggerUntilTime;
    public bool IsStaggered => !isDead && Time.time < _staggerUntilTime;

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

    // ---------- DAMAGE TEXT ----------
    [Header("Damage Text")]
    [Tooltip("Префаб с TMP_Text + DamageTextPopup.")]
    public GameObject damageTextPrefab;

    [Tooltip("Мировое смещение над врагом, откуда вылетают цифры.")]
    public Vector3 damageTextOffset = new Vector3(0f, 1.1f, 0f);

    [Tooltip("Небольшой разброс вокруг offset, чтобы цифры не налезали друг на друга.")]
    public float damageTextRandomRadius = 0.25f;

    // ---------- INTERNAL ----------
    private SpriteRenderer sr;
    private Color baseColor;
    private bool isDead;
    public bool IsDead => isDead;

    private AudioSource audioSource;
    private float lastHitSoundTime = -999f;

    private Coroutine _hitFlashRoutine;
    private Coroutine _stopParticlesRoutine;

    private Canvas _cachedDamageCanvas;

    // crit blink runtime
    private Coroutine _critBlinkRoutine;
    private float _critUntilTime;
    private bool _baseRendererEnabled = true;

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
        _baseRendererEnabled = sr.enabled;

        currentHealth = maxHealth;
        if (hpBar) hpBar.SetMax(maxHealth);
    }

    // ---------------- PUBLIC DAMAGE API ----------------

    /// <summary>
    /// Старый контракт — НЕ ломаем. Без крита.
    /// </summary>
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, 0f, 0f, -1f, 1f);
    }

    /// <summary>
    /// Старый расширенный контракт — НЕ ломаем. Множитель стаггера = 1.
    /// </summary>
    public void TakeDamage(int amount, float critChance, float critDuration, float critBlinkInterval = -1f)
    {
        TakeDamage(amount, critChance, critDuration, critBlinkInterval, 1f);
    }

    /// <summary>
    /// Новый контракт: крит + множитель микро-стаггера (для префабов снарядов).
    /// staggerMultiplier: 0 = без стаггера, 1 = норм, 2 = сильнее.
    /// </summary>
    public void TakeDamage(int amount, float critChance, float critDuration, float critBlinkInterval, float staggerMultiplier)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // ----- цифра урона -----
        ShowDamageNumber(amount);

        // обычный hit flash (тинт)
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

        // ---------- CRIT / MICRO STAGGER ----------
        TryApplyCritFromHit(critChance, critDuration, critBlinkInterval, staggerMultiplier);

        if (hpBar) hpBar.SetValue(currentHealth);

        TryPlayHitSound();

        if (currentHealth <= 0)
            Die();
    }

    private void TryApplyCritFromHit(float critChance, float critDuration, float critBlinkInterval, float staggerMultiplier)
    {
        if (!canBeCritStaggered) return;
        if (isDead) return;

        if (critChance <= 0f) return;
        if (critDuration <= 0f) return;

        if (UnityEngine.Random.value > Mathf.Clamp01(critChance))
            return;

        float interval = (critBlinkInterval > 0f) ? critBlinkInterval : Mathf.Max(0.01f, defaultCritBlinkInterval);
        ApplyCritBlink(critDuration, interval, staggerMultiplier);
    }

    /// <summary>
    /// Старый метод — оставляем (совместимость).
    /// </summary>
    public void ApplyCritBlink(float critDuration, float blinkInterval = -1f)
    {
        ApplyCritBlink(critDuration, blinkInterval, 1f);
    }

    /// <summary>
    /// Новый метод: крит-мигание + микро-стаггер с множителем.
    /// </summary>
    public void ApplyCritBlink(float critDuration, float blinkInterval, float staggerMultiplier)
    {
        if (!canBeCritStaggered || isDead) return;

        float interval = (blinkInterval > 0f) ? blinkInterval : Mathf.Max(0.01f, defaultCritBlinkInterval);
        float dur = Mathf.Max(0.01f, critDuration);

        // 1) Визуал (мигание) — до Time.time + dur
        float newCritUntil = Time.time + dur;

        if (_critBlinkRoutine == null)
        {
            _critUntilTime = newCritUntil;
            _critBlinkRoutine = StartCoroutine(CritBlinkRoutine(interval));
        }
        else
        {
            if (extendCritOnRehit)
                _critUntilTime = Mathf.Max(_critUntilTime, newCritUntil);
        }

        // 2) Микро-стаггер (пауза логики врага) + множитель от снаряда
        float m = Mathf.Max(0f, staggerMultiplier);
        if (m <= 0f) return; // снаряд может хотеть "крит-мигание без стаггера"

        float staggerDur = dur * Mathf.Clamp01(staggerFractionOfCritDuration) * m;

        // clamp (тоже масштабируем)
        float min = Mathf.Max(0f, staggerMinDuration) * m;
        float max = Mathf.Max(staggerMinDuration, staggerMaxDuration) * m;
        staggerDur = Mathf.Clamp(staggerDur, min, max);

        float newStaggerUntil = Time.time + staggerDur;

        if (extendCritOnRehit)
            _staggerUntilTime = Mathf.Max(_staggerUntilTime, newStaggerUntil);
        else
            _staggerUntilTime = newStaggerUntil;
    }

    private IEnumerator CritBlinkRoutine(float interval)
    {
        if (sr) _baseRendererEnabled = sr.enabled;

        bool visible = true;
        while (!isDead && Time.time < _critUntilTime)
        {
            if (sr)
            {
                visible = !visible;
                sr.enabled = visible;
            }
            yield return new WaitForSeconds(interval);
        }

        if (sr)
        {
            sr.enabled = _baseRendererEnabled;
            sr.color = baseColor;
        }

        _critBlinkRoutine = null;
    }

    // ---------------- FREEZE ----------------

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

    // ---------------- SOUND / FX ----------------

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

        if (sr) sr.color = fireTint;
        yield return new WaitForSeconds(half);

        float t = 0f;
        while (t < half)
        {
            if (sr) sr.color = Color.Lerp(fireTint, baseColor, t / half);
            t += Time.deltaTime;
            yield return null;
        }
        if (sr) sr.color = baseColor;

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

        // снимаем крит-мигание
        if (_critBlinkRoutine != null) StopCoroutine(_critBlinkRoutine);
        _critBlinkRoutine = null;
        if (sr)
        {
            sr.enabled = _baseRendererEnabled;
            sr.color = baseColor;
        }

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

    // ---------- DAMAGE TEXT LOGIC ----------

    private Canvas GetDamageCanvas()
    {
        if (_cachedDamageCanvas != null) return _cachedDamageCanvas;
        _cachedDamageCanvas = FindFirstObjectByType<Canvas>();
        return _cachedDamageCanvas;
    }

    private void ShowDamageNumber(int amount)
    {
        if (!damageTextPrefab) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = transform.position + damageTextOffset;

        if (damageTextRandomRadius > 0f)
        {
            Vector2 rnd = UnityEngine.Random.insideUnitCircle * damageTextRandomRadius;
            worldPos += new Vector3(rnd.x, rnd.y, 0f);
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        Canvas canvas = GetDamageCanvas();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;

        GameObject go = Instantiate(damageTextPrefab, canvas.transform);
        RectTransform rect = go.transform as RectTransform;

        if (rect != null && canvasRect != null)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out localPos
            );

            rect.anchoredPosition = localPos;
        }
        else
        {
            go.transform.position = screenPos;
        }

        var popup = go.GetComponent<DamageTextPopup>();
        if (popup != null)
            popup.Setup(amount);
    }
}
