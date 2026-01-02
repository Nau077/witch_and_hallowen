using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SoulPerksPanelUI : MonoBehaviour
{
    [Header("UI")]
    public Transform content;
    public GameObject iconPrefab;

    [Header("Icons")]
    public Sprite hpStickSprite;

    [Header("Layout")]
    [Tooltip("Фиксированный верхний отступ в контейнере сердечек.")]
    public int topPadding = 36;

    [Tooltip("Если true — выставляет padding при включении/refresh (без ForceUpdateCanvases).")]
    public bool ensurePadding = true;

    private VerticalLayoutGroup vlg;

    // пулл (НЕ Destroy/Instantiate каждый раз)
    private readonly List<GameObject> pool = new();

    private void Awake()
    {
        if (content != null)
            vlg = content.GetComponent<VerticalLayoutGroup>();
    }

    private void OnEnable()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (SoulPerksManager.Instance != null)
            SoulPerksManager.Instance.OnPerksChanged -= Refresh;
    }

    public void Refresh()
    {
        if (ensurePadding)
            EnsurePaddingOnce();

        var perks = SoulPerksManager.Instance;
        int totalHearts = (perks == null) ? 0 : (1 + perks.HpLevel);

        EnsurePoolSize(totalHearts);

        // включаем нужные
        for (int i = 0; i < totalHearts; i++)
        {
            var go = pool[i];
            if (!go.activeSelf) go.SetActive(true);

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = hpStickSprite;
                img.preserveAspect = true;
                img.enabled = (hpStickSprite != null);
            }
        }

        // выключаем лишние
        for (int i = totalHearts; i < pool.Count; i++)
        {
            var go = pool[i];
            if (go != null && go.activeSelf) go.SetActive(false);
        }

        // ВАЖНО:
        // НИКАКИХ Canvas.ForceUpdateCanvases и LayoutRebuilder.ForceRebuildLayoutImmediate
        // Unity сам пересчитает layout на следующем кадре.
    }

    private void EnsurePaddingOnce()
    {
        if (vlg == null && content != null)
            vlg = content.GetComponent<VerticalLayoutGroup>();

        if (vlg == null) return;

        var p = vlg.padding;
        if (p.top != topPadding)
        {
            p.top = topPadding;
            vlg.padding = p;
        }
    }

    private void EnsurePoolSize(int need)
    {
        if (content == null || iconPrefab == null) return;
        if (need <= pool.Count) return;

        int toAdd = need - pool.Count;
        for (int i = 0; i < toAdd; i++)
        {
            var go = Instantiate(iconPrefab, content);
            pool.Add(go);
        }
    }
}
