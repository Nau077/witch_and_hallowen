using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Рисует справа иконки перманентных перков (пока — только HP палочки 0..4).
/// </summary>
public class SoulPerksPanelUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Контейнер, куда спавним палочки (должен иметь VerticalLayoutGroup).")]
    public Transform content;

    [Tooltip("Префаб одной иконки (Image). Можно просто GameObject с Image.")]
    public GameObject iconPrefab;

    [Header("Icons")]
    [Tooltip("Спрайт красной вертикальной палочки для HP.")]
    public Sprite hpStickSprite;

    private readonly List<GameObject> spawned = new();

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        Clear();

        var perks = SoulPerksManager.Instance;
        if (perks == null) return;

        int hpLevel = perks.HpLevel; // 0..4

        for (int i = 0; i < hpLevel; i++)
        {
            SpawnIcon(hpStickSprite);
        }
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
        }
    }

    private void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();
    }
}
