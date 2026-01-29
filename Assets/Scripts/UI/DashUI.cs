using UnityEngine;
using UnityEngine.UI;

public class DashUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerDash dash;

    [Header("Icon by Level")]
    public Image dashIcon;
    public Sprite iconLevel1;
    public Sprite iconLevel2;
    public Sprite iconLevel3;

    [Header("Energy bar (yellow vertical)")]
    public Image energyFill; // Image Type = Filled, Fill Method = Vertical, Origin = Bottom

    private int lastLevel = -1;

    private void Awake()
    {
        if (!dash) dash = FindObjectOfType<PlayerDash>();
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged += OnPerksChanged;
    }

    private void OnDestroy()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged -= OnPerksChanged;
    }

    private void Start()
    {
        RefreshIconImmediate();
        RefreshEnergyImmediate();
    }

    private void Update()
    {
        RefreshEnergyImmediate();

        int lvl = GetDashLevelSafe();
        if (lvl != lastLevel)
            RefreshIconImmediate();
    }

    private void OnPerksChanged()
    {
        RefreshIconImmediate();
    }

    private int GetDashLevelSafe()
    {
        var perks = SoulPerksManager.Instance;
        return perks != null ? perks.GetDashRealLevel() : 1;
    }

    public void RefreshIconImmediate()
    {
        int lvl = GetDashLevelSafe();
        lastLevel = lvl;

        if (!dashIcon) return;

        Sprite s = lvl switch
        {
            1 => iconLevel1,
            2 => iconLevel2,
            3 => iconLevel3,
            _ => iconLevel1
        };

        if (s != null)
            dashIcon.sprite = s;
    }

    public void RefreshEnergyImmediate()
    {
        if (!energyFill || dash == null) return;
        energyFill.fillAmount = dash.EnergyNormalized;
    }
}
