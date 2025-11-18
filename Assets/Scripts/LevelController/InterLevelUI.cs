using UnityEngine;
using TMPro;
using System.Text;

public class InterLevelUI : MonoBehaviour
{
    [Tooltip("“екст, где показываем I -> II -> ( III ) -> IV.")]
    public TextMeshProUGUI progressText;

    private void Awake()
    {
        if (progressText == null)
            progressText = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// ќбновить строку прогрессии по текущему этапу и общему количеству.
    /// </summary>
    public void SetProgress(int currentStage, int totalStages)
    {
        if (progressText == null) return;

        progressText.text = BuildRomanProgressLine(currentStage, totalStages);
    }

    private string BuildRomanProgressLine(int currentStage, int totalStages)
    {
        string[] romans = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        totalStages = Mathf.Clamp(totalStages, 1, romans.Length);
        currentStage = Mathf.Clamp(currentStage, 1, totalStages);

        var sb = new StringBuilder();

        for (int i = 1; i <= totalStages; i++)
        {
            if (i > 1)
                sb.Append("  ->  ");

            string r = romans[i - 1];

            if (i == currentStage)
            {
                sb.Append("( ").Append(r).Append(" )");
            }
            else
            {
                sb.Append(r);
            }
        }

        return sb.ToString();
    }
}
