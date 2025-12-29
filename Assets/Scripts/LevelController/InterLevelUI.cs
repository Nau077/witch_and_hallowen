using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class InterLevelUI : MonoBehaviour
{
    [Tooltip("Òåêñò, ãäå ïîêàçûâàåì 0 -> I -> ( II ) -> III.")]
    public TextMeshProUGUI progressText;

    private void Awake()
    {
        if (progressText == null)
            progressText = GetComponent<TextMeshProUGUI>();

        if (progressText == null) return;

        // 1️⃣ Центрирование текста ВНУТРИ области
        progressText.alignment = TextAlignmentOptions.Center;

        // 2️⃣ Отключаем всё, что может ломать геометрию
        progressText.enableAutoSizing = false;
        progressText.wordSpacing = 0;
        progressText.characterSpacing = 0;

        // 3️⃣ Центрируем сам RectTransform
        RectTransform rt = progressText.rectTransform;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// currentStage: 0..totalForestStages
    /// 0 = áàçà, 1..N = ýòàæè ëåñà
    /// totalForestStages = êîëè÷åñòâî ýòàæåé ëåñà (áåç áàçû).
    /// </summary>
    public void SetProgress(int currentStage, int totalForestStages)
    {
        if (progressText == null) return;

        progressText.text = BuildProgressLine(currentStage, totalForestStages);
    }

    private string BuildProgressLine(int currentStage, int totalForestStages)
    {
        string[] romans = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        // totalForestStages = ñêîëüêî ýòàæåé ëåñà ìû ðåàëüíî ïîêàçûâàåì
        totalForestStages = Mathf.Clamp(totalForestStages, 1, romans.Length);
        // currentStage îò 0 (áàçà) äî totalForestStages (ïîñëåäíèé ýòàæ ëåñà)
        currentStage = Mathf.Clamp(currentStage, 0, totalForestStages);

        var parts = new List<string>();

        // --- ÁÀÇÀ (0) ---
        if (currentStage == 0)
            parts.Add("( 0 )");
        else
            parts.Add("0");

        // --- ÝÒÀÆÈ ËÅÑÀ (I..N) ---
        for (int forestStage = 1; forestStage <= totalForestStages; forestStage++)
        {
            string label = romans[forestStage - 1];

            if (forestStage == currentStage)
                parts.Add($"( {label} )");
            else
                parts.Add(label);
        }

        // Ñêëåèâàåì "0 -> I -> II -> III"
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
