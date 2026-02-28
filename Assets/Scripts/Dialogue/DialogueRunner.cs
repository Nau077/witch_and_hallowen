using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-450)]
public class DialogueRunner : MonoBehaviour
{
    private const float HardMinimumContinueGuardSeconds = 0.35f;
    public static DialogueRunner Instance { get; private set; }

    [Header("UI")]
    public DialogueUI ui;

    [Header("Input lock")]
    public bool lockGameplayInput = true;

    [Header("Continue Guard")]
    [Min(0f)] public float minContinueDelayAfterPlay = 0.25f;

    private DialogueSequenceSO _sequence;
    private int _index;
    private bool _playing;
    private Action _onFinished;
    private float _playStartedAtUnscaledTime;

    private void Awake()
    {
        if (minContinueDelayAfterPlay <= 0f)
            minContinueDelayAfterPlay = 0.25f;

        if (Instance != null && Instance != this)
        {
            Instance.TryAdoptUIFrom(this);
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        RefreshUIBinding();
    }

    private void OnDestroy()
    {
        if (ui != null)
            ui.OnContinuePressed -= HandleContinue;

        if (Instance == this)
            Instance = null;
    }

    public bool IsPlaying => _playing;

    public void Play(DialogueSequenceSO sequence, Action onFinished = null)
    {
        if (sequence == null || sequence.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        RefreshUIBinding();

        if (ui == null)
        {
            Debug.LogWarning("[DialogueRunner] DialogueUI not found in scene.");
            onFinished?.Invoke();
            return;
        }

        _sequence = sequence;
        _index = 0;
        _playing = true;
        _onFinished = onFinished;
        _playStartedAtUnscaledTime = Time.unscaledTime;

        if (lockGameplayInput)
            SetGameplayLocked(true);

        ui.SetContinueLabel("CONTINUE");
        ui.BeginConversation();
        ui.PlayShow();

        // ?????? ???????
        ui.DisplayLineAnimated(_sequence.GetLine(_index));
    }

    private void HandleContinue()
    {
        if (!_playing) return;
        float continueGuard = Mathf.Max(minContinueDelayAfterPlay, HardMinimumContinueGuardSeconds);
        if (Time.unscaledTime - _playStartedAtUnscaledTime < continueGuard)
            return;

        _index++;

        if (_sequence == null || _index >= _sequence.Count)
        {
            Finish();
            return;
        }

        ui.DisplayLineAnimated(_sequence.GetLine(_index));
    }

    private void Finish()
    {
        _playing = false;

        if (ui != null)
        {
            ui.PlayHide(() =>
            {
                // ?????????: ?????????????? ???????? ???
                ui.HideImmediate();

                if (lockGameplayInput)
                    SetGameplayLocked(false);

                _onFinished?.Invoke();
                _onFinished = null;

                _sequence = null;
                _index = 0;
            });
        }
        else
        {
            if (lockGameplayInput)
                SetGameplayLocked(false);

            _onFinished?.Invoke();
            _onFinished = null;

            _sequence = null;
            _index = 0;
        }
    }

    private void SetGameplayLocked(bool locked)
    {
        if (RunLevelManager.Instance != null)
            RunLevelManager.Instance.SetInputLocked(locked);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshUIBinding();
    }

    private void RefreshUIBinding()
    {
        DialogueUI nextUi = ui;
        if (nextUi == null)
            nextUi = FindObjectOfType<DialogueUI>(true);

        if (ui != nextUi && ui != null)
            ui.OnContinuePressed -= HandleContinue;

        ui = nextUi;
        if (ui != null)
        {
            ui.OnContinuePressed -= HandleContinue;
            ui.OnContinuePressed += HandleContinue;
        }
    }

    private void TryAdoptUIFrom(DialogueRunner other)
    {
        if (other == null || other.ui == null || ui != null)
            return;

        ui = other.ui;
        RefreshUIBinding();
    }
}
