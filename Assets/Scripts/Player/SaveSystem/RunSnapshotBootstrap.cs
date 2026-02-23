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

        // Capture scene-default fireball from slot 0 before snapshot apply.
        SkillDefinition defaultSlot0Def = null;
        var preLoadout = SkillLoadout.Instance;
        if (preLoadout != null && preLoadout.slots != null && preLoadout.slots.Length > 0)
            defaultSlot0Def = preLoadout.slots[0]?.def;

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
        if (applied)
            EnforceDefaultFireballSlot(defaultSlot0Def);
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

    private static void EnforceDefaultFireballSlot(SkillDefinition preferredFireball)
    {
        var loadout = SkillLoadout.Instance;
        if (loadout == null || loadout.slots == null || loadout.slots.Length == 0)
            return;

        SkillDefinition fireballDef = ResolveFireball(preferredFireball, loadout);
        if (fireballDef == null) return;

        var slot0 = loadout.slots[0];
        if (slot0 == null)
        {
            slot0 = new SkillSlot();
            loadout.slots[0] = slot0;
        }

        slot0.def = fireballDef;
        slot0.charges = fireballDef.infiniteCharges ? 0 : Mathf.Max(1, slot0.charges);
        slot0.cooldownUntil = 0f;

        // Remove ghost skills with level 0 from other slots.
        for (int i = 1; i < loadout.slots.Length; i++)
        {
            var s = loadout.slots[i];
            if (s == null || s.def == null) continue;

            SkillId id = s.def.skillId;
            if (id == SkillId.Fireball) continue;
            if (PlayerSkills.Instance != null && PlayerSkills.Instance.GetSkillLevel(id) > 0) continue;

            s.def = null;
            s.charges = 0;
            s.cooldownUntil = 0f;
        }

        loadout.SetActiveIndex(0);
        loadout.EnsureValidActive();
    }

    private static SkillDefinition ResolveFireball(SkillDefinition preferred, SkillLoadout loadout)
    {
        if (preferred != null && preferred.skillId == SkillId.Fireball)
            return preferred;

        SkillDefinition lookup = SkillDefinitionLookup.FindById(SkillId.Fireball);
        if (lookup != null) return lookup;

        if (loadout?.slots != null)
        {
            for (int i = 0; i < loadout.slots.Length; i++)
            {
                var s = loadout.slots[i];
                if (s?.def != null && s.def.skillId == SkillId.Fireball)
                    return s.def;
            }
        }

        var loaded = Resources.FindObjectsOfTypeAll<SkillDefinition>();
        if (loaded != null)
        {
            for (int i = 0; i < loaded.Length; i++)
            {
                var d = loaded[i];
                if (d != null && d.skillId == SkillId.Fireball)
                    return d;
            }
        }

        Debug.LogWarning("[RunSnapshotBootstrap] Fireball definition not found on Continue.");
        return null;
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
