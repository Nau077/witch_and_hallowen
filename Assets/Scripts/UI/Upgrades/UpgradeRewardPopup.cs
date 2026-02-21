using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeRewardPopup : MonoBehaviour
{
    [Serializable]
    public class OptionSlotUI
    {
        public GameObject root;
        public Button button;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text descText;
    }

    [Header("Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool allowCloseWithoutSelection = false;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string showTrigger = "Show";
    [SerializeField] private string hideTrigger = "Hide";

    [Header("Options (1-3)")]
    [SerializeField] private OptionSlotUI[] options = new OptionSlotUI[3];

    private UpgradeRewardDefinition[] _currentOptions = Array.Empty<UpgradeRewardDefinition>();
    private Action<UpgradeRewardDefinition> _onSelected;
    private bool _isOpen;

    private void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
    }

    public bool IsOpen => _isOpen;

    public void Show(string title, UpgradeRewardDefinition[] rewardOptions, Action<UpgradeRewardDefinition> onSelected)
    {
        if (popupRoot == null)
        {
            Debug.LogError("[UpgradeRewardPopup] popupRoot is not assigned.");
            return;
        }

        _currentOptions = rewardOptions ?? Array.Empty<UpgradeRewardDefinition>();
        _onSelected = onSelected;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(title) ? "Choose Upgrade" : title;

        BuildOptions();

        popupRoot.SetActive(true);
        _isOpen = true;

        if (animator != null && !string.IsNullOrEmpty(showTrigger))
            animator.SetTrigger(showTrigger);
    }

    public void HideImmediate()
    {
        _isOpen = false;
        _onSelected = null;
        _currentOptions = Array.Empty<UpgradeRewardDefinition>();

        if (animator != null && !string.IsNullOrEmpty(hideTrigger))
            animator.SetTrigger(hideTrigger);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnClickClose()
    {
        if (allowCloseWithoutSelection)
        {
            HideImmediate();
            return;
        }

        // Safety: with a single option, close acts as confirm to avoid losing reward by UI misclick.
        if (_currentOptions != null && _currentOptions.Length == 1 && _currentOptions[0] != null)
        {
            SelectOption(0);
            return;
        }

        Debug.LogWarning("[UpgradeRewardPopup] Close ignored: select an upgrade option first.");
    }

    private void BuildOptions()
    {
        for (int i = 0; i < options.Length; i++)
        {
            var slot = options[i];
            if (slot == null || slot.root == null) continue;

            bool hasData = i < _currentOptions.Length && _currentOptions[i] != null;
            slot.root.SetActive(hasData);

            if (!hasData) continue;

            var reward = _currentOptions[i];

            if (slot.icon != null)
                slot.icon.sprite = reward.icon;

            if (slot.nameText != null)
                slot.nameText.text = reward.displayName;

            if (slot.descText != null)
                slot.descText.text = reward.description;

            if (slot.button != null)
            {
                int capturedIndex = i;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => SelectOption(capturedIndex));

                var tooltip = slot.button.GetComponent<HoverTooltipTrigger>();
                if (tooltip == null)
                    tooltip = slot.button.gameObject.AddComponent<HoverTooltipTrigger>();

                tooltip.Bind(() => BuildTooltipData(capturedIndex), 0.4f);
            }
        }
    }

    private HoverTooltipData BuildTooltipData(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= _currentOptions.Length)
            return default;

        var reward = _currentOptions[optionIndex];
        if (reward == null) return default;

        string levelLine = "";
        if (reward.rewardType == UpgradeRewardType.SkillUnlockOrUpgrade && reward.skillId != SkillId.None)
        {
            int current = PlayerSkills.Instance != null ? PlayerSkills.Instance.GetSkillLevel(reward.skillId) : 0;
            levelLine = "Skill level: " + current + " -> " + Mathf.Max(current, reward.targetSkillLevel);
        }
        else if (reward.rewardType == UpgradeRewardType.SkillCharges && reward.skillId != SkillId.None)
        {
            int currentCharges = PlayerSkills.Instance != null ? PlayerSkills.Instance.GetCharges(reward.skillId) : 0;
            levelLine = "Charges: " + currentCharges + " -> " + (currentCharges + Mathf.Max(0, reward.addCharges));
        }

        return new HoverTooltipData
        {
            title = reward.displayName,
            levelLine = levelLine,
            priceLine = "Reward",
            description = reward.description
        };
    }

    private void SelectOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= _currentOptions.Length)
            return;

        var selected = _currentOptions[optionIndex];
        var callback = _onSelected;
        HideImmediate();
        callback?.Invoke(selected);
    }
}
