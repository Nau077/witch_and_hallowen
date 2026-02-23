using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeRewardTriggerType
{
    StageCleared = 0,
    CustomEvent = 1
}

public class UpgradeRewardSystem : MonoBehaviour
{
    [Serializable]
    public class RewardRule
    {
        public string ruleId = "rule";
        public string popupTitle = "Upgade";
        public UpgradeRewardTriggerType triggerType = UpgradeRewardTriggerType.StageCleared;
        public int stageIndex = 1;
        public string customEventId = "";
        public UpgradeRewardDefinition[] options = new UpgradeRewardDefinition[1];
        public bool triggerOnlyOnce = true;
    }

    public static UpgradeRewardSystem Instance { get; private set; }

    [Header("Popup")]
    [SerializeField] private UpgradeRewardPopup rewardPopup;
    [SerializeField] private bool lockInputWhileOpen = true;
    [SerializeField] private int defaultChargesOnReward = 10;

    [Header("Rules")]
    [SerializeField] private RewardRule[] rules = Array.Empty<RewardRule>();

    private readonly HashSet<string> _triggeredRuleIds = new HashSet<string>();
    private readonly Queue<PendingReward> _queue = new Queue<PendingReward>();
    private bool _showing;

    private struct PendingReward
    {
        public RewardRule rule;
        public Action onComplete;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (rewardPopup == null)
            rewardPopup = FindObjectOfType<UpgradeRewardPopup>(true);
    }

    public static bool TriggerCustomEvent(string customEventId, Action onComplete = null)
    {
        if (Instance == null) return false;
        return Instance.TryTriggerCustomEvent(customEventId, onComplete);
    }

    public bool TryTriggerStageCleared(int clearedStage, Action onComplete = null)
    {
        bool anyQueued = false;

        for (int i = 0; i < rules.Length; i++)
        {
            var rule = rules[i];
            if (rule == null) continue;
            if (rule.triggerType != UpgradeRewardTriggerType.StageCleared) continue;
            if (rule.stageIndex != clearedStage) continue;
            if (!CanRunRule(rule)) continue;

            EnqueueRule(rule, onComplete);
            anyQueued = true;
            onComplete = null;
        }

        if (anyQueued)
            ShowNextIfNeeded();

        return anyQueued;
    }

    public bool TryTriggerCustomEvent(string customEventId, Action onComplete = null)
    {
        if (string.IsNullOrWhiteSpace(customEventId))
            return false;

        bool anyQueued = false;

        for (int i = 0; i < rules.Length; i++)
        {
            var rule = rules[i];
            if (rule == null) continue;
            if (rule.triggerType != UpgradeRewardTriggerType.CustomEvent) continue;
            if (!string.Equals(rule.customEventId, customEventId, StringComparison.Ordinal)) continue;
            if (!CanRunRule(rule)) continue;

            EnqueueRule(rule, onComplete);
            anyQueued = true;
            onComplete = null;
        }

        if (anyQueued)
            ShowNextIfNeeded();

        return anyQueued;
    }

    private bool CanRunRule(RewardRule rule)
    {
        if (rule == null) return false;
        if (rule.options == null || rule.options.Length == 0) return false;
        if (rule.triggerOnlyOnce && _triggeredRuleIds.Contains(rule.ruleId)) return false;

        for (int i = 0; i < rule.options.Length; i++)
        {
            var option = rule.options[i];
            if (option == null) continue;
            if (CanApplyReward(option))
                return true;
        }

        return false;
    }

    private void EnqueueRule(RewardRule rule, Action onComplete)
    {
        _queue.Enqueue(new PendingReward
        {
            rule = rule,
            onComplete = onComplete
        });
    }

    private void ShowNextIfNeeded()
    {
        if (_showing) return;
        if (_queue.Count == 0) return;

        if (rewardPopup == null)
        {
            Debug.LogError("[UpgradeRewardSystem] rewardPopup is not assigned.");
            CompleteQueueWithoutPopup();
            return;
        }

        var pending = _queue.Dequeue();
        var validOptions = FilterValidOptions(pending.rule.options);

        if (validOptions.Length == 0)
        {
            MarkRuleTriggered(pending.rule);
            pending.onComplete?.Invoke();
            ShowNextIfNeeded();
            return;
        }

        _showing = true;

        if (lockInputWhileOpen)
        {
            RunLevelManager.Instance?.SetInputLocked(true);
            CursorManager.Instance?.SetPopupBlocking(true);
        }

        rewardPopup.Show(pending.rule.popupTitle, validOptions, selected =>
        {
            ApplyReward(selected);
            MarkRuleTriggered(pending.rule);

            if (lockInputWhileOpen)
            {
                RunLevelManager.Instance?.SetInputLocked(false);
                CursorManager.Instance?.SetPopupBlocking(false);
            }

            _showing = false;
            pending.onComplete?.Invoke();
            ShowNextIfNeeded();
        });
    }

