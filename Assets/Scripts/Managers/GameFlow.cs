using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    [Header("Bootstrap")]
    [Tooltip("Если включено — при старте GameFlow автоматически грузит главное меню. " +
             "Выключи, если ты часто запускаешь Play из Level_1 в редакторе.")]
    public bool autoLoadMainMenuOnAwake = true;

    [Header("Scene Names")]
    [Tooltip("Имя сцены главного меню (должно быть в Build Settings).")]
    public string mainMenuScene = "MainMenu";

    [Tooltip("Имя промежуточной сцены между уровнями (InterLevel).")]
    public string interLevelScene = "InterLevel";

    [Tooltip("Список сцен именно с уровнями. Порядок = порядок прохождения.")]
    public List<string> levelScenes = new List<string> { "Level_1", "Level_2", "Level_3" };

    [Header("State (runtime)")]
    [Tooltip("Текущий индекс уровня в списке levelScenes (0 = первый уровень).")]
    public int currentLevelIndex = 0;

    private void Awake()
    {
        // Синглтон
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Автозагрузка меню (опционально)
        if (autoLoadMainMenuOnAwake)
        {
            // если мы уже в меню — не дёргаем лишний раз
            if (!string.IsNullOrEmpty(mainMenuScene) &&
                SceneManager.GetActiveScene().name != mainMenuScene)
            {
                LoadMainMenu();
            }
        }
    }

    // -----------------------------
    // ✅ ДОБАВЛЕНО: универсальная загрузка сцены по имени
    // (нужно для MainMenuRunButtons и подобных UI)
    // -----------------------------
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameFlow] LoadSceneByName: sceneName is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // Загрузить главное меню и сбросить прогресс
    public void LoadMainMenu()
    {
        currentLevelIndex = 0;

        if (!string.IsNullOrEmpty(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
        }
        else
        {
            Debug.LogError("[GameFlow] mainMenuScene не задано в инспекторе.");
        }
    }

    // Начать новую игру (с первого уровня)
    public void StartNewGame()
    {
        currentLevelIndex = 0;
        LoadCurrentLevel();
    }

    // Загрузить текущий уровень по имени из levelScenes[currentLevelIndex]
    public void LoadCurrentLevel()
    {
        if (levelScenes == null || levelScenes.Count == 0)
        {
            Debug.LogError("[GameFlow] Список levelScenes пуст. Заполни его в инспекторе.");
            return;
        }

        if (currentLevelIndex < 0 || currentLevelIndex >= levelScenes.Count)
        {
            Debug.LogError("[GameFlow] currentLevelIndex вне диапазона. index = " + currentLevelIndex);
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levelScenes.Count - 1);
        }

        string sceneName = levelScenes[currentLevelIndex];
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("[GameFlow] Пустое имя сцены уровня в levelScenes[" + currentLevelIndex + "].");
        }
    }

    // Вызываем, когда уровень пройден (например, все враги умерли)
    public void OnLevelCompleted()
    {
        if (currentLevelIndex < levelScenes.Count - 1)
        {
            // Ещё есть уровни → идём на промежуточную сцену
            if (!string.IsNullOrEmpty(interLevelScene))
            {
                SceneManager.LoadScene(interLevelScene);
            }
            else
            {
                Debug.LogWarning("[GameFlow] interLevelScene не задана, сразу грузим следующий уровень.");
                ProceedFromInterLevel();
            }
        }
        else
        {
            // Последний уровень пройден → возвращаемся в главное меню
            LoadMainMenu();
        }
    }

    // Вызывается с InterLevel-сцены по кнопке "Next"
    public void ProceedFromInterLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex >= levelScenes.Count)
        {
            // На всякий случай защита
            LoadMainMenu();
            return;
        }

        LoadCurrentLevel();
    }

    // Вызывается, когда игрок умер
    public void OnPlayerDied()
    {
        // Если мы в забеге и есть RunLevelManager — отправляем на базу (нулевой этаж)
        if (RunLevelManager.Instance != null)
        {
            RunLevelManager.Instance.ReturnToBaseAfterDeath();
            return;
        }

        // Fallback: старое поведение (если вдруг сцена без RunLevelManager)
        currentLevelIndex = 0;
        LoadCurrentLevel();

        // Если ты хочешь рестарт именно текущего уровня без сброса индекса:
        // LoadCurrentLevel();
    }

    // Закрыть игру
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
