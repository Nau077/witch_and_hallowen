using UnityEngine;

public class ShopKeeperManager : MonoBehaviour
{
    public static ShopKeeperManager Instance { get; private set; }

    [Header("NPC reference")]
    public GameObject shopKeeperNPC;

    [Header("Spawn rules")]
    [Tooltip("Появляется ли NPC на базе (stage = 0).")]
    public bool appearOnBase = true;

    [Tooltip("Появляется ли NPC после прохождения уровня (например, перед 'следующим лесом').")]
    public bool appearAfterStageClear = false;

    [Tooltip("Этажи, где NPC появляется, даже если это не база.")]
    public int[] appearOnSpecificStages;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Вызывается RunLevelManager, когда этаж сменился.
    /// </summary>
    public void OnStageChanged(int stage)
    {
        if (shopKeeperNPC == null)
            return;

        bool shouldAppear = false;

        // 1 — появляется на базе
        if (stage == 0 && appearOnBase)
            shouldAppear = true;

        // 2 — появляется на конкретных этажах
        if (!shouldAppear && appearOnSpecificStages != null)
        {
            foreach (int st in appearOnSpecificStages)
            {
                if (stage == st)
                {
                    shouldAppear = true;
                    break;
                }
            }
        }

        // В будущем:
        // 3 — появление после победы на этапе → RunLevelManager вызовет специальный метод
        //    ShopKeeperManager.Instance.OnStageCleared(stage);

        shopKeeperNPC.SetActive(shouldAppear);
    }

    /// <summary>
    /// Спавн NPC сразу после победы (например, “комната награды”).
    /// </summary>
    public void OnStageCleared(int stage)
    {
        if (!appearAfterStageClear)
            return;

        if (shopKeeperNPC != null)
            shopKeeperNPC.SetActive(true);
    }
}
