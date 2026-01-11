using UnityEngine;

public static class SaveSystem
{
    private const string KEY_HAS_SAVE = "has_save";
    private const string KEY_LAST_SCENE = "last_scene";
    private const string KEY_RUN_LEVEL = "run_level";

    // 0 = NewGame, 1 = Continue
    private const string KEY_BOOT_MODE = "boot_mode";

    public static bool HasSave()
    {
        // Continue показываем, если есть хоть что-то:
        // старый флаг или реальный снапшот рана
        return PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1 || RunSaveSystem.HasSnapshot();
    }

    public static void MarkBootAsNewGame()
    {
        PlayerPrefs.SetInt(KEY_BOOT_MODE, 0);
        PlayerPrefs.Save();
    }

    public static void MarkBootAsContinue()
    {
        PlayerPrefs.SetInt(KEY_BOOT_MODE, 1);
        PlayerPrefs.Save();
    }

    public static int GetBootMode()
    {
        return PlayerPrefs.GetInt(KEY_BOOT_MODE, 0);
    }

    public static void NewGame(string startSceneName)
    {
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
        PlayerPrefs.SetString(KEY_LAST_SCENE, startSceneName);
        PlayerPrefs.SetInt(KEY_RUN_LEVEL, 1);

        MarkBootAsNewGame();
        PlayerPrefs.Save();
    }

    public static void SaveProgress(string currentSceneName, int runLevel)
    {
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
        PlayerPrefs.SetString(KEY_LAST_SCENE, currentSceneName);
        PlayerPrefs.SetInt(KEY_RUN_LEVEL, runLevel);
        PlayerPrefs.Save();
    }

    public static string GetLastScene(string fallback)
    {
        return PlayerPrefs.GetString(KEY_LAST_SCENE, fallback);
    }

    public static int GetRunLevel()
    {
        return PlayerPrefs.GetInt(KEY_RUN_LEVEL, 1);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(KEY_HAS_SAVE);
        PlayerPrefs.DeleteKey(KEY_LAST_SCENE);
        PlayerPrefs.DeleteKey(KEY_RUN_LEVEL);
        PlayerPrefs.DeleteKey(KEY_BOOT_MODE);
        PlayerPrefs.Save();

        // обязательно чистим снапшот рана
        RunSaveSystem.ClearSnapshot();
    }
}