    private void CompleteQueueWithoutPopup()
    {
        while (_queue.Count > 0)
        {
            var pending = _queue.Dequeue();
            if (pending.rule != null)
                MarkRuleTriggered(pending.rule);
            pending.onComplete?.Invoke();
        }
    }

    private void MarkRuleTriggered(RewardRule rule)
    {
        if (rule == null || !rule.triggerOnlyOnce) return;
        if (string.IsNullOrWhiteSpace(rule.ruleId)) return;
        _triggeredRuleIds.Add(rule.ruleId);
    }

    private UpgradeRewardDefinition[] FilterValidOptions(UpgradeRewardDefinition[] options)
    {
        if (options == null || options.Length == 0)
            return Array.Empty<UpgradeRewardDefinition>();

        var list = new List<UpgradeRewardDefinition>(options.Length);
        for (int i = 0; i < options.Length; i++)
        {
            var reward = options[i];
            if (reward == null) continue;
            if (!CanApplyReward(reward)) continue;
            list.Add(reward);
        }

        return list.ToArray();
    }

    private bool CanApplyReward(UpgradeRewardDefinition reward)
    {
        if (reward == null) return false;

        switch (reward.rewardType)
        {
            case UpgradeRewardType.SkillUnlockOrUpgrade:
                if (reward.skillId == SkillId.None) return false;
                if (PlayerSkills.Instance == null) return false;
                return PlayerSkills.Instance.GetSkillLevel(reward.skillId) < reward.targetSkillLevel;

            case UpgradeRewardType.SkillCharges:
                if (reward.skillId == SkillId.None) return false;
                if (PlayerSkills.Instance == null) return false;
                return PlayerSkills.Instance.GetSkillLevel(reward.skillId) > 0
                    && (reward.addCharges > 0 || defaultChargesOnReward > 0);

            case UpgradeRewardType.HealthHeart:
                return SoulPerksManager.Instance != null && SoulPerksManager.Instance.CanGrantHealthLevel(reward.amount);

            case UpgradeRewardType.ManaHeart:
                return SoulPerksManager.Instance != null && SoulPerksManager.Instance.CanGrantManaLevel(reward.amount);

            case UpgradeRewardType.StaminaHeart:
                return SoulPerksManager.Instance != null && SoulPerksManager.Instance.CanGrantStaminaLevel(reward.amount);
        }

        return false;
    }

    private void ApplyReward(UpgradeRewardDefinition reward)
    {
        if (reward == null) return;

        switch (reward.rewardType)
        {
            case UpgradeRewardType.SkillUnlockOrUpgrade:
                ApplySkillUnlockOrUpgrade(reward);
                break;

            case UpgradeRewardType.SkillCharges:
                ApplySkillCharges(reward);
                break;

            case UpgradeRewardType.HealthHeart:
                SoulPerksManager.Instance?.GrantHealthLevel(reward.amount);
                break;

            case UpgradeRewardType.ManaHeart:
                SoulPerksManager.Instance?.GrantManaLevel(reward.amount);
                break;

            case UpgradeRewardType.StaminaHeart:
                SoulPerksManager.Instance?.GrantStaminaLevel(reward.amount);
                break;
        }
    }

    private void ApplySkillUnlockOrUpgrade(UpgradeRewardDefinition reward)
    {
        if (PlayerSkills.Instance == null) return;
        if (reward.skillId == SkillId.None) return;

        int targetLevel = Mathf.Max(1, reward.targetSkillLevel);
        PlayerSkills.Instance.UnlockSkill(reward.skillId, targetLevel);

        int chargesToGrant = reward.addCharges > 0 ? reward.addCharges : Mathf.Max(0, defaultChargesOnReward);
        if (chargesToGrant > 0)
            PlayerSkills.Instance.AddCharges(reward.skillId, chargesToGrant);

        if (!reward.addToLoadout) return;

        var loadout = SkillLoadout.Instance;
        if (loadout == null) return;

        var def = SkillDefinitionLookup.FindById(reward.skillId);
        if (def == null)
        {
            Debug.LogWarning("[UpgradeRewardSystem] SkillDefinition not found for " + reward.skillId);
            return;
        }

        int loadoutCharges = def.infiniteCharges ? 0 : Mathf.Max(1, chargesToGrant);
        loadout.AddChargesToSkill(def, loadoutCharges);
    }

    private void ApplySkillCharges(UpgradeRewardDefinition reward)
    {
        if (PlayerSkills.Instance == null) return;
        if (reward.skillId == SkillId.None) return;
        int chargesToGrant = reward.addCharges > 0 ? reward.addCharges : Mathf.Max(0, defaultChargesOnReward);
        if (chargesToGrant <= 0) return;

        PlayerSkills.Instance.AddCharges(reward.skillId, chargesToGrant);

        var loadout = SkillLoadout.Instance;
        if (loadout == null) return;

        var def = SkillDefinitionLookup.FindById(reward.skillId);
        if (def == null)
        {
            Debug.LogWarning("[UpgradeRewardSystem] SkillDefinition not found for " + reward.skillId);
            return;
        }

        loadout.AddChargesToSkill(def, chargesToGrant);
    }
}
