using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ChargeDotsCooldownUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerFireballShooter shooter;

    [Tooltip("���� ����� � ������ ������������� ��� �������� Image ��� ��������� � ������ 'Cooldown'.")]
    [SerializeField] private List<Image> cooldownOverlays = new List<Image>();

    [Header("Visual")]
    [Tooltip("�������������� ������� �� ������ �������� (0..1)")]
    [Range(0f, 1f)] public float startAlpha = 0.65f;

    [Tooltip("����������� �������������� � ����� �������� (0..1)")]
    [Range(0f, 1f)] public float endAlpha = 0.0f;

    [Tooltip("� ����� ������� �������� ������� (Top=2 ������ �������� ��������)")]
    public Image.Origin360 origin = Image.Origin360.Top;

    private void Awake()
    {
        AutoWireIfNeeded();
    }

    private void OnValidate()
    {
        // ������ �������� � ���������� ��������
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

        // ���� ������ ���� � �������� ������� ��� �������� "Cooldown"
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
            // ����� ���������� �� ������� � �������� (����� Dot1/Dot2/Dot3 ��� ���������)
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
            img.raycastTarget = false;                  // �� ��������� �����
            img.type = Image.Type.Filled;               // ����� ������
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)origin;               // ������ �������� ������
            img.fillClockwise = true;                   // ��� ����
            img.fillAmount = 0f;                        // �� ��������� �����
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    private void LateUpdate()
    {
        if (shooter == null || cooldownOverlays == null || cooldownOverlays.Count == 0) return;

        // ��� ��� �������� ������ ���� � PlayerFireballShooter (��. ���� ����)
        float t = shooter.CooldownNormalized;              // 1..0
        bool active = shooter.IsOnCooldown && t > 0.0001f; // ���� �������?

        foreach (var img in cooldownOverlays)
        {
            if (img == null) continue;

            if (!active)
            {
                img.enabled = false;
                continue;
            }

            img.enabled = true;
            // ������ "���������": 1 -> 0
            img.fillAmount = t;

            // ����� ������ ������� (�� �������)
            var c = img.color;
            c.a = Mathf.Lerp(endAlpha, startAlpha, t);
            img.color = c;
        }
    }
}
