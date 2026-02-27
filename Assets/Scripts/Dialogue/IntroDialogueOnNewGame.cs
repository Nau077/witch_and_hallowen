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

    [Header("Fireball Reward After Intro (New Game only)")]
    [SerializeField] private bool removeDefaultFireballBeforeIntro = true;
    [SerializeField] private bool triggerFireballRewardAfterIntro = true;
    [SerializeField] private string fireballRewardCustomEventId = "new_game_intro_fireball_reward";

    [Header("Tutorial Popup After Fireball Reward")]
    [SerializeField] private TutorialHintPopup tutorialHintPopup;
    [SerializeField] private TutorialHintDefinition tutorialHintAfterReward;

    private void Start()
    {
        int mode = PlayerPrefs.GetInt(BOOT_MODE_KEY, 0);
        if (mode != BOOT_NEW_GAME) return;

        if (removeDefaultFireballBeforeIntro)
            RemoveFireballFromStartState();

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

            if (triggerFireballRewardAfterIntro)
                TriggerFireballReward();
        });
    }

    private void RemoveFireballFromStartState()
    {
        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.ResetSkill(SkillId.Fireball);

        var loadout = SkillLoadout.Instance;
        if (loadout == null || loadout.slots == null)
            return;

        for (int i = 0; i < loadout.slots.Length; i++)
        {
            var slot = loadout.slots[i];
            if (slot == null || slot.def == null)
                continue;

            if (slot.def.skillId != SkillId.Fireball)
                continue;

            slot.def = null;
            slot.charges = 0;
            slot.cooldownUntil = 0f;
        }

        loadout.EnsureValidActive();
    }

    private void TriggerFireballReward()
    {
        if (string.IsNullOrWhiteSpace(fireballRewardCustomEventId))
        {
            Debug.LogWarning("[IntroDialogueOnNewGame] fireballRewardCustomEventId is empty.");
            return;
        }

        bool queued = UpgradeRewardSystem.TriggerCustomEvent(fireballRewardCustomEventId, OnFireballRewardComplete);
        if (!queued)
            Debug.LogWarning("[IntroDialogueOnNewGame] Fireball reward rule was not triggered. Check UpgradeRewardSystem rules/customEventId.");
    }

    private void OnFireballRewardComplete()
    {
        if (tutorialHintPopup != null && tutorialHintAfterReward != null)
            tutorialHintPopup.Show(tutorialHintAfterReward);
    }
}
