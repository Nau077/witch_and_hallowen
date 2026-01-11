using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "game_save_v1";
    private const string HAS_SAVE_KEY = "has_save_v1";

    [Header("Auto save")]
    public bool autoSaveOnPause = true;
    public bool autoSaveOnQuit = true;

    public bool HasSave => PlayerPrefs.GetInt(HAS_SAVE_KEY, 0) == 1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationPause(bool pause)
    {
        if (!autoSaveOnPause) return;
        if (pause) Save();
    }

    private void OnApplicationQuit()
    {
        if (!autoSaveOnQuit) return;
        Save();
    }

    // ---------------- PUBLIC API ----------------

    public void Save()
    {
        var data = Capture();
        data.unixTimeUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.SetInt(HAS_SAVE_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("[SaveManager] Saved. stage=" + data.currentStage + " souls=" + data.souls + " coins=" + data.coins);
    }

    public bool TryLoadAndApply()
    {
        if (!HasSave) return false;

        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json)) return false;

        GameSaveData data;
        try
        {
            data = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveManager] Load failed (json parse). " + e.Message);
            return false;
        }

        Apply(data);
        Debug.Log("[SaveManager] Loaded & applied.");
        return true;
    }

    public GameSaveData LoadRaw()
    {
        if (!HasSave) return null;
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json)) return null;
        return JsonUtility.FromJson<GameSaveData>(json);
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.SetInt(HAS_SAVE_KEY, 0);
        PlayerPrefs.Save();

        Debug.Log("[SaveManager] Save cleared.");
    }

    // ---------------- CAPTURE ----------------

    public GameSaveData Capture()
    {
        var data = new GameSaveData();

        // stage
        var run = RunLevelManager.Instance;
        if (run != null)
            data.currentStage = run.CurrentStage;

        // souls
        if (SoulCounter.Instance != null)
            data.souls = SoulCounter.Instance.Souls;

        // coins
        if (PlayerWallet.Instance != null)
            data.coins = PlayerWallet.Instance.coins;

        // perks (мы читаем из SoulPerksManager)
        if (SoulPerksManager.Instance != null)
        {
            data.perkHpLevel = SoulPerksManager.Instance.HpLevel;
            data.perkSoulsSpent = SoulPerksManager.Instance.SoulsSpent;
        }
        else
        {
            // fallback: если менеджера нет в сцене, но prefs уже есть
            data.perkHpLevel = PlayerPrefs.GetInt("perk_hp_level", 0);
            data.perkSoulsSpent = PlayerPrefs.GetInt("perk_souls_spent", 0);
        }

        // skills / levels / charges
        data.skills = CaptureSkills();

        return data;
    }

    private GameSaveData.SkillSaveEntry[] CaptureSkills()
    {
        var list = new List<GameSaveData.SkillSaveEntry>();

        // 1) PlayerSkills (unlock/level)
        if (PlayerSkills.Instance != null)
        {
            foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
            {
                if (id == SkillId.None) continue;

                int lvl = PlayerSkills.Instance.GetSkillLevel(id);
                bool unlocked = PlayerSkills.Instance.IsSkillUnlocked(id);

                // 2) charges: пробуем из SkillLoadout, если есть
                int charges = 0;
                if (SkillLoadout.Instance != null)
                    charges = SkillLoadout.Instance.GetChargesForSkill(id);

                // сохраняем только полезное (чтобы json не раздувался)
                if (unlocked || lvl > 0 || charges > 0)
                {
                    list.Add(new GameSaveData.SkillSaveEntry
                    {
                        skillId = (int)id,
                        unlocked = unlocked,
                        level = lvl,
                        charges = charges
                    });
                }
            }
        }
        else
        {
            // Если PlayerSkills отсутствует — хотя бы сохраним charges из SkillLoadout (если есть)
            if (SkillLoadout.Instance != null)
            {
                foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
                {
                    if (id == SkillId.None) continue;
                    int charges = SkillLoadout.Instance.GetChargesForSkill(id);
                    if (charges > 0)
                    {
                        list.Add(new GameSaveData.SkillSaveEntry
                        {
                            skillId = (int)id,
                            unlocked = true,
                            level = 1,
                            charges = charges
                        });
                    }
                }
            }
        }

        return list.ToArray();
    }

    // ---------------- APPLY ----------------

    public void Apply(GameSaveData data)
    {
        if (data == null) return;

        // 1) souls (перманент)
        if (SoulCounter.Instance != null)
            SoulCounter.Instance.SetSouls(data.souls);
        else
        {
            // fallback — если SoulCounter ещё не загружен, сохраним в prefs под его ключ
            PlayerPrefs.SetInt("kills_lifetime", data.souls);
        }

        // 2) coins (ран)
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.SetCoins(data.coins);

        // 3) perks (перманент — уже и так живут в prefs, но мы приводим к данным сейва)
        PlayerPrefs.SetInt("perk_hp_level", Mathf.Max(0, data.perkHpLevel));
        PlayerPrefs.SetInt("perk_souls_spent", Mathf.Max(0, data.perkSoulsSpent));
        PlayerPrefs.Save();

        // применить к игроку, если возможно
        SoulPerksManager.Instance?.Load();
        SoulPerksManager.Instance?.ApplyToPlayerIfPossible();

        // 4) stage — обычно stage применяют при запуске сцены/рана.
        // Здесь мы только выставим в RunLevelManager, если есть метод.
        var run = RunLevelManager.Instance;
        if (run != null)
        {
            run.SetStageFromSave(data.currentStage); // <-- добавим метод ниже (в RunLevelManager)
        }

        // 5) skills
        ApplySkills(data.skills);

        // 6) обновить UI витрин
        SoulCounter.Instance?.RefreshUI();
        PlayerWallet.Instance?.RefreshUI();
    }

    private void ApplySkills(GameSaveData.SkillSaveEntry[] skills)
    {
        if (skills == null) return;

        // skills unlock/level
        if (PlayerSkills.Instance != null)
        {
            foreach (var e in skills)
            {
                var id = (SkillId)e.skillId;
                if (id == SkillId.None) continue;

                if (e.unlocked)
                    PlayerSkills.Instance.UnlockSkill(id, Mathf.Max(1, e.level));

                if (e.level > 0)
                    PlayerSkills.Instance.SetSkillLevel(id, e.level);
            }
        }

        // charges
        if (SkillLoadout.Instance != null)
        {
            foreach (var e in skills)
            {
                var id = (SkillId)e.skillId;
                if (id == SkillId.None) continue;

                if (e.charges > 0)
                    SkillLoadout.Instance.SetChargesForSkill(id, e.charges);
                else
                    SkillLoadout.Instance.SetChargesForSkill(id, 0);
            }
        }
    }
}
