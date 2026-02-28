using UnityEngine;

[DisallowMultipleComponent]
public class ClickableGlowOutlineSprite : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SpriteRenderer target;

    [Header("Glow look")]
    [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.85f;
    [SerializeField] private float pulseSpeed = 2.5f;

    [Tooltip("How much bigger the glow sprite is compared to the target (local scale multiplier).")]
    [SerializeField, Range(1.01f, 1.30f)] private float glowScale = 1.04f;

    [Header("Diagonal shine")]
    [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float shineMaxAlpha = 0.45f;
    [SerializeField, Range(0.01f, 0.25f)] private float shineOffset = 0.08f;
    [SerializeField] private float shineSpeed = 1.6f;

    [Header("Target scale pulse")]
    [SerializeField] private bool animateTargetScale = true;
    [SerializeField, Range(1f, 1.35f)] private float hoverScaleMultiplier = 1.12f;
    [SerializeField, Range(0f, 0.15f)] private float scalePulseAmount = 0.035f;
    [SerializeField, Min(0.1f)] private float scalePulseSpeed = 2.2f;
    [SerializeField, Min(0.1f)] private float hoverScaleLerpSpeed = 8f;

    [Header("Simple Hover (No Duplicate)")]
    [SerializeField] private bool useSimpleHoverNoDuplicate = true;
    [SerializeField, Range(0f, 1f)] private float hoverTintStrength = 0.28f;
    [SerializeField, Min(0.1f)] private float hoverTintPulseSpeed = 2f;

    [Header("Hover Boost")]
    [SerializeField] private bool useHoverBoost = true;
    [SerializeField, Range(0f, 1f)] private float hoverMinAlpha = 0.65f;
    [SerializeField, Range(0f, 1f)] private float hoverMaxAlpha = 1f;
    [SerializeField] private float hoverPulseSpeed = 1.05f;
    [SerializeField, Range(0f, 1f)] private float hoverShineMaxAlpha = 0.75f;
    [SerializeField] private float hoverShineSpeed = 0.75f;
    [SerializeField, Min(0f)] private float hoverHitPaddingWorld = 0.2f;
    [SerializeField, Range(1f, 1.6f)] private float hoverGlowScaleMultiplier = 1.08f;

    [Header("Sorting")]
    [SerializeField] private int sortingOrderOffset = 1;

    [Header("URP 2D")]
    [SerializeField] private bool forceUnlit = true;
    [SerializeField] private bool keepShineCenteredOnHover = true;

    SpriteRenderer _glowSr;
    SpriteRenderer _shineSr;
    Transform _glowTf;
    Transform _shineTf;
    Color _baseTargetColor;
    Vector3 _baseTargetLocalScale;
    Material _runtimeGlowMat;
    Material _runtimeShineMat;
    bool _inited;
    bool _isHovered;
    float _currentScaleMul = 1f;

    void Reset()
    {
        target = GetComponent<SpriteRenderer>();
    }

    void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (target == null)
        {
            Debug.LogError($"{nameof(ClickableGlowOutlineSprite)}: No SpriteRenderer on {name}");
            enabled = false;
            return;
        }

        _baseTargetColor = target.color;
        _baseTargetLocalScale = target.transform.localScale;
        if (scalePulseAmount <= 0f) scalePulseAmount = 0.035f;
        if (scalePulseSpeed <= 0f) scalePulseSpeed = 2.2f;

        if (sortingOrderOffset <= 0)
            sortingOrderOffset = 1;

        if (!useSimpleHoverNoDuplicate)
        {
            CreateGlow();
            CreateShine();
        }

        _currentScaleMul = 1f;

        _inited = true;
    }

    void OnEnable()
    {
        if (!_inited || target == null) return;
        _baseTargetColor = target.color;
        _baseTargetLocalScale = target.transform.localScale;
        _currentScaleMul = 1f;
    }

    void OnDisable()
    {
        if (!_inited) return;

        if (target != null)
        {
            target.color = _baseTargetColor;
            target.transform.localScale = _baseTargetLocalScale;
        }

        if (_glowSr != null) _glowSr.enabled = false;
        if (_shineSr != null) _shineSr.enabled = false;
        _isHovered = false;
        _currentScaleMul = 1f;
    }

    void OnDestroy()
    {
        if (target != null)
            target.transform.localScale = _baseTargetLocalScale;

        if (_runtimeGlowMat != null)
            Destroy(_runtimeGlowMat);

        if (_runtimeShineMat != null)
            Destroy(_runtimeShineMat);
    }

    void Update()
    {
        if (target == null) return;

        _isHovered = IsMouseOverTarget();

        if (useSimpleHoverNoDuplicate)
        {
            UpdateSimpleHover();
            return;
        }

        if (_glowSr == null || _shineSr == null) return;

        SyncRenderersWithTarget();

        float t = Time.unscaledTime;

        float pulse = _isHovered && useHoverBoost ? hoverPulseSpeed : pulseSpeed;
        float minA = _isHovered && useHoverBoost ? hoverMinAlpha : minAlpha;
        float maxA = _isHovered && useHoverBoost ? hoverMaxAlpha : maxAlpha;
        float activeShineMax = _isHovered && useHoverBoost ? hoverShineMaxAlpha : shineMaxAlpha;
        float activeShineSpeed = _isHovered && useHoverBoost ? hoverShineSpeed : shineSpeed;
        float activeShineOffset = (_isHovered && useHoverBoost && keepShineCenteredOnHover) ? 0f : shineOffset;

        // Pulse alpha for persistent click hint.
        float pulse01 = (Mathf.Sin(t * pulse) + 1f) * 0.5f;
        float glowAlpha = Mathf.Lerp(minA, maxA, pulse01);

        Color glow = glowColor;
        glow.a = glowAlpha;
        _glowSr.color = glow;

        // Soft shine: no aggressive diagonal offset on hover.
        float sweep01 = Mathf.PingPong(t * activeShineSpeed, 1f);
        float diag = sweep01 * 2f - 1f;
        _shineTf.localPosition = new Vector3(diag * activeShineOffset, -diag * activeShineOffset, 0f);

        float shineFade = Mathf.Sin(sweep01 * Mathf.PI); // 0..1..0
        Color shine = shineColor;
        shine.a = activeShineMax * shineFade;
        _shineSr.color = shine;

        if (animateTargetScale)
        {
            float targetMul = _isHovered ? hoverScaleMultiplier : 1f;
            _currentScaleMul = Mathf.MoveTowards(_currentScaleMul, targetMul, hoverScaleLerpSpeed * Time.unscaledDeltaTime);

            float pulseMul = 1f + Mathf.Sin(Time.unscaledTime * scalePulseSpeed) * scalePulseAmount;
            target.transform.localScale = _baseTargetLocalScale * (_currentScaleMul * pulseMul);
        }
        else
        {
            target.transform.localScale = _baseTargetLocalScale;
        }

        _glowSr.enabled = true;
        _shineSr.enabled = true;
    }

    void UpdateSimpleHover()
    {
        if (animateTargetScale)
        {
            float targetMul = _isHovered ? hoverScaleMultiplier : 1f;
            _currentScaleMul = Mathf.MoveTowards(_currentScaleMul, targetMul, hoverScaleLerpSpeed * Time.unscaledDeltaTime);

            float pulseMul = 1f + Mathf.Sin(Time.unscaledTime * scalePulseSpeed) * scalePulseAmount;
            target.transform.localScale = _baseTargetLocalScale * (_currentScaleMul * pulseMul);
        }
        else
        {
            target.transform.localScale = _baseTargetLocalScale;
        }

        if (_isHovered)
        {
            float pulse01 = (Mathf.Sin(Time.unscaledTime * hoverTintPulseSpeed) + 1f) * 0.5f;
            float tint = hoverTintStrength * (0.65f + 0.35f * pulse01);
            target.color = Color.Lerp(_baseTargetColor, Color.white, tint);
        }
        else
        {
            target.color = _baseTargetColor;
        }
    }

    void OnMouseEnter()
    {
        _isHovered = true;
    }

    void OnMouseExit()
    {
        _isHovered = false;
    }

    bool IsMouseOverTarget()
    {
        Camera cam = null;
        if (target != null)
            cam = target.GetComponentInParent<Camera>();
        if (cam == null || !cam.isActiveAndEnabled)
            cam = Camera.main;
        if (cam == null || !cam.isActiveAndEnabled)
        {
            var all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].isActiveAndEnabled)
                {
                    cam = all[i];
                    break;
                }
            }
        }
        if (cam == null) return _isHovered;

        Vector3 mp = Input.mousePosition;
        float z = Mathf.Abs(cam.transform.position.z - target.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, z));

        Bounds b = target.bounds;
        b.Expand(new Vector3(hoverHitPaddingWorld, hoverHitPaddingWorld, 0f));
        return b.Contains(world);
    }

    void SyncRenderersWithTarget()
    {
        if (_glowSr.sprite != target.sprite) _glowSr.sprite = target.sprite;
        if (_shineSr.sprite != target.sprite) _shineSr.sprite = target.sprite;

        float activeGlowScale = glowScale * (_isHovered && useHoverBoost ? hoverGlowScaleMultiplier : 1f);
        _glowTf.localScale = Vector3.one * activeGlowScale;
        _shineTf.localScale = Vector3.one;

        _glowSr.sortingLayerID = target.sortingLayerID;
        _glowSr.sortingOrder = target.sortingOrder + sortingOrderOffset;

        _shineSr.sortingLayerID = target.sortingLayerID;
        _shineSr.sortingOrder = target.sortingOrder + sortingOrderOffset + 1;
    }

    void CreateGlow()
    {
        var go = new GameObject("ClickableGlow_Outline");
        go.transform.SetParent(target.transform, false);

        _glowTf = go.transform;
        _glowTf.localPosition = Vector3.zero;
        _glowTf.localRotation = Quaternion.identity;
        _glowTf.localScale = Vector3.one * glowScale;

        _glowSr = go.AddComponent<SpriteRenderer>();
        _glowSr.sprite = target.sprite;

        if (forceUnlit)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            if (unlit != null)
            {
                _runtimeGlowMat = new Material(unlit);
                _glowSr.material = _runtimeGlowMat;
            }
        }
    }

    void CreateShine()
    {
        var go = new GameObject("ClickableGlow_Shine");
        go.transform.SetParent(target.transform, false);

        _shineTf = go.transform;
        _shineTf.localPosition = Vector3.zero;
        _shineTf.localRotation = Quaternion.identity;
        _shineTf.localScale = Vector3.one;

        _shineSr = go.AddComponent<SpriteRenderer>();
        _shineSr.sprite = target.sprite;

        if (forceUnlit)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            if (unlit != null)
            {
                _runtimeShineMat = new Material(unlit);
                _shineSr.material = _runtimeShineMat;
            }
        }
    }
}
