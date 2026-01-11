using UnityEngine;

public class VictoryDialogueTrigger : MonoBehaviour
{
    public DialogueSequenceSO victorySequence;

    public void PlayFinalAndReturnToBase()
    {
        if (victorySequence == null || victorySequence.Count == 0)
        {
            RunLevelManager.Instance?.InitializeRun();
            return;
        }

        if (DialogueRunner.Instance == null)
        {
            RunLevelManager.Instance?.InitializeRun();
            return;
        }

        DialogueRunner.Instance.Play(victorySequence, () =>
        {
            RunLevelManager.Instance?.InitializeRun(); // stage 0
        });
    }
}
