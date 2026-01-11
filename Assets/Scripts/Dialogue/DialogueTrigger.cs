using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        OnStart,
        Manual
    }

    public TriggerMode mode = TriggerMode.OnStart;

    [Tooltip("Какой диалог проиграть.")]
    public DialogueSequenceSO sequence;

    [Tooltip("Проигрывать только один раз (используем PlayerPrefs ключ).")]
    public bool playOnlyOnce = false;

    [Tooltip("Ключ, чтобы помнить, что уже проигрывали. Если пусто — будет auto.")]
    public string onceKeyOverride = "";

    private string OnceKey
    {
        get
        {
            if (!string.IsNullOrEmpty(onceKeyOverride)) return onceKeyOverride;
            return "dlg_once_" + gameObject.scene.name + "_" + gameObject.name;
        }
    }

    private void Start()
    {
        if (mode == TriggerMode.OnStart)
            TryPlay();
    }

    public void TryPlay()
    {
        if (sequence == null) return;
        if (DialogueRunner.Instance == null) return;

        if (playOnlyOnce)
        {
            if (PlayerPrefs.GetInt(OnceKey, 0) == 1) return;
            PlayerPrefs.SetInt(OnceKey, 1);
            PlayerPrefs.Save();
        }

        DialogueRunner.Instance.Play(sequence);
    }

    public void ForcePlay()
    {
        if (sequence == null) return;
        if (DialogueRunner.Instance == null) return;
        DialogueRunner.Instance.Play(sequence);
    }
}
