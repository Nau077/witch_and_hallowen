using UnityEngine;

public static class SaveSystem
{
    private const string KEY_HAS_SAVE = "has_save";
    private const string KEY_LAST_SCENE = "last_scene";
    private const string KEY_RUN_LEVEL = "run_level"; // пример прогресса

    public static bool HasSave()
    {
        return PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;
    }

    public static void NewGame(string startSceneName)
    {
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
        PlayerPrefs.SetString(KEY_LAST_SCENE, startSceneName);
        PlayerPrefs.SetInt(KEY_RUN_LEVEL, 1);
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
        PlayerPrefs.Save();
    }
}
