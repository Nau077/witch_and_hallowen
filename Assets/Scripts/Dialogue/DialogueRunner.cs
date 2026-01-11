using System;
using UnityEngine;

[DefaultExecutionOrder(-450)]
public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    [Header("UI")]
    public DialogueUI ui;

    [Header("Input lock")]
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

        ui.SetContinueLabel("CONTINUE");
        ui.BeginConversation();
        ui.PlayShow();

        // первая реплика
        ui.DisplayLineAnimated(_sequence.GetLine(_index));
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

        ui.DisplayLineAnimated(_sequence.GetLine(_index));
    }

    private void Finish()
    {
        _playing = false;

        if (ui != null)
        {
            ui.PlayHide(() =>
            {
                // страховка: гарантированно погасить всё
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
}
