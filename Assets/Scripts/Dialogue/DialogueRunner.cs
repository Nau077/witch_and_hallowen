using System;
using UnityEngine;

[DefaultExecutionOrder(-450)]
public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    [Header("UI")]
    public DialogueUI ui;

    [Header("Input lock")]
    [Tooltip("Если true — на время диалога ставим RunLevelManager.inputLocked=true (если есть).")]
    public bool lockGameplayInput = true;

    private DialogueSequenceSO _sequence;
    private int _index;
    private bool _playing;

    private Action _onFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (ui == null)
            ui = FindObjectOfType<DialogueUI>(true);

        if (ui != null)
            ui.OnContinuePressed += HandleContinue;
    }

    private void OnDestroy()
    {
        if (ui != null)
            ui.OnContinuePressed -= HandleContinue;
    }

    public bool IsPlaying => _playing;

    public void Play(DialogueSequenceSO sequence, Action onFinished = null)
    {
        if (sequence == null || sequence.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        if (ui == null)
            ui = FindObjectOfType<DialogueUI>(true);

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

        if (lockGameplayInput)
            SetGameplayLocked(true);

        // показать UI и первую реплику
        ui.SetContinueLabel("CONTINUE");
        ui.PlayShow();
        ui.DisplayLine(_sequence.GetLine(_index));
    }

    private void HandleContinue()
    {
        if (!_playing) return;

        _index++;

        if (_sequence == null || _index >= _sequence.Count)
        {
            Finish();
            return;
        }

        var line = _sequence.GetLine(_index);
        ui.DisplayLine(line);
    }

    private void Finish()
    {
        _playing = false;

        if (ui != null)
        {
            ui.PlayHide(() =>
            {
                if (lockGameplayInput)
                    SetGameplayLocked(false);

                _onFinished?.Invoke();
                _onFinished = null;
            });
        }
        else
        {
            if (lockGameplayInput)
                SetGameplayLocked(false);

            _onFinished?.Invoke();
            _onFinished = null;
        }
    }

    private void SetGameplayLocked(bool locked)
    {
        // Мы не лезем в твой PlayerMovement напрямую.
        // Основной общий механизм у тебя уже есть: RunLevelManager.inputLocked.
        if (RunLevelManager.Instance != null)
            RunLevelManager.Instance.SetInputLocked(locked);
    }
}
