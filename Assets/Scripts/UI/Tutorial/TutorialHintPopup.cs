using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHintPopup : MonoBehaviour
{
    public bool IsOpen => _isOpen && popupRoot != null && popupRoot.activeInHierarchy;

    [Serializable]
    public class HintLineUI
    {
        public GameObject root;
        public TMP_Text text;
        public Image icon;
    }

    [Header("Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private string nextLabelEn = "Next";
    [SerializeField] private string nextLabelRu = "Далее";

    [Header("Rows")]
    [SerializeField] private HintLineUI[] linesUi = Array.Empty<HintLineUI>();

    [Header("Behavior")]
    [SerializeField] private bool closeOnSpace = true;
    [SerializeField] private bool lockInputWhileOpen = true;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string showTrigger = "Show";
    [SerializeField] private string hideTrigger = "Hide";
    [SerializeField] private PopupFadeCanvas popupFade;

    private bool _isOpen;
    private Action _onClosed;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        EnsureRuntimeLineBindings();

        if (popupFade == null)
            popupFade = GetComponent<PopupFadeCanvas>();

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnClickNext);
            nextButton.onClick.AddListener(OnClickNext);
        }

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnClickNext);
    }

    private void Update()
    {
        if (!_isOpen || !closeOnSpace)
            return;

        if (Input.GetKeyDown(KeyCode.Space) && nextButton != null && nextButton.interactable)
            OnClickNext();
    }

    public void Show(TutorialHintDefinition hint, Action onClosed = null)
    {
        if (hint == null)
        {
            Debug.LogWarning("[TutorialHintPopup] Hint definition is null.");
            return;
        }

        _onClosed = onClosed;
        ApplyHint(hint);
        SetBlocking(true);
        ShowRoot();
        _isOpen = true;
    }

    public void HideImmediate()
    {
        _isOpen = false;
        _onClosed = null;
        SetBlocking(false);

        if (popupFade != null)
        {
            popupFade.HideImmediate();
        }
        else if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void OnClickNext()
    {
        HideWithAnimation();
    }

    private void HideWithAnimation()
    {
        _isOpen = false;
        SetBlocking(false);

        if (popupFade != null)
        {
            popupFade.HideSmooth();
        }
        else
        {
            if (animator != null && !string.IsNullOrEmpty(hideTrigger))
                animator.SetTrigger(hideTrigger);

            if (popupRoot != null)
                popupRoot.SetActive(false);
        }

        _onClosed?.Invoke();
        _onClosed = null;
    }

    private void ShowRoot()
    {
        if (popupRoot == null)
            return;

        if (popupFade != null)
        {
            popupFade.ShowSmooth();
        }
        else
        {
            popupRoot.SetActive(true);
            if (animator != null && !string.IsNullOrEmpty(showTrigger))
                animator.SetTrigger(showTrigger);
        }
    }

    private void ApplyHint(TutorialHintDefinition hint)
    {
        EnsureRuntimeLineBindings();

        if (titleText != null)
            titleText.text = hint.ResolveTitle();

        if (nextButtonText != null)
        {
            bool isRu = Application.systemLanguage == SystemLanguage.Russian;
            nextButtonText.text = isRu ? nextLabelRu : nextLabelEn;
        }

        var lines = hint.lines ?? Array.Empty<TutorialHintDefinition.HintLine>();

        for (int i = 0; i < linesUi.Length; i++)
        {
            var ui = linesUi[i];
            if (ui == null)
                continue;

            bool hasLine = i < lines.Length && lines[i] != null;
            if (ui.root != null)
                ui.root.SetActive(hasLine);

            if (!hasLine)
                continue;

            var data = lines[i];

            if (ui.text != null)
                ui.text.text = data.ResolveText();

            if (ui.icon != null)
            {
                ui.icon.sprite = data.icon;
                ui.icon.enabled = data.icon != null;
                ui.icon.preserveAspect = true;
            }
        }

        if (linesUi == null || linesUi.Length == 0)
            Debug.LogWarning("[TutorialHintPopup] Lines Ui is empty. Assign rows in inspector or ensure row objects contain TMP_Text + Image.");
    }

    private void EnsureRuntimeLineBindings()
    {
        if (linesUi != null && linesUi.Length > 0)
            return;

        if (popupRoot == null)
            return;

        var rows = new List<HintLineUI>(4);
        var allTexts = popupRoot.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text rowText = allTexts[i];
            if (rowText == null) continue;
            if (rowText == titleText) continue;
            if (rowText == nextButtonText) continue;

            Transform rowRoot = rowText.transform.parent;
            if (rowRoot == null) continue;

            Image rowIcon = null;
            var images = rowRoot.GetComponentsInChildren<Image>(true);
            for (int j = 0; j < images.Length; j++)
            {
                var img = images[j];
                if (img == null) continue;
                if (nextButton != null && img.gameObject == nextButton.gameObject) continue;
                rowIcon = img;
                break;
            }

            if (rowIcon == null) continue;

            bool alreadyAdded = false;
            for (int k = 0; k < rows.Count; k++)
            {
                if (rows[k].root == rowRoot.gameObject)
                {
                    alreadyAdded = true;
                    break;
                }
            }
            if (alreadyAdded) continue;

            rows.Add(new HintLineUI
            {
                root = rowRoot.gameObject,
                text = rowText,
                icon = rowIcon
            });
        }

        if (rows.Count > 0)
            linesUi = rows.ToArray();
    }

    private void SetBlocking(bool blocking)
    {
        if (!lockInputWhileOpen)
            return;

        RunLevelManager.Instance?.SetInputLocked(blocking);
        CursorManager.Instance?.SetPopupBlocking(blocking);
    }
}
