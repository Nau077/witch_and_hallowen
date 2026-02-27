using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
public class RunSnapshotBootstrap : MonoBehaviour
{
    private const string BOOT_MODE_KEY = "dw_boot_mode";
    private const int BOOT_CONTINUE = 2;

    private const float DEFAULT_SAVE_INTERVAL = 8f;
    private const float APPLY_TIMEOUT = 6f;

    private static RunSnapshotBootstrap _instance;
    private bool _applyingSnapshot;
    private float _nextSaveTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (_instance != null) return;

        var go = new GameObject(nameof(RunSnapshotBootstrap));
        _instance = go.AddComponent<RunSnapshotBootstrap>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        _nextSaveTime = Time.unscaledTime + DEFAULT_SAVE_INTERVAL;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (_applyingSnapshot) return;

        if (Time.unscaledTime >= _nextSaveTime)
        {
            SaveSnapshotIfPossible();
            _nextSaveTime = Time.unscaledTime + DEFAULT_SAVE_INTERVAL;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            SaveSnapshotIfPossible();
    }

    private void OnApplicationQuit()
    {
        SaveSnapshotIfPossible();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_applyingSnapshot) return;
        if (!ShouldApplyContinueSnapshot()) return;

        StartCoroutine(ApplyContinueSnapshotWhenReady());
    }

    private bool ShouldApplyContinueSnapshot()
    {
        if (PlayerPrefs.GetInt(BOOT_MODE_KEY, 0) != BOOT_CONTINUE)
            return false;

        return RunSaveSystem.HasSnapshot();
    }

    private IEnumerator ApplyContinueSnapshotWhenReady()
    {
        if (!RunSaveSystem.TryLoadSnapshot(out var snapshot) || snapshot == null)
        {
            ClearContinueBootFlag();
            yield break;
        }

        _applyingSnapshot = true;

        float timeout = APPLY_TIMEOUT;
        while (timeout > 0f)
        {
            bool ready = RunLevelManager.Instance != null &&
                         PlayerSkills.Instance != null &&
                         SkillLoadout.Instance != null;
            if (ready) break;

            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        bool applied = RunSaveSystem.ApplySnapshot(snapshot);
        if (applied && RunLevelManager.Instance != null)
            RunLevelManager.Instance.SetStageFromSave(snapshot.stage);

        RefreshSkillPanels();
        ClearContinueBootFlag();
        _applyingSnapshot = false;
    }

    private void SaveSnapshotIfPossible()
    {
        if (_applyingSnapshot) return;
        if (RunLevelManager.Instance == null) return;

        int stage = Mathf.Max(0, RunLevelManager.Instance.CurrentStage);
        RunSaveSystem.SaveSnapshot(stage);
    }

    private static void RefreshSkillPanels()
    {
        var skillPanel = Object.FindObjectOfType<PlayerSkillPerksPanelUI>(true);
        if (skillPanel != null)
            skillPanel.Refresh();
    }

    private static void ClearContinueBootFlag()
    {
        if (PlayerPrefs.GetInt(BOOT_MODE_KEY, 0) == BOOT_CONTINUE)
        {
            PlayerPrefs.DeleteKey(BOOT_MODE_KEY);
            PlayerPrefs.Save();
        }
    }
}
