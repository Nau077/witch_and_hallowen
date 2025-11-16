using UnityEngine;
using TMPro;

/// <summary>
/// Простая всплывающая циферка урона.
/// Висит на объекте с TMP_Text и сам себя уничтожает.
/// </summary>
public class DamageTextPopup : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("Скорость подъёма текста вверх (в юнитах/сек).")]
    public float moveUpSpeed = 1.8f;

    [Header("Lifetime")]
    [Tooltip("Сколько времени текст просто висит, прежде чем начнет исчезать.")]
    public float stayDuration = 0.15f;

    [Tooltip("Сколько времени занимает полное исчезновение (альфа 1 → 0).")]
    public float fadeDuration = 0.35f;

    private TMP_Text _text;
    private Color _startColor;
    private float _time;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        if (_text != null)
        {
            _startColor = _text.color;
        }
    }

    /// <summary>
    /// Вызываем сразу после Instantiate, чтобы записать число урона.
    /// </summary>
    public void Setup(int amount)
    {
        if (_text == null) _text = GetComponent<TMP_Text>();

        if (_text != null)
        {
            _text.text = amount.ToString();
            _startColor = _text.color;
        }
    }

    /// <summary>
    /// Вариант с указанием цвета (на будущее, можно не использовать).
    /// </summary>
    public void Setup(int amount, Color color)
    {
        if (_text == null) _text = GetComponent<TMP_Text>();

        if (_text != null)
        {
            _text.text = amount.ToString();
            _text.color = color;
            _startColor = color;
        }
    }

    void Update()
    {
        // Легко подпрыгиваем вверх
        transform.position += Vector3.up * moveUpSpeed * Time.deltaTime;

        _time += Time.deltaTime;

        if (_time <= stayDuration)
            return;

        // Фейд-аут
        float t = (_time - stayDuration) / Mathf.Max(0.01f, fadeDuration);
        t = Mathf.Clamp01(t);

        if (_text != null)
        {
            Color c = _startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            _text.color = c;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
