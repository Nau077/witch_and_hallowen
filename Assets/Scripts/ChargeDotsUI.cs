using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChargeDotsUI : MonoBehaviour
{
    [Header("Settings")]
    public Color inactiveColor = new Color(0.53f, 0.53f, 0.53f, 1f); // серый
    public Color activeColor = Color.white;                           // €ркий
    public float fillFadeSpeed = 8f;                                  // (на будущее)

    [Header("Refs")]
    public List<Image> dots = new List<Image>();                      // Dot1/Dot2/Dot3

    private int currentCount = 0;

    private void Awake()
    {
        if (dots.Count == 0)
            dots.AddRange(GetComponentsInChildren<Image>(true));
        Clear();
    }

    public void Clear()
    {
        currentCount = 0;
        foreach (var img in dots)
            if (img) img.color = inactiveColor;
    }

    public void SetCount(int count)
    {
        currentCount = Mathf.Clamp(count, 0, dots.Count);
        for (int i = 0; i < dots.Count; i++)
            if (dots[i]) dots[i].color = (i < currentCount ? activeColor : inactiveColor);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (dots.Count == 0)
            dots.AddRange(GetComponentsInChildren<Image>(true));
    }
#endif
}
