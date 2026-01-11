using UnityEngine;
using UnityEngine.UI;

public class MainMenuRunButtons : MonoBehaviour
{
    [Header("Buttons")]
    public Button continueButton;
    public Button newGameButton;

    [Header("Scene names")]
    [Tooltip("Сцена, где начинается ран (например Level_1 или Base).")]
    public string runSceneName = "Level_1";

    private void Start()
    {
        bool hasSave = false;

        // Если SaveManager есть и ты реально используешь HasSave — показываем Continue.
        if (SaveManager.Instance != null)
            hasSave = SaveManager.Instance.HasSave;
        else
            hasSave = PlayerPrefs.GetInt("has_save_v1", 0) == 1;

        if (continueButton != null)
            continueButton.gameObject.SetActive(hasSave);

        if (newGameButton != null)
            newGameButton.gameObject.SetActive(true);

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinue);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGame);
        }
    }

    private void OnContinue()
    {
        // Пока просто грузим сцену рана.
        // Позже: SaveManager.TryLoadAndApply() после загрузки сцены.
        if (GameFlow.Instance != null) GameFlow.Instance.LoadSceneByName(runSceneName);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(runSceneName);
    }

    private void OnNewGame()
    {
        ProgressResetter.ResetAllProgressForNewGame();

        if (GameFlow.Instance != null) GameFlow.Instance.LoadSceneByName(runSceneName);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(runSceneName);
    }
}
