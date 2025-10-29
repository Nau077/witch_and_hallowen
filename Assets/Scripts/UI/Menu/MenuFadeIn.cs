using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MenuFadeIn : MonoBehaviour
{
    [Tooltip("Сколько секунд длится появление из темноты")]
    public float duration = 5f;

    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 4f;           // стартуем с полной темноты
        cg.blocksRaycasts = true; // блокируем клики, пока темно
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
            t += Time.unscaledDeltaTime;            // независимо от timeScale
            cg.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        cg.alpha = 0f;
        cg.blocksRaycasts = false;  // больше не перекрывает клики
        cg.interactable = false;
        // необязательно: можно просто скрыть объект
        // gameObject.SetActive(false);
    }
}
