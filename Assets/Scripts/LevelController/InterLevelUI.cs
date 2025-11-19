using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class InterLevelUI : MonoBehaviour
{
    [Tooltip("Текст, где показываем 0 -> I -> ( II ) -> III.")]
    public TextMeshProUGUI progressText;

    private void Awake()
    {
        if (progressText == null)
            progressText = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// currentStage: 0..totalForestStages
    /// 0 = база, 1..N = этажи леса
    /// totalForestStages = количество этажей леса (без базы).
    /// </summary>
    public void SetProgress(int currentStage, int totalForestStages)
    {
        if (progressText == null) return;

        progressText.text = BuildProgressLine(currentStage, totalForestStages);
    }

    private string BuildProgressLine(int currentStage, int totalForestStages)
    {
        string[] romans = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        // totalForestStages = сколько этажей леса мы реально показываем
        totalForestStages = Mathf.Clamp(totalForestStages, 1, romans.Length);
        // currentStage от 0 (база) до totalForestStages (последний этаж леса)
        currentStage = Mathf.Clamp(currentStage, 0, totalForestStages);

        var parts = new List<string>();

        // --- БАЗА (0) ---
        if (currentStage == 0)
            parts.Add("( 0 )");
        else
            parts.Add("0");

        // --- ЭТАЖИ ЛЕСА (I..N) ---
        for (int forestStage = 1; forestStage <= totalForestStages; forestStage++)
        {
            string label = romans[forestStage - 1];

            if (forestStage == currentStage)
                parts.Add($"( {label} )");
            else
                parts.Add(label);
        }

        // Склеиваем "0 -> I -> II -> III"
        var sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
                sb.Append("  ->  ");

            sb.Append(parts[i]);
        }

        return sb.ToString();
    }
}
