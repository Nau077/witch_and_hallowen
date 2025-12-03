using UnityEngine;

/// <summary>
/// Отвечает за появление / скрытие NPC-продавца на базе и на этажах леса.
/// </summary>
public class ShopKeeperManager : MonoBehaviour
{
    public static ShopKeeperManager Instance { get; private set; }

    [Header("NPC reference")]
    [Tooltip("Сам объект с SoulShopKeeper (ведьмочка-продавец).")]
    public GameObject shopKeeperNPC;

    [Header("Spawn rules")]
    [Tooltip("Появляется ли NPC на базе (stage = 0).")]
    public bool appearOnBase = true;

    [Tooltip("Появляется ли NPC сразу после победы на этапе (для комнат награды).")]
    public bool appearAfterStageClear = false;

    [Tooltip("Этажи, где NPC должен появляться, даже если это не база.")]
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
    /// Вызывается RunLevelManager, когда активный этаж ИЗМЕНИЛСЯ
    /// (перешли на базу, зашли глубже и т.п.).
    /// </summary>
    public void OnStageChanged(int stage)
    {
        if (shopKeeperNPC == null)
            return;

        bool shouldAppear = false;

        // 1) база
        if (stage == 0 && appearOnBase)
            shouldAppear = true;

        // 2) конкретные этажи
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

        shopKeeperNPC.SetActive(shouldAppear);
    }

    /// <summary>
    /// Вызывается RunLevelManager, когда этаж был ОЧИЩЕН (победа).
    /// Можно использовать, чтобы заспавнить продавца
    /// в "комнате награды" после волны.
    /// </summary>
    public void OnStageCleared(int stage)
    {
        if (!appearAfterStageClear)
            return;

        if (shopKeeperNPC != null)
            shopKeeperNPC.SetActive(true);
    }
}
