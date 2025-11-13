using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth health;          // перетащи Player
    public Image fill;                   // красный внутренний Image
    public TextMeshProUGUI text;         // HealthText справа

    [Header("Blink")]
    [Tooltip("При каком проценте здоровья начинать мигание (0.3 = 30%)")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.30f;
    public float lowBlinkSpeed = 8f;
    public Color lowHealthColor = new Color(1f, 0.4f, 0.4f); // мягко-красный

    [Header("Blink Intensity")]
    [Range(0f, 1f)] public float lowBlinkAlphaMin = 0.2f;
    [Range(0f, 1f)] public float lowBlinkAlphaMax = 1f;

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

        if (text != null)
        {
            text.text = $"{health.currentHealth}/{health.maxHealth}";
        }

        // Низкое здоровье?
        bool isLow = normalized <= lowHealthThreshold && health.currentHealth > 0;

        if (isLow)
        {
            float pulse = (Mathf.Sin(Time.time * lowBlinkSpeed) + 1f) * 0.5f;

            float alpha = Mathf.Lerp(lowBlinkAlphaMin, lowBlinkAlphaMax, pulse);

            Color c = lowHealthColor;
            c.a = alpha;
            fill.color = c;

            if (text != null)
            {
                Color tc = lowHealthColor;
                tc.a = Mathf.Lerp(lowBlinkAlphaMin, 1f, pulse);
                text.color = tc;
            }

            // СЖИМАЕМ симметрично, не больше базового размера
            float scaleFactor = 1f - pulse * scaleAmplitude;
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
