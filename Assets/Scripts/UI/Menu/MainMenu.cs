using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private const string BOOT_MODE_KEY = "dw_boot_mode";
    private const int BOOT_NEW_GAME = 1;
    private const int BOOT_CONTINUE = 2;

    [Header("Scenes")]
    public string newGameStartScene = "Level_1";

    [Header("UI")]
    public Button newGameButton;
    public Button continueButton;
    public Button exitButton; // <- добавили, чтобы тоже можно было гарантировать видимость

    private void Start()
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        // Считаем "есть сохранение" максимально надежно для твоего проекта
        bool hasAnySave =
            SaveSystem.HasSave()
            || (SaveManager.Instance != null && SaveManager.Instance.HasSave)
            || PlayerPrefs.GetInt("has_save_v1", 0) == 1;

        // 1) New Game всегда показываем
        if (newGameButton != null)
            newGameButton.gameObject.SetActive(true);

        // 2) Continue показываем ТОЛЬКО если есть сохранение
        if (continueButton != null)
            continueButton.gameObject.SetActive(hasAnySave);

        // 3) Exit всегда показываем
        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    public void NewGame()
    {
        PlayerPrefs.SetInt(BOOT_MODE_KEY, BOOT_NEW_GAME);
        PlayerPrefs.Save();

        SaveSystem.ClearSave();
        if (SaveManager.Instance != null)
            SaveManager.Instance.ClearSave();

        ProgressResetter.ResetAllProgressForNewGame();

        SaveSystem.NewGame(newGameStartScene);
        SceneManager.LoadScene(newGameStartScene);
    }

    public void Continue()
    {
        PlayerPrefs.SetInt(BOOT_MODE_KEY, BOOT_CONTINUE);
        PlayerPrefs.Save();

        string sceneToLoad = SaveSystem.GetLastScene(newGameStartScene);
        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
