using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI refs")]
    public Image fillImage; // Image type = Filled, Fill Method = Horizontal
    public CanvasGroup canvasGroup; // дл€ скрыти€ при смерти (опционально)

    [Header("Position")]
    public Vector3 offset = new Vector3(0f, 1.2f, 0f); // над врагом

    int max = 100;
    int current = 100;

    Transform target;

    void Awake()
    {
        if (fillImage == null)
            Debug.LogWarning("HealthBar: fillImage не задан.");
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void AttachTo(Transform t)
    {
        target = t;
    }

    void LateUpdate()
    {
        if (target)
        {
            transform.position = target.position + offset;
            // при необходимости Ч поворачивать к камере:
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void SetMax(int maxHealth)
    {
        max = Mathf.Max(1, maxHealth);
        SetCurrent(maxHealth);
    }

    public void SetCurrent(int value)
    {
        current = Mathf.Clamp(value, 0, max);
        if (fillImage)
            fillImage.fillAmount = (float)current / max;
    }

    public void Hide()
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        else gameObject.SetActive(false);
    }
}
