using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Serializable]
    public class DialogueGroup
    {
        public GameObject root;             // Dialog_group_1 или Dialog_group_2
        public Image portraitImage;         // PortraitImage / PortraitImageReverse
        public TMP_Text bodyText;           // BodyText
    }

    [Header("Wrapper")]
    [Tooltip("Твой backgroundDialog (большая панель-враппер).")]
    public GameObject backgroundDialog;

    [Tooltip("CanvasGroup на backgroundDialog (для fade/scale). Если пусто — попробуем найти.")]
    public CanvasGroup canvasGroup;

    [Header("Groups")]
    public DialogueGroup group1; // Dialog_group_1
    public DialogueGroup group2; // Dialog_group_2

    [Header("Continue Button")]
    public Button continueButton;
    public TMP_Text continueButtonText;

    [Header("Animation")]
    public float showDuration = 0.18f;
    public float hideDuration = 0.12f;
    public float startScale = 0.98f;

    public event Action OnContinuePressed;

    private Coroutine _animRoutine;

    private void Awake()
    {
        if (backgroundDialog != null && canvasGroup == null)
            canvasGroup = backgroundDialog.GetComponent<CanvasGroup>();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => OnContinuePressed?.Invoke());
        }

        HideImmediate();
    }

    public void SetContinueLabel(string text)
    {
        if (continueButtonText != null)
            continueButtonText.text = text;
    }

    public void ShowImmediate()
    {
        if (backgroundDialog == null) return;

        backgroundDialog.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            backgroundDialog.transform.localScale = Vector3.one;
        }
    }

    public void HideImmediate()
    {
        if (backgroundDialog == null) return;

        // выключаем всё безопасно
        if (group1.root) group1.root.SetActive(false);
        if (group2.root) group2.root.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        backgroundDialog.SetActive(false);
    }

    public void PlayShow()
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(ShowRoutine());
    }

    public void PlayHide(Action onDone = null)
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(HideRoutine(onDone));
    }

    private IEnumerator ShowRoutine()
    {
        if (backgroundDialog == null) yield break;

        backgroundDialog.SetActive(true);

        if (canvasGroup == null)
        {
            // без CanvasGroup просто покажем
            yield break;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        float t = 0f;
        Vector3 from = Vector3.one * startScale;
        Vector3 to = Vector3.one;
        backgroundDialog.transform.localScale = from;

        while (t < showDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, showDuration));
            canvasGroup.alpha = k;
            backgroundDialog.transform.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        backgroundDialog.transform.localScale = Vector3.one;
    }

    private IEnumerator HideRoutine(Action onDone)
    {
        if (backgroundDialog == null) yield break;

        if (canvasGroup == null)
        {
            backgroundDialog.SetActive(false);
            onDone?.Invoke();
            yield break;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float t = 0f;
        float startAlpha = canvasGroup.alpha;
        Vector3 from = backgroundDialog.transform.localScale;
        Vector3 to = Vector3.one * startScale;

        while (t < hideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, hideDuration));
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, k);
            backgroundDialog.transform.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // выключаем группы
        if (group1.root) group1.root.SetActive(false);
        if (group2.root) group2.root.SetActive(false);

        backgroundDialog.SetActive(false);
        onDone?.Invoke();
    }

    /// <summary>
    /// Показать реплику: активируем только нужную группу (1 или 2),
    /// заполняем текст/портрет.
    /// </summary>
    public void DisplayLine(DialogueSequenceSO.Line line)
    {
        if (line == null) return;

        bool left = line.side == DialogueSpeakerSide.Left;

        if (group1.root) group1.root.SetActive(left);
        if (group2.root) group2.root.SetActive(!left);

        DialogueGroup g = left ? group1 : group2;

        if (g.bodyText != null)
            g.bodyText.text = line.text ?? "";

        if (g.portraitImage != null && line.portraitOverride != null)
            g.portraitImage.sprite = line.portraitOverride;
    }
}
