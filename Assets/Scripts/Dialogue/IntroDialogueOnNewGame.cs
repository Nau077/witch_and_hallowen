using UnityEngine;

public class IntroDialogueOnNewGame : MonoBehaviour
{
    private const string BOOT_MODE_KEY = "dw_boot_mode";
    private const int BOOT_NEW_GAME = 1;

    [Header("Intro Dialogue")]
    public DialogueSequenceSO introSequence;

    [Tooltip("Если true — после показа интро, сбросим boot_mode, чтобы при перезаходе не показывать снова.")]
    public bool clearBootModeAfterPlay = true;

    private void Start()
    {
        int mode = PlayerPrefs.GetInt(BOOT_MODE_KEY, 0);
        if (mode != BOOT_NEW_GAME) return;

        if (DialogueRunner.Instance == null)
        {
            Debug.LogWarning("[IntroDialogueOnNewGame] DialogueRunner not found.");
            return;
        }

        if (introSequence == null || introSequence.Count == 0)
        {
            Debug.LogWarning("[IntroDialogueOnNewGame] introSequence is empty.");
            return;
        }

        // На время диалога RunLevelManager.inputLocked включится через DialogueRunner (у тебя уже есть lockGameplayInput)
        DialogueRunner.Instance.Play(introSequence, () =>
        {
            if (clearBootModeAfterPlay)
            {
                PlayerPrefs.SetInt(BOOT_MODE_KEY, 0);
                PlayerPrefs.Save();
            }
        });
    }
}
