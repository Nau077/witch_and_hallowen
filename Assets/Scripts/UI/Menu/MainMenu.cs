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

    void Start()
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(SaveSystem.HasSave());
    }

    public void NewGame()
    {
        // 1) Ставим флаг "это новый забег"
        PlayerPrefs.SetInt(BOOT_MODE_KEY, BOOT_NEW_GAME);
        PlayerPrefs.Save();

        // 2) Чистим старый сейв (чтобы Continue исчезал, если хочешь)
        SaveSystem.ClearSave();

        // 3) Сбрасываем прогресс (души/перки/last-run coins и т.п.)
        ProgressResetter.ResetAllProgressForNewGame();

        // 4) Стартуем сцену
        SaveSystem.NewGame(newGameStartScene);
        SceneManager.LoadScene(newGameStartScene);
    }

    public void Continue()
    {
        // Ставим флаг "continue" — на будущее
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
