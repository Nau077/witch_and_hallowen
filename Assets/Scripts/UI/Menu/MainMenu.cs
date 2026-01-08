using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
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
        SaveSystem.ClearSave();
        SaveSystem.NewGame(newGameStartScene);
        SceneManager.LoadScene(newGameStartScene);
    }

    public void Continue()
    {
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
