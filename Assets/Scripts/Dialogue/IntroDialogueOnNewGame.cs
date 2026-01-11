using System.Collections;
using UnityEngine;

public class IntroDialogueOnNewGame : MonoBehaviour
{
    private const string BOOT_MODE_KEY = "dw_boot_mode";
    private const int BOOT_NEW_GAME = 1;

    [Header("Intro Dialogue")]
    public DialogueSequenceSO introSequence;

    [Header("Timing")]
    [Range(0f, 5f)]
    public float startDelay = 1.8f;

    [Tooltip("Если true — после интро сбросим boot_mode, чтобы при перезаходе не показывать снова.")]
    public bool clearBootModeAfterPlay = true;

    private void Start()
    {
        int mode = PlayerPrefs.GetInt(BOOT_MODE_KEY, 0);
        if (mode != BOOT_NEW_GAME) return;

        if (introSequence == null || introSequence.Count == 0) return;

        StartCoroutine(PlayDelayed());
    }

    private IEnumerator PlayDelayed()
    {
        // ждать в unscaled, чтобы не зависеть от timeScale
        float t = 0f;
        while (t < startDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (DialogueRunner.Instance == null)
        {
            Debug.LogWarning("[IntroDialogueOnNewGame] DialogueRunner not found.");
            yield break;
        }

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
