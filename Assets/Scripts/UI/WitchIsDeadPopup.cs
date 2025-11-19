using UnityEngine;
using TMPro;

public class WitchIsDeadPopup : MonoBehaviour
{
    public static WitchIsDeadPopup Instance;

    public CanvasGroup group;
    public TextMeshProUGUI text;

    private void Awake()
    {
        Instance = this;
        if (!group) group = GetComponent<CanvasGroup>();
        group.alpha = 0;
        gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        gameObject.SetActive(true);
        text.text = message;
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        group.alpha = 0;
        while (group.alpha < 1f)
        {
            group.alpha += Time.deltaTime * 1f;
            yield return null;
        }
        group.alpha = 1f;
    }

    public void HideImmediate()
    {
        group.alpha = 0;
        gameObject.SetActive(false);
    }
}
