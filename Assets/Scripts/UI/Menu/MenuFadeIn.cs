using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class MenuFadeIn : MonoBehaviour
{
    [Tooltip("������� ������ ������ ��������� �� �������")]
    public float duration = 5f;

    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        cg.alpha = 4f;           // �������� � ������ �������
        cg.blocksRaycasts = true; // ��������� �����, ���� �����
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
            t += Time.unscaledDeltaTime;            // ���������� �� timeScale
            cg.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        cg.alpha = 0f;
        cg.blocksRaycasts = false;  // ������ �� ����������� �����
        cg.interactable = false;
        // �������������: ����� ������ ������ ������
        // gameObject.SetActive(false);
    }
}
