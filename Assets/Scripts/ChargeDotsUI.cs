// ChargeDotsUI.cs
using UnityEngine;
using UnityEngine.UI;

public class ChargeDotsUI : MonoBehaviour
{
    [Tooltip("Иконки-точки по порядку слева-направо")]
    public Image[] dots;

    public void SetCount(int litCount)
    {
        if (dots == null) return;
        for (int i = 0; i < dots.Length; i++)
        {
            var img = dots[i];
            if (img) img.enabled = (i < litCount);
        }
    }

    public int Capacity => dots != null ? dots.Length : 0;

    public void Clear() => SetCount(0);
}