using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ChargeDotsCooldownUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerFireballShooter shooter;

    [Tooltip("Если пусто — соберём автоматически все дочерние Image под объектами с именем 'Cooldown'.")]
    [SerializeField] private List<Image> cooldownOverlays = new List<Image>();

    [Header("Visual")]
    [Tooltip("Непрозрачность оверлея на старте кулдауна (0..1)")]
    [Range(0f, 1f)] public float startAlpha = 0.65f;

    [Tooltip("Минимальная непрозрачность к концу кулдауна (0..1)")]
    [Range(0f, 1f)] public float endAlpha = 0.0f;

    [Tooltip("С какой стороны начинать заливку (Top=2 обычно выглядит привычно)")]
    public Image.Origin360 origin = Image.Origin360.Top;

    private void Awake()
    {
        AutoWireIfNeeded();
    }

    private void OnValidate()
    {
        // держим значения в адекватных пределах
        if (endAlpha > startAlpha) endAlpha = 0f;
        AutoWireIfNeeded();
        EnsureImagesSetup();
    }

    private void AutoWireIfNeeded()
    {
        if (shooter == null)
        {
            shooter = FindObjectOfType<PlayerFireballShooter>();
        }

        if (cooldownOverlays == null) cooldownOverlays = new List<Image>();

        // Если список пуст — пытаемся собрать все дочерние "Cooldown"
        if (cooldownOverlays.Count == 0)
        {
            cooldownOverlays = new List<Image>();
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.transform.name.Equals("Cooldown"))
                {
                    cooldownOverlays.Add(img);
                }
            }
            // лёгкая сортировка по порядку в иерархии (чтобы Dot1/Dot2/Dot3 шли правильно)
            cooldownOverlays.Sort((a, b) =>
                a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        }
    }

    private void EnsureImagesSetup()
    {
        if (cooldownOverlays == null) return;

        foreach (var img in cooldownOverlays)
        {
            if (img == null) continue;
            img.raycastTarget = false;                  // не блокируем клики
            img.type = Image.Type.Filled;               // будет сектор
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)origin;               // откуда стартует сектор
            img.fillClockwise = true;                   // как часы
            img.fillAmount = 0f;                        // по умолчанию скрыт
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    private void LateUpdate()
    {
        if (shooter == null || cooldownOverlays == null || cooldownOverlays.Count == 0) return;

        // Эти два свойства должны быть в PlayerFireballShooter (см. блок ниже)
        float t = shooter.CooldownNormalized;              // 1..0
        bool active = shooter.IsOnCooldown && t > 0.0001f; // есть кулдаун?

        foreach (var img in cooldownOverlays)
        {
            if (img == null) continue;

            if (!active)
            {
                img.enabled = false;
                continue;
            }

            img.enabled = true;
            // сектор "сдувается": 1 -> 0
            img.fillAmount = t;

            // альфа плавно убывает (по желанию)
            var c = img.color;
            c.a = Mathf.Lerp(endAlpha, startAlpha, t);
            img.color = c;
        }
    }
}
