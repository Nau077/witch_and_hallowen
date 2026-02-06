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
        public GameObject root;             // Dialog_group_1 / Dialog_group_2
        public Image portraitImage;         // PortraitImage / PortraitImageReverse
        public TMP_Text bodyText;           // BodyText

        [NonSerialized] public CanvasGroup cg;   // добавим/кэшируем
    }

    [Header("Root (WRAPPER)")]
    [Tooltip("ВРАППЕР = объект DialogueUI. Его будем анимировать и выключать.")]
    public GameObject dialogRoot;

    [Tooltip("CanvasGroup на dialogRoot. Добавь CanvasGroup на DialogueUI (root).")]
    public CanvasGroup rootCanvasGroup;

    [Header("Optional background panel (NOT a wrapper)")]
    [Tooltip("Твой backgroundDialog (картинка). Это НЕ враппер, просто визуальный фон.")]
    public GameObject backgroundPanel;

    [Header("Groups")]
    public DialogueGroup group1;
    public DialogueGroup group2;

    [Header("Continue Button")]
    public Button continueButton;
    public TMP_Text continueButtonText;

    [Tooltip("CanvasGroup на кнопке (для пульса). Если пусто — добавим на кнопку автоматически.")]
    public CanvasGroup continueCanvasGroup;

    [Tooltip("Подсветка по краю кнопки (Outline). Если пусто — попробуем добавить.")]
    public Outline continueOutline;

    [Tooltip("Image кнопки (чтобы слегка тинтить во время пульса). Если пусто — найдём автоматически.")]
    public Image continueImage;

    [Header("Root Animation")]
    public float showDuration = 0.22f;
    public float hideDuration = 0.14f;
    public float startScale = 0.985f;

    [Header("Group (bubble) Animation")]
    [Tooltip("Длительность появления конкретной плашки (левая/правая).")]
    public float bubbleShowDuration = 0.18f;

    [Tooltip("Длительность исчезновения плашки в конце диалога.")]
    public float bubbleHideDuration = 0.14f;

    [Tooltip("Scale при появлении плашки.")]
    public float bubbleStartScale = 0.96f;

    [Header("Line Timing")]
    public float textDelayAfterPanel = 0.10f;

    [Tooltip("0 = без печати (просто появится).")]
    public float typewriterCharsPerSecond = 0f;

    public float textFadeInDuration = 0.10f;

    [Tooltip("Пока печатается/появляется текст — Continue не кликабелен.")]
    public bool blockContinueWhileTyping = true;

    [Header("Continue Glow (pulse + blue edge)")]
    public bool glowEnabled = true;
    public float glowStartDelayAfterText = 1.0f;
    public float glowPulsePeriod = 0.9f;

    [Range(0f, 1f)] public float glowMinAlpha = 0.35f;
    [Range(0f, 1f)] public float glowMaxAlpha = 1.0f;

    [Tooltip("Цвет подсветки края (Outline) при пике пульса.")]
    public Color glowOutlineColor = new Color(0.35f, 0.85f, 1f, 1f);

    [Tooltip("Насколько сильно кнопка тинтится в голубой во время пульса (0 = не тинтим).")]
    [Range(0f, 1f)] public float glowButtonTintStrength = 0.18f;

    public event Action OnContinuePressed;

    Coroutine _rootRoutine;
    Coroutine _lineRoutine;
    Coroutine _glowRoutine;
    Coroutine _bubbleAnimRoutine1;
    Coroutine _bubbleAnimRoutine2;

    private void Awake()
    {
        if (dialogRoot == null)
            dialogRoot = gameObject;

        if (rootCanvasGroup == null && dialogRoot != null)
            rootCanvasGroup = dialogRoot.GetComponent<CanvasGroup>();

        EnsureGroupCanvasGroup(ref group1);
        EnsureGroupCanvasGroup(ref group2);

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => OnContinuePressed?.Invoke());

            if (continueCanvasGroup == null)
                continueCanvasGroup = continueButton.GetComponent<CanvasGroup>();
            if (continueCanvasGroup == null)
                continueCanvasGroup = continueButton.gameObject.AddComponent<CanvasGroup>();

            if (continueImage == null)
                continueImage = continueButton.GetComponent<Image>();

            if (continueOutline == null)
                continueOutline = continueButton.GetComponent<Outline>();
            if (continueOutline == null)
                continueOutline = continueButton.gameObject.AddComponent<Outline>();

            // аккуратная "светящаяся" обводка (можешь поменять в инспекторе)
            continueOutline.effectDistance = new Vector2(2f, -2f);
        }

        HideImmediate();
    }

    private void Update()
    {
        // Активируем кнопку "продолжить" по пробелу, если она интерактивна
        if (Input.GetKeyDown(KeyCode.Space) && continueButton != null && continueButton.interactable)
        {
            OnContinuePressed?.Invoke();
        }
    }

    void EnsureGroupCanvasGroup(ref DialogueGroup g)
    {
        if (g == null || g.root == null) return;

        g.cg = g.root.GetComponent<CanvasGroup>();
        if (g.cg == null)
            g.cg = g.root.AddComponent<CanvasGroup>();
    }

    // ---------------- PUBLIC ----------------

    public void SetContinueLabel(string text)
    {
        if (continueButtonText != null)
            continueButtonText.text = text; // шрифт/размер не трогаем
    }

    public void BeginConversation()
    {
        StopLine();
        StopGlow();

        SetRootActive(true);

        if (backgroundPanel != null)
            backgroundPanel.SetActive(true);

        // Группы сразу выключаем
        SetGroupActive(group1, false, immediate: true);
        SetGroupActive(group2, false, immediate: true);

        SetContinueVisible(false);
        SetContinueInteractable(false);
        SetContinueAlpha(1f);
        SetContinueGlowVisual(0f); // убираем подсветку
    }

    public void PlayShow()
    {
        if (_rootRoutine != null) StopCoroutine(_rootRoutine);
        _rootRoutine = StartCoroutine(ShowRootRoutine());
    }

    public void PlayHide(Action onDone = null)
    {
        StopLine();
        StopGlow();

        if (_rootRoutine != null) StopCoroutine(_rootRoutine);
        _rootRoutine = StartCoroutine(HideRootRoutine(onDone));
    }

    public void HideImmediate()
    {
        StopLine();
        StopGlow();

        StopBubbleRoutines();

        SetGroupActive(group1, false, immediate: true);
        SetGroupActive(group2, false, immediate: true);

        SetContinueVisible(false);
        SetContinueInteractable(false);
        SetContinueAlpha(1f);
        SetContinueGlowVisual(0f);

        if (backgroundPanel != null)
            backgroundPanel.SetActive(false);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (dialogRoot != null)
            dialogRoot.transform.localScale = Vector3.one * startScale;

        SetRootActive(false);
    }

    /// <summary>
    /// Показ реплики: включаем нужную сторону (плавно), другую НЕ выключаем.
    /// Текст появляется после панели, потом включаем continue, потом glow.
    /// </summary>
    public void DisplayLineAnimated(DialogueSequenceSO.Line line)
    {
        if (line == null) return;

        StopLine();
        StopGlow();

        _lineRoutine = StartCoroutine(DisplayLineRoutine(line));
    }

    // ---------------- ROUTINES ----------------

    IEnumerator ShowRootRoutine()
    {
        if (dialogRoot == null) yield break;

        SetRootActive(true);

        if (backgroundPanel != null)
            backgroundPanel.SetActive(true);

        if (rootCanvasGroup == null)
            yield break;

        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        float t = 0f;
        Vector3 from = Vector3.one * startScale;
        Vector3 to = Vector3.one;
        dialogRoot.transform.localScale = from;

        while (t < showDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, showDuration));
            rootCanvasGroup.alpha = k;
            dialogRoot.transform.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }

        rootCanvasGroup.alpha = 1f;
        dialogRoot.transform.localScale = Vector3.one;
    }

    IEnumerator HideRootRoutine(Action onDone)
    {
        // ✅ сначала плавно прячем обе плашки + кнопку (чтобы не "висело" в конце)
        SetContinueVisible(false);
        SetContinueInteractable(false);
        SetContinueGlowVisual(0f);

        yield return HideGroupsRoutine();

        if (rootCanvasGroup == null || dialogRoot == null)
        {
            if (backgroundPanel != null) backgroundPanel.SetActive(false);
            SetRootActive(false);
            onDone?.Invoke();
            yield break;
        }

        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        float t = 0f;
        float startA = rootCanvasGroup.alpha;
        Vector3 from = dialogRoot.transform.localScale;
        Vector3 to = Vector3.one * startScale;

        while (t < hideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, hideDuration));
            rootCanvasGroup.alpha = Mathf.Lerp(startA, 0f, k);
            dialogRoot.transform.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }

        rootCanvasGroup.alpha = 0f;

        if (backgroundPanel != null)
            backgroundPanel.SetActive(false);

        SetRootActive(false);
        onDone?.Invoke();
    }

    IEnumerator HideGroupsRoutine()
    {
        // плавно прячем активные группы, если они есть
        bool g1 = group1.root != null && group1.root.activeSelf;
        bool g2 = group2.root != null && group2.root.activeSelf;

        if (!g1 && !g2)
            yield break;

        float dur = Mathf.Max(0.0001f, bubbleHideDuration);
        float t = 0f;

        CanvasGroup cg1 = group1.cg;
        CanvasGroup cg2 = group2.cg;

        Vector3 s1 = (group1.root != null) ? group1.root.transform.localScale : Vector3.one;
        Vector3 s2 = (group2.root != null) ? group2.root.transform.localScale : Vector3.one;

        float a1 = (cg1 != null) ? cg1.alpha : 1f;
        float a2 = (cg2 != null) ? cg2.alpha : 1f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);

            float a = 1f - k;

            if (g1 && cg1 != null)
            {
                cg1.alpha = Mathf.Lerp(a1, 0f, k);
                group1.root.transform.localScale = Vector3.Lerp(s1, Vector3.one * bubbleStartScale, k);
            }
            if (g2 && cg2 != null)
            {
                cg2.alpha = Mathf.Lerp(a2, 0f, k);
                group2.root.transform.localScale = Vector3.Lerp(s2, Vector3.one * bubbleStartScale, k);
            }

            yield return null;
        }

        SetGroupActive(group1, false, immediate: true);
        SetGroupActive(group2, false, immediate: true);
    }

    IEnumerator DisplayLineRoutine(DialogueSequenceSO.Line line)
    {
        bool left = line.side == DialogueSpeakerSide.Left;
        DialogueGroup g = left ? group1 : group2;

        if (g == null || g.root == null || g.bodyText == null)
            yield break;

        EnsureGroupCanvasGroup(ref g);

        // ✅ нужную сторону включаем (плавно), вторую НЕ выключаем
        if (!g.root.activeSelf)
        {
            SetGroupActive(g, true, immediate: true); // включить объект
            StartBubbleShow(g);                       // и анимировать появление
        }

        // портрет
        if (g.portraitImage != null && line.portraitOverride != null)
            g.portraitImage.sprite = line.portraitOverride;

        // сброс текста/альфы (не трогаем шрифт/размер)
        g.bodyText.maxVisibleCharacters = int.MaxValue;
        g.bodyText.text = "";
        SetTMPAlpha(g.bodyText, 0f);

        if (blockContinueWhileTyping)
        {
            SetContinueVisible(false);
            SetContinueInteractable(false);
            SetContinueGlowVisual(0f);
        }

        // задержка после появления панели
        if (textDelayAfterPanel > 0f)
        {
            float td = 0f;
            while (td < textDelayAfterPanel)
            {
                td += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // fade текста
        if (textFadeInDuration > 0f)
        {
            float t = 0f;
            while (t < textFadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, textFadeInDuration));
                SetTMPAlpha(g.bodyText, k);
                yield return null;
            }
        }
        SetTMPAlpha(g.bodyText, 1f);

        string full = line.text ?? "";

        // typewriter опционально
        if (typewriterCharsPerSecond <= 0f)
        {
            g.bodyText.text = full;
        }
        else
        {
            g.bodyText.text = full;
            g.bodyText.ForceMeshUpdate();

            int total = g.bodyText.textInfo.characterCount;
            g.bodyText.maxVisibleCharacters = 0;

            float cps = Mathf.Max(1f, typewriterCharsPerSecond);
            float visible = 0f;

            while (g.bodyText.maxVisibleCharacters < total)
            {
                visible += Time.unscaledDeltaTime * cps;
                g.bodyText.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(visible), 0, total);
                yield return null;
            }

            g.bodyText.maxVisibleCharacters = total;
        }

        // ✅ теперь включаем continue
        SetContinueVisible(true);
        SetContinueInteractable(true);

        // ✅ glow стартует позже
        if (glowEnabled)
        {
            if (glowStartDelayAfterText > 0f)
            {
                float t = 0f;
                while (t < glowStartDelayAfterText)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            StartGlow();
        }
        else
        {
            SetContinueAlpha(1f);
            SetContinueGlowVisual(0f);
        }
    }

    // ---------------- GROUP ANIM ----------------

    void StartBubbleShow(DialogueGroup g)
    {
        if (g == null || g.root == null || g.cg == null) return;

        // стопаем конкретную корутину группы
        if (g == group1 && _bubbleAnimRoutine1 != null) StopCoroutine(_bubbleAnimRoutine1);
        if (g == group2 && _bubbleAnimRoutine2 != null) StopCoroutine(_bubbleAnimRoutine2);

        var routine = StartCoroutine(BubbleShowRoutine(g));

        if (g == group1) _bubbleAnimRoutine1 = routine;
        if (g == group2) _bubbleAnimRoutine2 = routine;
    }

    IEnumerator BubbleShowRoutine(DialogueGroup g)
    {
        g.cg.alpha = 0f;
        g.root.transform.localScale = Vector3.one * bubbleStartScale;

        float dur = Mathf.Max(0.0001f, bubbleShowDuration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);

            g.cg.alpha = k;
            g.root.transform.localScale = Vector3.Lerp(Vector3.one * bubbleStartScale, Vector3.one, k);

            yield return null;
        }

        g.cg.alpha = 1f;
        g.root.transform.localScale = Vector3.one;
    }

    void StopBubbleRoutines()
    {
        if (_bubbleAnimRoutine1 != null) { StopCoroutine(_bubbleAnimRoutine1); _bubbleAnimRoutine1 = null; }
        if (_bubbleAnimRoutine2 != null) { StopCoroutine(_bubbleAnimRoutine2); _bubbleAnimRoutine2 = null; }
    }

    void SetGroupActive(DialogueGroup g, bool on, bool immediate)
    {
        if (g == null || g.root == null) return;

        if (g.root.activeSelf != on)
            g.root.SetActive(on);

        EnsureGroupCanvasGroup(ref g);

        if (immediate && g.cg != null)
        {
            g.cg.alpha = on ? 1f : 0f;
            g.root.transform.localScale = Vector3.one;
        }
    }

    // ---------------- HELPERS ----------------

    void SetRootActive(bool on)
    {
        if (dialogRoot != null && dialogRoot.activeSelf != on)
            dialogRoot.SetActive(on);
    }

    void SetContinueVisible(bool on)
    {
        if (continueButton == null) return;

        if (continueButton.gameObject.activeSelf != on)
            continueButton.gameObject.SetActive(on);

        if (on)
            SetContinueAlpha(1f);
    }

    void SetContinueInteractable(bool on)
    {
        if (continueButton != null)
            continueButton.interactable = on;

        if (continueCanvasGroup != null)
        {
            continueCanvasGroup.interactable = on;
            continueCanvasGroup.blocksRaycasts = on;
        }
    }

    void SetContinueAlpha(float a)
    {
        a = Mathf.Clamp01(a);

        if (continueCanvasGroup != null)
            continueCanvasGroup.alpha = a;
        else if (continueButtonText != null)
            SetTMPAlpha(continueButtonText, a);
    }

    void SetContinueGlowVisual(float intensity01)
    {
        intensity01 = Mathf.Clamp01(intensity01);

        // Outline: голубая обводка, меняем альфу
        if (continueOutline != null)
        {
            var c = glowOutlineColor;
            c.a *= intensity01;
            continueOutline.effectColor = c;
            continueOutline.enabled = intensity01 > 0.01f;
        }

        // Лёгкий тинт самой кнопки (Image), но не ломаем дизайн
        if (continueImage != null && glowButtonTintStrength > 0f)
        {
            Color baseC = Color.white; // мы не знаем твой исходный цвет; белый обычно нейтрален
            // Если хочешь сохранять исходный цвет — просто выстави в инспекторе continueImage.color = white.
            Color tint = new Color(glowOutlineColor.r, glowOutlineColor.g, glowOutlineColor.b, 1f);
            float s = glowButtonTintStrength * intensity01;
            continueImage.color = Color.Lerp(baseC, tint, s);
        }
    }

    void SetTMPAlpha(TMP_Text txt, float a)
    {
        if (txt == null) return;
        var c = txt.color;
        c.a = Mathf.Clamp01(a);
        txt.color = c;
    }

    void StopLine()
    {
        if (_lineRoutine != null)
        {
            StopCoroutine(_lineRoutine);
            _lineRoutine = null;
        }
    }

    void StartGlow()
    {
        StopGlow();
        _glowRoutine = StartCoroutine(GlowRoutine());
    }

    void StopGlow()
    {
        if (_glowRoutine != null)
        {
            StopCoroutine(_glowRoutine);
            _glowRoutine = null;
        }

        // вернем нормальный вид
        SetContinueAlpha(1f);
        SetContinueGlowVisual(0f);
    }

    IEnumerator GlowRoutine()
    {
        float period = Mathf.Max(0.15f, glowPulsePeriod);

        while (true)
        {
            float t = 0f;
            while (t < period)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / period);

                // 0..1..0
                float wave = 0.5f - 0.5f * Mathf.Cos(k * Mathf.PI * 2f);

                // alpha пульс
                float a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, wave);
                SetContinueAlpha(a);

                // outline/tint пульс (сильнее возле пика)
                SetContinueGlowVisual(wave);

                yield return null;
            }
        }
    }
}
