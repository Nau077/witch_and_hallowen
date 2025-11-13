using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth health;          // перетащи Player
    public Image fill;                   // красный внутренний Image
    public TextMeshProUGUI text;         // HealthText справа (опционально)

    [Header("Blink")]
    [Tooltip("При каком проценте здоровья начинать мигание (0.3 = 30%)")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.30f;
    public float lowBlinkSpeed = 8f;
    public Color lowHealthColor = new Color(1f, 0.4f, 0.4f); // более яркий красный

    [Header("Scale Pulse")]
    [Tooltip("Насколько сильно СЖИМАТЬ полоску при низком здоровье.")]
    [Range(0f, 0.5f)] public float scaleAmplitude = 0.15f;

    private Color _fillBaseColor;
    private Color _textBaseColor;
    private Vector3 _baseScale;

    private void Reset()
    {
        if (health == null) health = FindObjectOfType<PlayerHealth>();
        if (fill == null) fill = GetComponentInChildren<Image>();
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        if (health == null) health = FindObjectOfType<PlayerHealth>();
        if (fill != null) _fillBaseColor = fill.color;
        if (text != null) _textBaseColor = text.color;
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        if (health == null || fill == null) return;

        // режим заливки
        if (fill.type != Image.Type.Filled) fill.type = Image.Type.Filled;
        if (fill.fillMethod != Image.FillMethod.Horizontal) fill.fillMethod = Image.FillMethod.Horizontal;

        float normalized = health.Normalized;
        fill.fillAmount = normalized;

        // цифры 35/50
        if (text != null)
        {
            text.text = $"{health.currentHealth}/{health.maxHealth}";
        }

        bool isLow = normalized <= lowHealthThreshold && health.currentHealth > 0;

        if (isLow)
        {
            // 0..1..0..1..0
            float pulse = (Mathf.Sin(Time.time * lowBlinkSpeed) + 1f) * 0.5f;

            // цвет мигает между обычным и «тревожным» красным, НО без изменения альфы
            Color c = Color.Lerp(_fillBaseColor, lowHealthColor, pulse);
            fill.color = c;

            if (text != null)
            {
                Color tc = Color.Lerp(_textBaseColor, lowHealthColor, pulse);
                text.color = tc;
            }

            // полоска СЖИМАЕТСЯ, но не становится больше исходного размера
            float scaleFactor = 1f - pulse * scaleAmplitude; // 1 → (1 - amp)
            transform.localScale = _baseScale * scaleFactor;
        }
        else
        {
            fill.color = _fillBaseColor;
            if (text != null) text.color = _textBaseColor;
            transform.localScale = _baseScale;
        }
    }
}
