using UnityEngine;

[DisallowMultipleComponent]
public class ClickableGlowOutlineSprite : MonoBehaviour
{
    [Header("Target")]
    public SpriteRenderer target;

    [Header("Glow look")]
    public Color glowColor = new Color(1f, 1f, 1f, 1f);
    [Range(0f, 1f)] public float minAlpha = 0.35f;
    [Range(0f, 1f)] public float maxAlpha = 0.85f;
    public float pulseSpeed = 2.5f;

    [Tooltip("How much bigger the glow sprite is compared to the target (local scale multiplier).")]
    [Range(1.01f, 1.30f)] public float glowScale = 1.04f;

    [Header("Diagonal shine")]
    public Color shineColor = new Color(1f, 1f, 1f, 1f);
    [Range(0f, 1f)] public float shineMaxAlpha = 0.45f;
    [Range(0.01f, 0.25f)] public float shineOffset = 0.08f;
    public float shineSpeed = 1.6f;

    [Header("Target scale pulse")]
    public bool animateTargetScale = true;
    [Range(0f, 0.12f)] public float scalePulseAmount = 0.035f;
    public float scalePulseSpeed = 2.2f;

    [Header("Sorting")]
    public int sortingOrderOffset = 1;

    [Header("URP 2D")]
    public bool forceUnlit = true;

    SpriteRenderer _glowSr;
    SpriteRenderer _shineSr;
    Transform _glowTf;
    Transform _shineTf;
    Color _baseTargetColor;
    Vector3 _baseTargetLocalScale;
    Material _runtimeGlowMat;
    Material _runtimeShineMat;
    bool _inited;

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

        CreateGlow();
        CreateShine();

        _inited = true;
    }

    void OnEnable()
    {
        if (!_inited || target == null) return;
        _baseTargetColor = target.color;
        _baseTargetLocalScale = target.transform.localScale;
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
        if (_glowSr == null || _shineSr == null || target == null) return;

        SyncRenderersWithTarget();

        float t = Time.unscaledTime;

        // Pulse alpha for persistent click hint.
        float pulse01 = (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f;
        float glowAlpha = Mathf.Lerp(minAlpha, maxAlpha, pulse01);

        Color glow = glowColor;
        glow.a = glowAlpha;
        _glowSr.color = glow;

        // Diagonal shine sweep.
        float sweep01 = Mathf.PingPong(t * shineSpeed, 1f);
        float diag = sweep01 * 2f - 1f;
        _shineTf.localPosition = new Vector3(diag * shineOffset, -diag * shineOffset, 0f);

        float shineFade = Mathf.Sin(sweep01 * Mathf.PI); // 0..1..0
        Color shine = shineColor;
        shine.a = shineMaxAlpha * shineFade;
        _shineSr.color = shine;

        if (animateTargetScale)
        {
            float scalePulse = 1f + Mathf.Sin(t * scalePulseSpeed) * scalePulseAmount;
            target.transform.localScale = _baseTargetLocalScale * scalePulse;
        }

        _glowSr.enabled = true;
        _shineSr.enabled = true;
    }

    void SyncRenderersWithTarget()
    {
        if (_glowSr.sprite != target.sprite) _glowSr.sprite = target.sprite;
        if (_shineSr.sprite != target.sprite) _shineSr.sprite = target.sprite;

        _glowTf.localScale = Vector3.one * glowScale;
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
