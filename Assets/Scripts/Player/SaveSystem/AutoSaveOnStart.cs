using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSaveOnStart : MonoBehaviour
{
    public int runLevel = 1;

    void Start()
    {
        SaveSystem.SaveProgress(SceneManager.GetActiveScene().name, runLevel);
    }
}
