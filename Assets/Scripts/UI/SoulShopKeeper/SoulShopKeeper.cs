using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SoulShopKeeper : MonoBehaviour
{
    [Tooltip("Reference to SoulShopKeeperPopup.")]
    [SerializeField] private SoulShopKeeperPopup popup;

    [Header("Hover Tooltip")]
    [SerializeField] private bool enableHoverTooltip = true;
    [SerializeField] private string tooltipTitleEn = "SoulKeeper";
    [SerializeField] private string tooltipDescriptionEn = "Forest guardian who saved Witchy and teaches her magic.";
    [SerializeField, Min(0f)] private float tooltipDelay = 0.2f;

    private HoverTooltipTrigger _hoverTooltipTrigger;

    private void Awake()
    {
        if (!enableHoverTooltip)
            return;

        _hoverTooltipTrigger = GetComponent<HoverTooltipTrigger>();
        if (_hoverTooltipTrigger == null)
            _hoverTooltipTrigger = gameObject.AddComponent<HoverTooltipTrigger>();

        _hoverTooltipTrigger.Bind(BuildHoverTooltipData, tooltipDelay);
    }

    private void OnMouseDown()
    {
        var shooter = FindObjectOfType<PlayerSkillShooter>();
        if (shooter != null)
            shooter.SkipNextClickFromUI();

        if (popup != null)
            popup.Show();
        else
            Debug.LogWarning("SoulShopKeeper: popup is not assigned in the inspector.");
    }

    private void OnMouseEnter()
    {
        if (!enableHoverTooltip || _hoverTooltipTrigger == null)
            return;

        HoverTooltipUI.Instance.ShowFrom(_hoverTooltipTrigger);
    }

    private void OnMouseExit()
    {
        if (!enableHoverTooltip || _hoverTooltipTrigger == null)
            return;

        HoverTooltipUI.Instance.HideFrom(_hoverTooltipTrigger);
    }

    private HoverTooltipData BuildHoverTooltipData()
    {
        return new HoverTooltipData
        {
            title = tooltipTitleEn,
            levelLine = string.Empty,
            priceLine = string.Empty,
            description = tooltipDescriptionEn
        };
    }
}
