using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeRewardPopup : MonoBehaviour
{
    [Serializable]
    public class OptionUI
    {
        public GameObject root;
        public Button button;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text descText;
        public Image highlightImage;
    }

    [Header("Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private bool allowCloseWithoutSelection;

    [Header("Selection Visual")]
    [SerializeField] private Color selectedTint = Color.white;
    [SerializeField] private Color normalTint = Color.white;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float normalScale = 1f;
    [Header("Selected Icon Glow (like dialogue Next)")]
    [SerializeField] private bool selectedIconGlowEnabled = true;
    [SerializeField] private float selectedIconGlowPulsePeriod = 0.9f;
    [SerializeField, Range(0f, 1f)] private float selectedIconGlowMinAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float selectedIconGlowMaxAlpha = 1f;
    [SerializeField] private Color selectedIconGlowColor = new Color(0.35f, 0.85f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float selectedIconGlowTintStrength = 0.18f;
    [SerializeField] private Vector2 selectedIconGlowOutlineDistance = new Vector2(2f, -2f);

    [Header("Animation (optional)")]
    [SerializeField] private bool showInstantly = false;
    [SerializeField] private bool speedUpAnimatorOnShow = true;
    [SerializeField] private float popupAnimatorSpeed = 12f;
    [SerializeField] private Animator animator;
    [SerializeField] private string showTrigger = "Show";
    [SerializeField] private string hideTrigger = "Hide";
    [SerializeField] private PopupFadeCanvas popupFade;

    [Header("Options (1-3)")]
    [SerializeField] private OptionUI[] options = new OptionUI[3];

    private readonly List<BoundOption> _active = new List<BoundOption>(3);
    private Action<UpgradeRewardDefinition> _onSelected;
    private int _selectedActiveIndex = -1;
    private Coroutine _selectedIconGlowRoutine;
    private OptionUI _selectedGlowOption;

    private struct BoundOption
    {
        public OptionUI ui;
        public UpgradeRewardDefinition reward;
    }

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        SetupTitleText();

        if (popupFade == null)
            popupFade = GetComponent<PopupFadeCanvas>();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickGet);

        HideImmediate();
    }

    private void OnDestroy()
    {
        StopSelectedIconGlow();

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnClickGet);

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i]?.button != null)
                options[i].button.onClick.RemoveAllListeners();
        }
    }

    public void Show(string popupTitle, UpgradeRewardDefinition[] rewardOptions, Action<UpgradeRewardDefinition> onSelected)
    {
        _onSelected = onSelected;
        _selectedActiveIndex = -1;
        _active.Clear();

        EnsureRuntimeOptionBindings();

        if (titleText != null)
        {
            SetupTitleText();
            titleText.text = string.IsNullOrWhiteSpace(popupTitle) ? "Upgade" : popupTitle;
        }

        HideAllOptions();

        if (rewardOptions == null || rewardOptions.Length == 0)
        {
            Debug.LogWarning("[UpgradeRewardPopup] No reward options passed.");
            ShowRoot();
            return;
        }

        int rewardCount = Mathf.Min(3, rewardOptions.Length);
        List<int> usableSlotIndexes = GetUsableSlotIndexes();
        List<int> targetSlotIndexes = ResolveTargetSlotIndexes(usableSlotIndexes, rewardCount);

        int shown = 0;
        for (int i = 0; i < targetSlotIndexes.Count && i < rewardCount; i++)
        {
            UpgradeRewardDefinition reward = rewardOptions[i];
            if (reward == null)
                continue;

            OptionUI ui = options[targetSlotIndexes[i]];
            if (!IsUsable(ui))
                continue;

            BindOption(ui, reward, shown);
            _active.Add(new BoundOption { ui = ui, reward = reward });
            shown++;
        }

        ShowRoot();

        if (_active.Count == 0)
            Debug.LogWarning("[UpgradeRewardPopup] No usable UI slots to show rewards. Check Options array bindings.");
        else if (_active.Count == 1)
            SelectActiveIndex(0);
    }

    public void HideImmediate()
    {
        StopSelectedIconGlow();
        _active.Clear();
        _selectedActiveIndex = -1;

        if (popupFade != null)
        {
            popupFade.HideImmediate();
        }
        else if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void OnClickGet()
    {
        if (_selectedActiveIndex < 0 || _selectedActiveIndex >= _active.Count)
        {
            if (!allowCloseWithoutSelection)
            {
                Debug.LogWarning("[UpgradeRewardPopup] Select an option, then press GET.");
                return;
            }

            CloseWithoutReward();
            return;
        }

        UpgradeRewardDefinition picked = _active[_selectedActiveIndex].reward;
        HideWithAnimation();
        _onSelected?.Invoke(picked);
        _onSelected = null;
    }

    private void CloseWithoutReward()
    {
        HideWithAnimation();
        _onSelected?.Invoke(null);
        _onSelected = null;
    }

    private void ShowRoot()
    {
        if (popupRoot == null)
            return;

        if (popupFade != null)
        {
            if (showInstantly)
                popupFade.ShowImmediate();
            else
                popupFade.ShowSmooth();
        }
        else
        {
            popupRoot.SetActive(true);
            if (animator != null && !string.IsNullOrEmpty(showTrigger))
            {
                if (speedUpAnimatorOnShow)
                    animator.speed = Mathf.Max(12f, popupAnimatorSpeed);
                animator.SetTrigger(showTrigger);
            }
        }
    }

    private void HideWithAnimation()
    {
        StopSelectedIconGlow();
        _active.Clear();
        _selectedActiveIndex = -1;

        if (popupFade != null)
        {
            popupFade.HideSmooth();
            return;
        }

        if (animator != null && !string.IsNullOrEmpty(hideTrigger))
            animator.SetTrigger(hideTrigger);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void BindOption(OptionUI ui, UpgradeRewardDefinition reward, int activeIndex)
    {
        if (ui.root != null)
            ui.root.SetActive(true);

        if (ui.icon != null)
            ui.icon.sprite = reward.icon;

        if (ui.nameText != null)
            ui.nameText.text = reward.displayName;

        if (ui.descText != null)
            ui.descText.text = reward.description;

        if (ui.button != null)
        {
            ui.button.onClick.RemoveAllListeners();
            ui.button.onClick.AddListener(() => SelectActiveIndex(activeIndex));
            BindTooltip(ui.button.gameObject, reward);
        }

        SetOptionVisual(ui, false);
    }

    private void SelectActiveIndex(int activeIndex)
    {
        if (activeIndex < 0 || activeIndex >= _active.Count)
            return;

        _selectedActiveIndex = activeIndex;

        for (int i = 0; i < _active.Count; i++)
            SetOptionVisual(_active[i].ui, i == _selectedActiveIndex);

        if (selectedIconGlowEnabled)
            StartSelectedIconGlow(_active[_selectedActiveIndex].ui);
        else
            StopSelectedIconGlow();
    }

    private void SetOptionVisual(OptionUI ui, bool selected)
    {
        if (ui == null)
            return;

        if (ui.root != null)
            ui.root.transform.localScale = Vector3.one * (selected ? selectedScale : normalScale);

        if (ui.icon != null)
        {
            ui.icon.color = selected ? selectedTint : normalTint;
            if (!selected)
                SetIconGlowVisual(ui, 0f);
        }

        if (ui.highlightImage != null)
        {
            ui.highlightImage.enabled = selected;
            ui.highlightImage.color = selected ? selectedTint : normalTint;
        }
        else
        {
            Graphic g = null;
            if (ui.button != null)
                g = ui.button.image;
            if (g == null)
                g = ui.icon;
            if (g != null)
                g.color = selected ? selectedTint : normalTint;
        }
    }

    private void StartSelectedIconGlow(OptionUI ui)
    {
        StopSelectedIconGlow();

        if (ui == null || ui.icon == null)
            return;

        _selectedGlowOption = ui;
        _selectedIconGlowRoutine = StartCoroutine(SelectedIconGlowRoutine());
    }

    private void StopSelectedIconGlow()
    {
        if (_selectedIconGlowRoutine != null)
        {
            StopCoroutine(_selectedIconGlowRoutine);
            _selectedIconGlowRoutine = null;
        }

        if (_selectedGlowOption != null)
            SetIconGlowVisual(_selectedGlowOption, 0f);

        _selectedGlowOption = null;
    }

    private IEnumerator SelectedIconGlowRoutine()
    {
        float period = Mathf.Max(0.15f, selectedIconGlowPulsePeriod);

        while (_selectedGlowOption != null && _selectedGlowOption.icon != null)
        {
            float t = 0f;
            while (t < period && _selectedGlowOption != null && _selectedGlowOption.icon != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / period);
                float wave = 0.5f - 0.5f * Mathf.Cos(k * Mathf.PI * 2f); // 0..1..0
                SetIconGlowVisual(_selectedGlowOption, wave);
                yield return null;
            }
        }

        _selectedIconGlowRoutine = null;
    }

    private void SetIconGlowVisual(OptionUI ui, float intensity01)
    {
        if (ui == null || ui.icon == null)
            return;

        intensity01 = Mathf.Clamp01(intensity01);

        var outline = ui.icon.GetComponent<Outline>();
        if (outline == null)
            outline = ui.icon.gameObject.AddComponent<Outline>();

        outline.effectDistance = selectedIconGlowOutlineDistance;

        float alpha = Mathf.Lerp(selectedIconGlowMinAlpha, selectedIconGlowMaxAlpha, intensity01);
        Color outlineColor = selectedIconGlowColor;
        outlineColor.a *= alpha;
        outline.effectColor = outlineColor;
        outline.enabled = alpha > 0.01f;

        Color tint = new Color(selectedIconGlowColor.r, selectedIconGlowColor.g, selectedIconGlowColor.b, 1f);
        float s = selectedIconGlowTintStrength * intensity01;
        ui.icon.color = Color.Lerp(selectedTint, tint, s);
    }

    private void HideAllOptions()
    {
        for (int i = 0; i < options.Length; i++)
        {
            OptionUI ui = options[i];
            if (ui == null)
                continue;

            if (ui.root != null)
                ui.root.SetActive(false);

            if (ui.button != null)
                ui.button.onClick.RemoveAllListeners();

            SetOptionVisual(ui, false);
        }
    }

    private List<int> GetUsableSlotIndexes()
    {
        var indexes = new List<int>(3);

        for (int i = 0; i < options.Length; i++)
        {
            if (IsUsable(options[i]))
                indexes.Add(i);
        }

        return indexes;
    }

    private List<int> ResolveTargetSlotIndexes(List<int> usable, int rewardCount)
    {
        var result = new List<int>(3);
        if (usable == null || usable.Count == 0 || rewardCount <= 0)
            return result;

        int count = Mathf.Min(rewardCount, usable.Count);

        if (count == 1)
        {
            int middle = usable.Count / 2;
            result.Add(usable[middle]);
            return result;
        }

        if (count == 2)
        {
            if (usable.Count >= 3)
            {
                result.Add(usable[0]);
                result.Add(usable[usable.Count - 1]);
                return result;
            }

            result.Add(usable[0]);
            result.Add(usable[1]);
            return result;
        }

        result.Add(usable[0]);
        result.Add(usable[Mathf.Min(1, usable.Count - 1)]);
        result.Add(usable[Mathf.Min(2, usable.Count - 1)]);
        return result;
    }

    private void EnsureRuntimeOptionBindings()
    {
        if (popupRoot == null)
            return;

        var assignedButtons = new HashSet<Button>();

        for (int i = 0; i < options.Length; i++)
        {
            OptionUI ui = options[i];
            if (ui == null)
            {
                options[i] = new OptionUI();
                ui = options[i];
            }

            if (ui.button != null)
            {
                assignedButtons.Add(ui.button);
                if (ui.root == null)
                    ui.root = ui.button.gameObject;
                if (ui.icon == null)
                    ui.icon = TryFindIcon(ui.button.transform);
            }
        }

        Button[] allButtons = popupRoot.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < options.Length; i++)
        {
            OptionUI ui = options[i];
            if (IsUsable(ui))
                continue;

            Button candidate = FindCandidateButton(allButtons, assignedButtons);
            if (candidate == null)
                continue;

            ui.button = candidate;
            ui.root = candidate.gameObject;
            ui.icon = TryFindIcon(candidate.transform);
            assignedButtons.Add(candidate);
        }
    }

    private Button FindCandidateButton(Button[] allButtons, HashSet<Button> assigned)
    {
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            if (button == null) continue;
            if (button == closeButton) continue;
            if (assigned.Contains(button)) continue;

            string lowerName = button.gameObject.name.ToLowerInvariant();
            if (!lowerName.Contains("slot") && !lowerName.Contains("reward"))
                continue;

            return button;
        }

        return null;
    }

    private static Image TryFindIcon(Transform root)
    {
        if (root == null) return null;

        Transform iconTransform = root.Find("Icon");
        if (iconTransform != null)
        {
            Image image = iconTransform.GetComponent<Image>();
            if (image != null) return image;
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            if (images[i].gameObject == root.gameObject) continue;
            return images[i];
        }

        return root.GetComponent<Image>();
    }

    private static bool IsUsable(OptionUI ui)
    {
        return ui != null && ui.button != null;
    }

    private void BindTooltip(GameObject target, UpgradeRewardDefinition reward)
    {
        if (target == null || reward == null)
            return;

        var trigger = target.GetComponent<HoverTooltipTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<HoverTooltipTrigger>();

        trigger.Bind(() => BuildTooltipData(reward), 0.2f);
    }

    private HoverTooltipData BuildTooltipData(UpgradeRewardDefinition reward)
    {
        string levelLine = "";
        string priceLine = "";

        if (reward.rewardType == UpgradeRewardType.SkillUnlockOrUpgrade)
        {
            int currentLevel = PlayerSkills.Instance != null ? PlayerSkills.Instance.GetSkillLevel(reward.skillId) : 0;
            levelLine = TooltipLocalization.Tr("Level: ", "Уровень: ") + currentLevel +
                        TooltipLocalization.Tr(" -> ", " -> ") +
                        Mathf.Max(1, reward.targetSkillLevel);
        }
        else if (reward.rewardType == UpgradeRewardType.SkillCharges)
        {
            int charges = reward.addCharges > 0 ? reward.addCharges : 10;
            priceLine = TooltipLocalization.Tr("Charges: +", "Заряды: +") + charges;
        }
        else if (reward.rewardType == UpgradeRewardType.HealthHeart ||
                 reward.rewardType == UpgradeRewardType.ManaHeart ||
                 reward.rewardType == UpgradeRewardType.StaminaHeart)
        {
            priceLine = TooltipLocalization.Tr("Amount: +", "Количество: +") + Mathf.Max(1, reward.amount);
        }

        return new HoverTooltipData
        {
            title = string.IsNullOrWhiteSpace(reward.displayName) ? reward.rewardId : reward.displayName,
            levelLine = levelLine,
            priceLine = priceLine,
            description = reward.description
        };
    }

    private void SetupTitleText()
    {
        if (titleText == null) return;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
    }
}
