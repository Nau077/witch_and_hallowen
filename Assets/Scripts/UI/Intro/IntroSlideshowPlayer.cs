using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IntroSlideshowPlayer : MonoBehaviour
{
    private const string BootModeKey = "dw_boot_mode";
    private const int BootNewGame = 1;
    private const string NewGameIntroPendingKey = "dw_intro_new_game_pending";

    public enum PlayCondition
    {
        Always = 0,
        NewGameBootModeOnly = 1
    }

    public enum FinishAction
    {
        None = 0,
        LoadSceneByName = 1,
        DisableGameObject = 2
    }

    public enum ZoomDirectionMode
    {
        AlwaysOut = 0,
        AlwaysIn = 1,
        Alternate = 2
    }

    [Serializable]
    public class SlideSettings
    {
        public Sprite sprite;

        [Header("Timing Override")]
        public bool overrideTiming = false;
        [Min(0.01f)] public float fadeInDuration = 0.6f;
        [Min(0f)] public float holdDuration = 1.5f;
        [Min(0.01f)] public float fadeOutDuration = 0.6f;

        [Header("Zoom Override")]
        public bool overrideZoom = false;
        public bool enableZoom = true;
        public ZoomDirectionMode zoomDirectionMode = ZoomDirectionMode.Alternate;
        public bool firstSlideZoomOut = true;
        [Min(0.01f)] public float zoomStart = 1.08f;
        [Min(0.01f)] public float zoomEnd = 1.0f;
    }

    [Header("Condition")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private PlayCondition playCondition = PlayCondition.Always;
    [SerializeField] private bool clearNewGameBootModeAfterPlay = true;
    [SerializeField] private bool disableIfConditionNotMet = true;

    [Header("Slides")]
    [SerializeField] private Image slideImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Sprite[] slides;
    [SerializeField] private SlideSettings[] slideSettings;

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupSlideFullscreen = true;
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private bool forceMainCameraSolidColor = false;
    [SerializeField] private Color forcedCameraColor = Color.black;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float startDelay = 0f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float holdDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.6f;

    [Header("Transition")]
    [SerializeField] private bool fadeThroughColor = true;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private Image fadeOverlayImage;

    [Header("Pan/Zoom")]
    [SerializeField] private bool enableZoomOut = false;
    [SerializeField] private ZoomDirectionMode zoomDirectionMode = ZoomDirectionMode.Alternate;
    [SerializeField] private bool firstSlideZoomOut = true;
    [SerializeField, Min(0.01f)] private float zoomStart = 1.08f;
    [SerializeField, Min(0.01f)] private float zoomEnd = 1.0f;

    [Header("Input")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private bool alwaysAllowSpaceSkip = true;
    [SerializeField] private bool skipOnAnyMouseClick = true;
    [SerializeField] private KeyCode skipKeyPrimary = KeyCode.Space;
    [SerializeField] private KeyCode skipKeySecondary = KeyCode.Escape;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool useAudioSourceClipWhenMusicClipMissing = true;
    [SerializeField] private bool stopMusicOnFinish = true;

    [Header("After Finish")]
    [SerializeField] private FinishAction finishAction = FinishAction.None;
    [SerializeField] private string sceneToLoad = "MainMenu";
    [SerializeField] private bool carryMusicToLoadedScene = false;
    [SerializeField] private bool hideNoDeathRecordInThisScene = true;
    [SerializeField] private GameObject[] objectsToDisableOnFinish;
    [SerializeField] private bool disableObjectsAlsoOnSkip = true;

    private bool _skipRequested;
    private Coroutine _playRoutine;
    private Action _runtimeFinishedCallback;
    private bool _countedAsPlaying;

    private RectTransform _slideRect;
    private float _slideTotalDuration;
    private float _slideElapsed;
    private float _currentSlideZoomStart = 1.08f;
    private float _currentSlideZoomEnd = 1.0f;
    private bool _currentSlideZoomEnabled;

    private static int _activePlaybackCount;
    private static int _activeHideNoDeathPlaybackCount;
    public static bool IsAnyIntroPlaying => _activePlaybackCount > 0;
    public static bool IsNoDeathRecordHideRequested => _activeHideNoDeathPlaybackCount > 0;
    public static event Action<bool> AnyIntroPlaybackChanged;

    public bool IsPlaying => _playRoutine != null;

    private void Awake()
    {
        if (disableIfConditionNotMet && playOnStart && !ShouldPlayNow())
        {
            DisableConditionTargetImmediate();
            return;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (slideImage == null)
            slideImage = GetComponentInChildren<Image>(true);

        if (slideImage != null)
            _slideRect = slideImage.rectTransform;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        EnsureFadeOverlay();
        ApplyAutoSetup();
        ApplyFadeColor();
    }

    private void Start()
    {
        if (!playOnStart)
            return;

        if (!ShouldPlayNow())
        {
            if (disableIfConditionNotMet)
                DisableConditionTargetImmediate();
            return;
        }

        PlayNow();
    }

    public void PlayNow(Action onFinished = null)
    {
        _runtimeFinishedCallback = onFinished;

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        if (!ValidateSetup())
            return;

        MarkPlayingStarted();
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    private bool ShouldPlayNow()
    {
        if (playCondition == PlayCondition.Always)
            return true;

        if (playCondition == PlayCondition.NewGameBootModeOnly)
        {
            if (PlayerPrefs.GetInt(NewGameIntroPendingKey, 0) == 1)
                return true;

            return PlayerPrefs.GetInt(BootModeKey, 0) == BootNewGame;
        }

        return false;
    }

    private bool ValidateSetup()
    {
        if (slideImage == null)
        {
            Debug.LogError("[IntroSlideshowPlayer] Slide Image is not assigned.", this);
            return false;
        }

        if (canvasGroup == null)
        {
            Debug.LogError("[IntroSlideshowPlayer] CanvasGroup is not assigned.", this);
            return false;
        }

        if (GetSlideCount() == 0)
        {
            Debug.LogError("[IntroSlideshowPlayer] Slides are empty.", this);
            return false;
        }

        return true;
    }

    private IEnumerator PlayRoutine()
    {
        _skipRequested = false;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (!fadeThroughColor)
            canvasGroup.alpha = 0f;
        else
            canvasGroup.alpha = 1f;

        if (musicClip != null && audioSource != null)
        {
            audioSource.clip = musicClip;
            audioSource.Play();
        }
        else if (audioSource != null && useAudioSourceClipWhenMusicClipMissing && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        if (startDelay > 0f)
            yield return WaitForSecondsOrSkip(startDelay);

        int count = GetSlideCount();
        for (int i = 0; i < count; i++)
        {
            Sprite sprite = GetSlideSprite(i);
            if (sprite == null)
                continue;

            slideImage.sprite = sprite;

            float fadeIn = GetFadeInForSlide(i);
            float hold = GetHoldForSlide(i);
            float fadeOut = GetFadeOutForSlide(i);

            BeginSlideZoomCycle(i, fadeIn, hold, fadeOut);

            yield return Fade(0f, 1f, fadeIn);
            if (_skipRequested) break;

            if (hold > 0f)
            {
                yield return WaitForSecondsOrSkip(hold);
                if (_skipRequested) break;
            }

            yield return Fade(1f, 0f, fadeOut);
            if (_skipRequested) break;
        }

        OnSequenceFinished();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        ApplyVisibilityValue(from);

        while (t < duration)
        {
            if (CheckSkipInput())
                yield break;

            float dt = Time.unscaledDeltaTime;
            t += dt;
            TickSlideZoom(dt);

            float k = Mathf.Clamp01(t / duration);
            float visibility = Mathf.Lerp(from, to, k);
            ApplyVisibilityValue(visibility);
            yield return null;
        }

        ApplyVisibilityValue(to);
    }

    private IEnumerator WaitForSecondsOrSkip(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (CheckSkipInput())
                yield break;

            float dt = Time.unscaledDeltaTime;
            t += dt;
            TickSlideZoom(dt);
            yield return null;
        }
    }

    private bool CheckSkipInput()
    {
        if (!allowSkip)
            return false;

        if (_skipRequested)
            return true;

        bool pressedPrimaryOrSecondary = Input.GetKeyDown(skipKeyPrimary) || Input.GetKeyDown(skipKeySecondary);
        bool pressedSpaceForced = alwaysAllowSpaceSkip && Input.GetKeyDown(KeyCode.Space);

        if (pressedPrimaryOrSecondary || pressedSpaceForced)
        {
            _skipRequested = true;
            return true;
        }

        if (skipOnAnyMouseClick && Input.GetMouseButtonDown(0))
        {
            _skipRequested = true;
            return true;
        }

        return false;
    }

    private void OnSequenceFinished()
    {
        if (clearNewGameBootModeAfterPlay && playCondition == PlayCondition.NewGameBootModeOnly)
        {
            PlayerPrefs.SetInt(BootModeKey, 0);
            PlayerPrefs.SetInt(NewGameIntroPendingKey, 0);
            PlayerPrefs.Save();
        }

        if (finishAction == FinishAction.LoadSceneByName && carryMusicToLoadedScene && audioSource != null)
            IntroMusicCarryover.CreateFrom(audioSource);

        if (audioSource != null && stopMusicOnFinish)
            audioSource.Stop();

        DisableConfiguredObjects();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (fadeOverlayImage != null)
            SetImageAlpha(fadeOverlayImage, 1f);

        _playRoutine = null;
        MarkPlayingStopped();

        var callback = _runtimeFinishedCallback;
        _runtimeFinishedCallback = null;
        callback?.Invoke();

        if (finishAction == FinishAction.LoadSceneByName)
        {
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                Debug.LogError("[IntroSlideshowPlayer] sceneToLoad is empty.", this);
                return;
            }

            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        if (finishAction == FinishAction.DisableGameObject)
            gameObject.SetActive(false);
    }

    private void ApplyFinishActionWhenSkipped()
    {
        if (finishAction == FinishAction.LoadSceneByName)
        {
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                Debug.LogError("[IntroSlideshowPlayer] sceneToLoad is empty.", this);
                return;
            }

            if (carryMusicToLoadedScene && audioSource != null)
                IntroMusicCarryover.CreateFrom(audioSource);

            if (disableObjectsAlsoOnSkip)
                DisableConfiguredObjects();

            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        if (disableObjectsAlsoOnSkip)
            DisableConfiguredObjects();

        if (finishAction == FinishAction.DisableGameObject)
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        MarkPlayingStopped();
    }

    private void EnsureFadeOverlay()
    {
        if (!fadeThroughColor)
            return;

        if (slideImage == null)
            return;

        if (fadeOverlayImage == null)
        {
            var go = new GameObject("FadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(slideImage.transform.parent, false);
            fadeOverlayImage = go.GetComponent<Image>();
            fadeOverlayImage.raycastTarget = false;

            RectTransform rect = fadeOverlayImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        fadeOverlayImage.transform.SetAsLastSibling();
    }

    private void ApplyAutoSetup()
    {
        if (slideImage != null)
            slideImage.preserveAspect = preserveAspect;

        if (autoSetupSlideFullscreen && _slideRect != null)
        {
            _slideRect.anchorMin = Vector2.zero;
            _slideRect.anchorMax = Vector2.one;
            _slideRect.offsetMin = Vector2.zero;
            _slideRect.offsetMax = Vector2.zero;
            _slideRect.pivot = new Vector2(0.5f, 0.5f);
            _slideRect.localScale = Vector3.one;
            _slideRect.localRotation = Quaternion.identity;
        }

        if (forceMainCameraSolidColor)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = forcedCameraColor;
            }
        }
    }

    private void ApplyVisibilityValue(float visibility)
    {
        float v = Mathf.Clamp01(visibility);

        if (fadeThroughColor && fadeOverlayImage != null)
        {
            canvasGroup.alpha = 1f;
            SetImageAlpha(fadeOverlayImage, 1f - v);
        }
        else
        {
            canvasGroup.alpha = v;
        }
    }

    private void ApplyFadeColor()
    {
        if (fadeOverlayImage == null)
            return;

        var c = fadeColor;
        c.a = 1f;
        fadeOverlayImage.color = c;
    }

    private static void SetImageAlpha(Image img, float a)
    {
        if (img == null)
            return;

        var c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
    }

    private void BeginSlideZoomCycle(int slideIndex, float fadeInForSlide, float holdForSlide, float fadeOutForSlide)
    {
        if (_slideRect == null)
            return;

        bool zoomEnabled = enableZoomOut;
        ZoomDirectionMode mode = zoomDirectionMode;
        bool firstOut = firstSlideZoomOut;
        float start = zoomStart;
        float end = zoomEnd;

        SlideSettings s = GetSlideSettingsAt(slideIndex);
        if (s != null && s.overrideZoom)
        {
            zoomEnabled = s.enableZoom;
            mode = s.zoomDirectionMode;
            firstOut = s.firstSlideZoomOut;
            start = s.zoomStart;
            end = s.zoomEnd;
        }

        if (!zoomEnabled)
        {
            _currentSlideZoomEnabled = false;
            _slideRect.localScale = Vector3.one;
            return;
        }

        _currentSlideZoomEnabled = true;
        float safeZoomStart = GetSafePositive(start, 1.08f);
        float safeZoomEnd = GetSafePositive(end, 1.0f);

        if (mode == ZoomDirectionMode.Alternate && GetSlideCount() > 1)
        {
            bool useZoomOut = firstOut ? (slideIndex % 2 == 0) : (slideIndex % 2 != 0);
            _currentSlideZoomStart = useZoomOut ? safeZoomStart : safeZoomEnd;
            _currentSlideZoomEnd = useZoomOut ? safeZoomEnd : safeZoomStart;
        }
        else if (mode == ZoomDirectionMode.AlwaysIn)
        {
            _currentSlideZoomStart = safeZoomEnd;
            _currentSlideZoomEnd = safeZoomStart;
        }
        else
        {
            _currentSlideZoomStart = safeZoomStart;
            _currentSlideZoomEnd = safeZoomEnd;
        }

        _slideTotalDuration = Mathf.Max(0.01f, GetSafeNonNegative(fadeInForSlide, 0.6f) + GetSafeNonNegative(holdForSlide, 1.5f) + GetSafeNonNegative(fadeOutForSlide, 0.6f));
        _slideElapsed = 0f;
        _slideRect.localScale = Vector3.one * _currentSlideZoomStart;
    }

    private void TickSlideZoom(float dt)
    {
        if (_slideRect == null || !_currentSlideZoomEnabled)
            return;

        if (!float.IsFinite(dt) || dt < 0f)
            dt = 0f;

        Vector3 currentScale = _slideRect.localScale;
        if (!IsFiniteVector3(currentScale))
            _slideRect.localScale = Vector3.one;

        _slideElapsed = Mathf.Min(_slideElapsed + dt, _slideTotalDuration);
        if (!float.IsFinite(_slideElapsed))
            _slideElapsed = 0f;

        if (!float.IsFinite(_slideTotalDuration) || _slideTotalDuration <= 0f)
            _slideTotalDuration = 0.01f;

        float k = Mathf.Clamp01(_slideElapsed / _slideTotalDuration);
        float start = GetSafePositive(_currentSlideZoomStart, 1.08f);
        float end = GetSafePositive(_currentSlideZoomEnd, 1.0f);
        float s = Mathf.Lerp(start, end, k);
        if (!float.IsFinite(s) || s <= 0f)
            s = 1f;

        _slideRect.localScale = Vector3.one * s;
    }

    private static float GetSafePositive(float value, float fallback)
    {
        if (!float.IsFinite(value) || value <= 0f)
            return fallback;
        return value;
    }

    private static float GetSafeNonNegative(float value, float fallback)
    {
        if (!float.IsFinite(value) || value < 0f)
            return fallback;
        return value;
    }

    private static bool IsFiniteVector3(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    private void DisableConfiguredObjects()
    {
        if (objectsToDisableOnFinish == null || objectsToDisableOnFinish.Length == 0)
            return;

        for (int i = 0; i < objectsToDisableOnFinish.Length; i++)
        {
            GameObject go = objectsToDisableOnFinish[i];
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }
    }

    private int GetSlideCount()
    {
        if (slideSettings != null && slideSettings.Length > 0)
            return slideSettings.Length;

        return slides != null ? slides.Length : 0;
    }

    private SlideSettings GetSlideSettingsAt(int index)
    {
        if (slideSettings == null || index < 0 || index >= slideSettings.Length)
            return null;

        return slideSettings[index];
    }

    private Sprite GetSlideSprite(int index)
    {
        SlideSettings s = GetSlideSettingsAt(index);
        if (s != null)
            return s.sprite;

        if (slides != null && index >= 0 && index < slides.Length)
            return slides[index];

        return null;
    }

    private float GetFadeInForSlide(int index)
    {
        SlideSettings s = GetSlideSettingsAt(index);
        if (s != null && s.overrideTiming)
            return GetSafePositive(s.fadeInDuration, 0.6f);

        return GetSafePositive(fadeInDuration, 0.6f);
    }

    private float GetHoldForSlide(int index)
    {
        SlideSettings s = GetSlideSettingsAt(index);
        if (s != null && s.overrideTiming)
            return GetSafeNonNegative(s.holdDuration, 1.5f);

        return GetSafeNonNegative(holdDuration, 1.5f);
    }

    private float GetFadeOutForSlide(int index)
    {
        SlideSettings s = GetSlideSettingsAt(index);
        if (s != null && s.overrideTiming)
            return GetSafePositive(s.fadeOutDuration, 0.6f);

        return GetSafePositive(fadeOutDuration, 0.6f);
    }

    private void MarkPlayingStarted()
    {
        if (_countedAsPlaying)
            return;

        _countedAsPlaying = true;
        bool wasPlaying = _activePlaybackCount > 0;
        _activePlaybackCount++;
        if (hideNoDeathRecordInThisScene)
            _activeHideNoDeathPlaybackCount++;

        if (!wasPlaying && _activePlaybackCount > 0)
            AnyIntroPlaybackChanged?.Invoke(true);
    }

    private void MarkPlayingStopped()
    {
        if (!_countedAsPlaying)
            return;

        _countedAsPlaying = false;
        _activePlaybackCount = Mathf.Max(0, _activePlaybackCount - 1);
        if (hideNoDeathRecordInThisScene)
            _activeHideNoDeathPlaybackCount = Mathf.Max(0, _activeHideNoDeathPlaybackCount - 1);

        if (_activePlaybackCount == 0)
            AnyIntroPlaybackChanged?.Invoke(false);
    }

    private void DisableConditionTargetImmediate()
    {
        DisableConfiguredObjects();
    }
}


