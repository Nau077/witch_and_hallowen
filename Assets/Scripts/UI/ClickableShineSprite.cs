using UnityEngine;

[DisallowMultipleComponent]
public class ClickableGlowOutlineSprite : MonoBehaviour
{
    [Header("Target")]
    public SpriteRenderer target;

    [Header("Glow look")]
    public Color glowColor = new Color(1f, 1f, 1f, 1f);
    [Range(0f, 1f)] public float minAlpha = 0.986f;
    [Range(0f, 1f)] public float maxAlpha = 0.65f;
    public float pulseSpeed = 2.5f;

    [Tooltip("How much bigger the glow sprite is compared to the target (local scale multiplier).")]
    [Range(1.01f, 1.30f)] public float glowScale = 1.01f;

    [Header("Sorting")]
    public int sortingOrderOffset = -1; // обычно обводка позади (ниже), но можно +1

    [Header("URP 2D")]
    public bool forceUnlit = true;

    // internals
    SpriteRenderer _glowSr;
    Transform _glowTf;
    Color _baseTargetColor;
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
        CreateGlow();
        _inited = true;
    }

    void OnDisable()
    {
        if (!_inited) return;
        if (target != null) target.color = _baseTargetColor;
        if (_glowSr != null) _glowSr.enabled = false;
    }

    void Update()
    {
        if (_glowSr == null || target == null) return;

        // синхронизируем спрайт (если он меняется)
        if (_glowSr.sprite != target.sprite) _glowSr.sprite = target.sprite;

        // позиция/поворот/скейл как у target, но чуть больше
        _glowTf.position = target.transform.position;
        _glowTf.rotation = target.transform.rotation;

        Vector3 baseScale = target.transform.lossyScale;
        // lossyScale напрямую не выставить, поэтому держим glow как дочерний объект target:
        // (мы создали его дочерним, так что здесь достаточно localScale)
        _glowTf.localScale = Vector3.one * glowScale;

        // пульс альфы
        float s = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f; // 0..1
        float a = Mathf.Lerp(minAlpha, maxAlpha, s);

        Color c = glowColor;
        c.a = a;
        _glowSr.color = c;

        _glowSr.enabled = true;
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

        // sorting
        _glowSr.sortingLayerID = target.sortingLayerID;
        _glowSr.sortingOrder = target.sortingOrder + sortingOrderOffset;

        if (forceUnlit)
        {
            var unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            if (unlit != null)
                _glowSr.material = new Material(unlit);
        }
    }
}
