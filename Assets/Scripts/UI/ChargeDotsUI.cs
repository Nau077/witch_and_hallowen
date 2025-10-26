using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChargeDotsUI : MonoBehaviour
{
    [Header("Settings")]
    public Color inactiveColor = new Color(0.53f, 0.53f, 0.53f, 1f); // серый
    public Color activeColor = Color.white;                           // яркий
    public float fillFadeSpeed = 8f;                                  // (на будущее)

    [Header("Refs")]
    public List<Image> dots = new List<Image>();                      // Dot1/Dot2/...

    private int currentCount = 0;

    private void Awake()
    {
        RebuildList();
        Clear();
    }

    private void OnEnable()
    {
        RebuildList();
        ApplyColors();
    }

    // вызывается Unity, когда структура детей меняется (добавили/удалили Dot)
    private void OnTransformChildrenChanged()
    {
        RebuildList();
        ApplyColors();
    }

    public void Clear()
    {
        currentCount = 0;
        ApplyColors();
    }

    public void SetCount(int count)
    {
        currentCount = Mathf.Clamp(count, 0, dots.Count);
        ApplyColors();
    }

    private void ApplyColors()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if (!dots[i]) continue;
            dots[i].color = (i < currentCount ? activeColor : inactiveColor);
        }
    }

    // Собираем всех детей-Image в порядке их расположения в иерархии
    public void RebuildList()
    {
        dots.Clear();
        // берём только прямых детей, чтобы случайные картинки глубже не попали
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var img = child.GetComponent<Image>();
            if (img) dots.Add(img);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Now")]
    private void EditorRebuild()
    {
        RebuildList();
        ApplyColors();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private void OnValidate()
    {
        // В редакторе тоже держим список в актуальном состоянии
        RebuildList();
        ApplyColors();
    }
#endif
}
