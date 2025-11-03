using UnityEngine;
using TMPro;
using System.Collections;

public class VictoryResultsUI : MonoBehaviour
{
    [Header("Target text (this)")]
    public TMP_Text target;                   // можно не заполн€ть Ч возьмЄм с этого GO

    [Header("Animation")]
    public bool animateCount = true;
    public float countDuration = 0.7f;

    [Header("Format")]
    [TextArea(2, 4)]
    public string format = "VICTORY\nKills: {K}\nCursed Gold: {G}";
    // {K} = kills, {G} = gold

    private int cachedKills;
    private int cachedGold;

    void Awake()
    {
        if (!target) target = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        // 1) фиксируем значени€ и ставим "замок" от сброса
        var sc = SoulCounter.Instance;
        if (sc)
        {
            sc.BeginVictorySequence();                // защитим золото от случайного сброса
            sc.GetVictorySnapshot(out cachedKills, out cachedGold); // снимок
        }
        else
        {
            cachedKills = 0;
            cachedGold = 0;
        }

        // 2) показываем числа
        ShowCached();
    }

    void OnDisable()
    {
        // 3) снимаем замок, когда закрываем панель победы (Continue/Next Level)
        SoulCounter.Instance?.EndVictorySequence();
    }

    private void ShowCached()
    {
        if (!target)
            return;

        if (!animateCount)
        {
            target.text = Format(cachedKills, cachedGold);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(CountIn(cachedKills, cachedGold));
    }

    IEnumerator CountIn(int kills, int gold)
    {
        float t = 0f;
        while (t < countDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0, 1, t / countDuration);
            int ck = Mathf.RoundToInt(Mathf.Lerp(0, kills, k));
            int cg = Mathf.RoundToInt(Mathf.Lerp(0, gold, k));
            target.text = Format(ck, cg);
            yield return null;
        }
        target.text = Format(kills, gold);
    }

    private string Format(int kills, int gold)
    {
        return format.Replace("{K}", kills.ToString())
                     .Replace("{G}", gold.ToString());
    }
}
