using UnityEngine;
using UnityEngine.UI;

public class ClickProgressBar : MonoBehaviour
{
    public Image fill;

    public void SetProgress(float v01)
    {
        if (fill) fill.fillAmount = Mathf.Clamp01(v01);
    }
}
