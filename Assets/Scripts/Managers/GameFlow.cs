using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";
    public string interLevelScene = "InterLevel";
    public List<string> levelScenes = new List<string> { "Level_1", "Level_2", "Level_3" };

    public int currentLevelIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadMainMenu(); // запускаем меню при старте
    }

    public void LoadMainMenu()
    {
        currentLevelIndex = 0;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void StartNewGame()
    {
        currentLevelIndex = 0;
        LoadCurrentLevel();
    }

    public void LoadCurrentLevel()
    {
        SceneManager.LoadScene(levelScenes[currentLevelIndex]);
    }

    public void OnLevelCompleted()
    {
        if (currentLevelIndex < levelScenes.Count - 1)
        {
            SceneManager.LoadScene(interLevelScene);
        }
        else
        {
            LoadMainMenu(); // после последнего уровня вернуться в меню
        }
    }

    public void ProceedFromInterLevel()
    {
        currentLevelIndex++;
        LoadCurrentLevel();
    }

    public void OnPlayerDied()
    {
        currentLevelIndex = 0;
        LoadCurrentLevel();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
