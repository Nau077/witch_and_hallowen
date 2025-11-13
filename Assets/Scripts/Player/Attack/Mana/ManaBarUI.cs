using UnityEngine;
using UnityEngine.UI;
using TMPro; // для TextMeshProUGUI

[DisallowMultipleComponent]
public class ManaBarUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMana mana;        // перетащи Player
    public Image fill;             // синий Image (внутренний)
    public TextMeshProUGUI text;   // ManaText справа от полоски (опционально)

    [Header("Blink")]
    [Tooltip("При каком проценте маны начинать мягко мигать (0.1 = 10%)")]
    [Range(0f, 1f)] public float lowManaThreshold = 0.10f;
    public float lowBlinkSpeed = 12f;
    public Color lowManaColor = new Color(0.2f, 0.9f, 1f);
    [Tooltip("Длительность усиленного мигания при попытке броска без маны.")]
    public float noManaFlashDuration = 0.25f;

    // НАСТРОЙКИ ЯРКОСТИ МИГАНИЯ
    [Header("Blink Intensity")]
    [Range(0f, 1f)] public float lowBlinkAlphaMin = 0.05f;
    [Range(0f, 1f)] public float lowBlinkAlphaMax = 1f;

    [Header("Scale Pulse")]
    [Tooltip("Насколько сильно СЖИМАТЬ полоску при низкой мане (0.1 = до 90% от размера).")]
    [Range(0f, 0.5f)] public float scaleAmplitude = 0.12f;

    [Header("Skill Cost Hint")]
    [Tooltip("Сколько маны нужно для текущего активного скилла, чтобы начать замах.")]
    public int minCastCost = 0;    // сюда шутер пишет стоимость скилла

    private Color _fillBaseColor;
    private Color _textBaseColor;
    private float _noManaFlashTimer;
    private Vector3 _baseScale;

    private void Reset()
    {
        if (mana == null) mana = FindObjectOfType<PlayerMana>();
        if (fill == null) fill = GetComponentInChildren<Image>();
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        if (mana == null) mana = FindObjectOfType<PlayerMana>();
        if (fill != null) _fillBaseColor = fill.color;
        if (text != null) _textBaseColor = text.color;
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        if (mana == null || fill == null) return;

        // Настройка режима заливки
        if (fill.type != Image.Type.Filled) fill.type = Image.Type.Filled;
        if (fill.fillMethod != Image.FillMethod.Horizontal) fill.fillMethod = Image.FillMethod.Horizontal;

        float normalized = mana.NormalizedExact; // 0..1
        fill.fillAmount = normalized;

        // Обновляем текст  (10/50)
        if (text != null)
        {
            text.text = $"{mana.currentMana}/{mana.maxMana}";
        }

        // === 1. Усиленное мигание при попытке броска без маны ===
        if (_noManaFlashTimer > 0f)
        {
            _noManaFlashTimer -= Time.deltaTime;

            // Пульс (0..1..0..1..0)
            float pulse = (Mathf.Sin(Time.time * lowBlinkSpeed * 1.5f) + 1f) * 0.5f;

            // Альфа в диапазоне [min, max]
            float alpha = Mathf.Lerp(lowBlinkAlphaMin, lowBlinkAlphaMax, pulse);

            Color c = lowManaColor;
            c.a = alpha;
            fill.color = c;

            if (text != null)
            {
                Color tc = lowManaColor;
                tc.a = Mathf.Lerp(lowBlinkAlphaMin, 1f, pulse);
                text.color = tc;
            }

            // СЖИМАЕМ до (1 - scaleAmplitude), но НЕ БОЛЬШЕ 1
            float scaleFactor = 1f - pulse * scaleAmplitude;   // 1 → (1 - amp)
            transform.localScale = _baseScale * scaleFactor;

            return; // не выполняем обычное мигание
        }

        // === 2. Обычное мигание при низкой мане / нехватке на скилл ===

        bool lowByPercent = normalized <= lowManaThreshold;
        bool lowBySkillCost = (minCastCost > 0 && mana.currentMana < minCastCost);

        bool shouldBlink = lowByPercent || lowBySkillCost;

        if (shouldBlink)
        {
            float pulse = (Mathf.Sin(Time.time * lowBlinkSpeed) + 1f) * 0.5f;

            // Альфа в диапазоне [min, max]
            float alpha = Mathf.Lerp(lowBlinkAlphaMin, lowBlinkAlphaMax, pulse);

            Color c = lowManaColor;
            c.a = alpha;
            fill.color = c;

            if (text != null)
            {
                Color tc = lowManaColor;
                tc.a = Mathf.Lerp(lowBlinkAlphaMin, 1f, pulse);
                text.color = tc;
            }

            // СЖИМАЕМ: 1 → (1 - amp), всегда <= 1
            float scaleFactor = 1f - pulse * scaleAmplitude;
            transform.localScale = _baseScale * scaleFactor;
        }
        else
        {
            // Нормальное состояние
            fill.color = _fillBaseColor;
            if (text != null) text.color = _textBaseColor;
            transform.localScale = _baseScale;
        }
    }

    /// <summary>
    /// Вызови это, когда игрок попытался кастануть, но маны не хватило.
    /// </summary>
    public void FlashNoMana()
    {
        _noManaFlashTimer = noManaFlashDuration;
    }
}
