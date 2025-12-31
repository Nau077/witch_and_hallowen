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
    public int topPadding = 8;

    [Tooltip("Если true — принудительно выставляет padding на каждом Refresh (лечит сброс в 0).")]
    public bool forcePaddingEachRefresh = true;

    private VerticalLayoutGroup vlg;
    private readonly List<GameObject> spawned = new();

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
        if (forcePaddingEachRefresh)
            EnsurePadding();

        Clear();

        var perks = SoulPerksManager.Instance;
        if (perks == null) { ForceLayoutNow(); return; }

        int totalHearts = 1 + perks.HpLevel;

        for (int i = 0; i < totalHearts; i++)
            SpawnIcon(hpStickSprite);

        ForceLayoutNow();
    }

    private void EnsurePadding()
    {
        if (vlg == null && content != null)
            vlg = content.GetComponent<VerticalLayoutGroup>();

        if (vlg == null) return;

        var p = vlg.padding;
        p.top = topPadding;
        vlg.padding = p;
    }

    private void SpawnIcon(Sprite spr)
    {
        if (content == null || iconPrefab == null) return;

        var go = Instantiate(iconPrefab, content);
        spawned.Add(go);

        var img = go.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = spr;
            img.preserveAspect = true;
            img.enabled = (spr != null);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);

        spawned.Clear();
    }

    private void ForceLayoutNow()
    {
        if (!content) return;

        var rt = content as RectTransform;
        if (!rt) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();
    }
}
