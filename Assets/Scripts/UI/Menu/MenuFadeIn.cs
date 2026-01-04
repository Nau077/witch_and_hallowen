using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MenuFadeIn : MonoBehaviour
{
    [Tooltip("Ñêîëüêî ñåêóíä äëèòñÿ ïîÿâëåíèå èç òåìíîòû")]
    public float duration = 5f;

    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 1f;            // было 4f (это вообще лишнее)
        cg.blocksRaycasts = false; // ✅ НЕ блокируем клики
        cg.interactable = false;
    }

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;            // íåçàâèñèìî îò timeScale
            cg.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        cg.alpha = 0f;
        cg.blocksRaycasts = false;  // áîëüøå íå ïåðåêðûâàåò êëèêè
        cg.interactable = false;
        // íåîáÿçàòåëüíî: ìîæíî ïðîñòî ñêðûòü îáúåêò
        // gameObject.SetActive(false);
    }
}
