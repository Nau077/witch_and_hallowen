using UnityEngine;

[ExecuteAlways] // обновляет позицию и в редакторе
public class HealthBar2D : MonoBehaviour
{
    [Header("Refs")]
    public Transform fill;

    [Header("Look & Pos")]
    public bool useLocalOffset = true;             // ✅ по умолчанию локально
    public Vector3 offset = new Vector3(0f, 1.0f, 0f);
    public float width = 1.0f;
    public bool faceCamera = false;

    int max = 1;
    int cur = 1;
    Transform target; // родитель

    void OnEnable()
    {
        target = transform.parent;
        ApplyTransform();
        UpdateFill(cur, Mathf.Max(max, 1));
    }

    void Update()
    {
        // в Edit и Play режимах держим совпадение Scene/Game
        ApplyTransform();
        if (faceCamera && Camera.main)
            transform.rotation = Camera.main.transform.rotation;
        else
            transform.rotation = Quaternion.identity;
    }

    void ApplyTransform()
    {
        if (!target) target = transform.parent;

        if (useLocalOffset)
        {
            // 🚩 локальная привязка — то, что ты видишь в Scene, будет и в Game
            transform.localPosition = offset;
        }
        else if (target)
        {
            // мировой офсет от родителя
            transform.position = target.position + offset;
        }
    }

    public void SetMax(int maxHealth)
    {
        max = Mathf.Max(1, maxHealth);
        SetValue(maxHealth);
    }

    public void SetValue(int value)
    {
        cur = Mathf.Clamp(value, 0, Mathf.Max(1, max));
        UpdateFill(cur, max);
    }

    void UpdateFill(int current, int maximum)
    {
        if (!fill) return;

        float k = Mathf.Clamp01((float)current / maximum);
        float newW = Mathf.Max(0.0001f, k * Mathf.Max(0.01f, width));

        // масштаб по X
        var s = fill.localScale;
        s.x = newW;
        fill.localScale = s;

        // якорим левый край: левый = -width/2, правый двигается
        var p = fill.localPosition;
        p.x = (-width * 0.5f) + (newW * 0.5f);
        fill.localPosition = p;
    }

    public void Hide() => gameObject.SetActive(false);
}
