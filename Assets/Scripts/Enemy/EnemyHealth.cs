// Assets/Scripts/Enemy/EnemyHealth.cs
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

    [Header("Hit Effect (regular hit flash)")]
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

    // ---------- CRIT / STAGGER ----------
    [Header("Crit (Color Blink + Stagger)")]
    public bool canBeCritStaggered = true;

    [Tooltip("Дефолтный интервал мигания (сек). Снаряд может переопределить.")]
    public float defaultCritBlinkInterval = 0.12f;

    [Tooltip("Если крит уже активен и прилетает новый — продлеваем.")]
    public bool extendCritOnRehit = true;

    [Tooltip("Если true — стаггер = ВСЯ длительность крита (пока мигает — стоит и не атакует).")]
    public bool lockMovementAndAttackForFullCritDuration = true;

    [Tooltip("Если lockMovementAndAttackForFullCritDuration выключен, то стаггер = critDuration * lockFraction.")]
    [Range(0f, 1f)]
    public float lockFractionOfCritDuration = 0.35f;

    [Tooltip("Минимальный стаггер (сек)")]
    public float lockMinDuration = 0.06f;

    [Tooltip("Максимальный стаггер (сек)")]
    public float lockMaxDuration = 0.25f;

    private float _staggerUntilTime;
    public bool IsStaggered => !isDead && Time.time < _staggerUntilTime;

    // ---------- ICE / FREEZE ----------
    [Header("Ice Freeze")]
    public bool canBeFrozen = true;
    public GameObject freezeVfxPrefab;
    public Vector2 freezeVfxOffset = new Vector2(0f, 0.3f);

    private bool isFrozen;
    public bool IsFrozen => isFrozen;

    private GameObject _currentFreezeVfx;
    private Coroutine _freezeRoutine;

    // ---------- DAMAGE TEXT ----------
    [Header("Damage Text")]
    public GameObject damageTextPrefab;
    public Vector3 damageTextOffset = new Vector3(0f, 1.1f, 0f);
    public float damageTextRandomRadius = 0.25f;

    [Header("Hit Knockback")]
    [SerializeField] private bool enableHitKnockback = true;
    [SerializeField] private float knockbackCells = 1f;

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

    private Coroutine _critBlinkRoutine;
    private float _critUntilTime;
    private bool _critBlinkOn;
    private Color _critBlinkColor = Color.white;

    private EnemyWalker _walkerCached;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        _walkerCached = GetComponent<EnemyWalker>();

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

        baseColor = sr ? sr.color : Color.white;

        currentHealth = maxHealth;
        if (hpBar) hpBar.SetMax(maxHealth);
    }

    // ---------------- PUBLIC DAMAGE API ----------------

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, 0f, 0f, -1f, 1f, Color.white);
    }

    public void TakeDamage(int amount, float critChance, float critDuration, float critBlinkInterval = -1f)
    {
        TakeDamage(amount, critChance, critDuration, critBlinkInterval, 1f, Color.white);
    }

    public void TakeDamage(int amount, float critChance, float critDuration, float critBlinkInterval, float staggerMultiplier)
    {
        TakeDamage(amount, critChance, critDuration, critBlinkInterval, staggerMultiplier, Color.white);
    }

    public void TakeDamage(int amount, float critChance, float critDuration, float critBlinkInterval, float staggerMultiplier, Color critBlinkColor)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        bool isCrit = TryRollCrit(critChance, critDuration);
        ShowDamageNumber(amount, isCrit);

        // обычный hit-flash НЕ должен "ломать" крит-цвет — поэтому:
        // если сейчас идет крит-мигание, хит-флэш не запускаем
        if (_critBlinkRoutine == null)
        {
            if (_hitFlashRoutine != null)
                StopCoroutine(_hitFlashRoutine);
            _hitFlashRoutine = StartCoroutine(HitFlash());
        }

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

        if (isCrit)
        {
            float interval = (critBlinkInterval > 0f) ? critBlinkInterval : Mathf.Max(0.01f, defaultCritBlinkInterval);
            ApplyCritBlink(critDuration, interval, staggerMultiplier, critBlinkColor);
        }

        TryApplyHitKnockback();

        if (hpBar) hpBar.SetValue(currentHealth);

        TryPlayHitSound();

        if (currentHealth <= 0)
            Die();
    }

    private bool TryRollCrit(float critChance, float critDuration)
    {
        if (!canBeCritStaggered) return false;
        if (isDead) return false;

        if (critChance <= 0f) return false;
        if (critDuration <= 0f) return false;

        if (UnityEngine.Random.value > Mathf.Clamp01(critChance))
            return false;

        return true;
    }

    public void ApplyCritBlink(float critDuration, float blinkInterval = -1f)
    {
        ApplyCritBlink(critDuration, blinkInterval, 1f, Color.white);
    }

    public void ApplyCritBlink(float critDuration, float blinkInterval, float staggerMultiplier)
    {
        ApplyCritBlink(critDuration, blinkInterval, staggerMultiplier, Color.white);
    }

    /// <summary>
    /// Крит: мигание цветом + стаггер + мгновенный interrupt атаки.
    /// </summary>
    public void ApplyCritBlink(float critDuration, float blinkInterval, float staggerMultiplier, Color critBlinkColor)
    {
        if (!canBeCritStaggered || isDead) return;

        float dur = Mathf.Max(0.01f, critDuration);
        float interval = (blinkInterval > 0f) ? blinkInterval : Mathf.Max(0.01f, defaultCritBlinkInterval);

        // цвет (если пришёл без альфы — делаем альфу 1)
        _critBlinkColor = (critBlinkColor.a <= 0f)
            ? new Color(critBlinkColor.r, critBlinkColor.g, critBlinkColor.b, 1f)
            : critBlinkColor;

        // 1) Таймер мигания
        float newCritUntil = Time.time + dur;

        if (_critBlinkRoutine == null)
        {
            _critUntilTime = newCritUntil;
            _critBlinkOn = false;
            _critBlinkRoutine = StartCoroutine(CritBlinkRoutine(interval));
        }
        else
        {
            if (extendCritOnRehit)
                _critUntilTime = Mathf.Max(_critUntilTime, newCritUntil);
        }

        // 2) Стаггер (блок движения/атаки) + множитель от снаряда
        float m = Mathf.Max(0f, staggerMultiplier);
        if (m > 0f)
        {
            float staggerDur;

            if (lockMovementAndAttackForFullCritDuration)
            {
                staggerDur = dur;
            }
            else
            {
                staggerDur = dur * Mathf.Clamp01(lockFractionOfCritDuration);
                staggerDur = Mathf.Clamp(staggerDur, lockMinDuration, lockMaxDuration);
            }

            staggerDur *= m;

            float newStaggerUntil = Time.time + staggerDur;

            if (extendCritOnRehit)
                _staggerUntilTime = Mathf.Max(_staggerUntilTime, newStaggerUntil);
            else
                _staggerUntilTime = newStaggerUntil;

            // 3) МГНОВЕННО прерываем текущую атаку/скиллы (в этот же кадр)
            if (_walkerCached != null)
                _walkerCached.ForceInterruptFromExternalStagger();
        }
    }

    private IEnumerator CritBlinkRoutine(float interval)
    {
        while (!isDead && Time.time < _critUntilTime)
        {
            if (sr)
            {
                _critBlinkOn = !_critBlinkOn;
                sr.color = _critBlinkOn ? _critBlinkColor : baseColor;
            }
            yield return new WaitForSeconds(interval);
        }

        if (sr) sr.color = baseColor;
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

        if (_critBlinkRoutine != null) StopCoroutine(_critBlinkRoutine);
        _critBlinkRoutine = null;
        if (sr) sr.color = baseColor;

        if (_freezeRoutine != null) StopCoroutine(_freezeRoutine);
        isFrozen = false;
        if (_currentFreezeVfx != null)
        {
            Destroy(_currentFreezeVfx);
            _currentFreezeVfx = null;
        }

        if (_walkerCached) _walkerCached.OnDeathExternal();

        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>(); if (rb) rb.simulated = false;
        if (_walkerCached) _walkerCached.enabled = false;

        // Always set dead sprite as a reliable fallback.
        if (deadSprite && sr) sr.sprite = deadSprite;

        var anim = GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController && HasTriggerParameter(anim, "Die"))
            anim.SetTrigger("Die");

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

    private static bool HasTriggerParameter(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;

        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger &&
                parameters[i].name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    // ---------- DAMAGE TEXT ----------

    private Canvas GetDamageCanvas()
    {
        if (_cachedDamageCanvas != null) return _cachedDamageCanvas;
        _cachedDamageCanvas = FindFirstObjectByType<Canvas>();
        return _cachedDamageCanvas;
    }

    private void ShowDamageNumber(int amount, bool isCrit)
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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out Vector2 localPos
            );
            rect.anchoredPosition = localPos;
        }
        else
        {
            go.transform.position = screenPos;
        }

        var popup = go.GetComponent<DamageTextPopup>();
        if (popup != null)
            popup.Setup(amount, isCrit);
    }

    private void TryApplyHitKnockback()
    {
        if (!enableHitKnockback || isDead)
            return;

        if (_walkerCached == null)
            return;

        Transform playerTr = _walkerCached.PlayerTransform;
        if (playerTr == null)
            return;

        float cell = Mathf.Max(0.01f, _walkerCached.cellSize);
        float distance = Mathf.Max(0.1f, knockbackCells) * cell;

        Vector2 current = transform.position;
        Vector2 away = current - (Vector2)playerTr.position;
        if (away.sqrMagnitude <= 0.000001f)
            away = Vector2.right;
        else
            away.Normalize();

        Vector2 target = current + away * distance;
        Vector2 clamped = _walkerCached.DebugClampToBounds(target);

        transform.position = clamped;
    }
}
