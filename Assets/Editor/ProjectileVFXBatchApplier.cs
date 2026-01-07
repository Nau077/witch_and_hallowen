#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ProjectileVFXBatchApplier
{
    // Поменяй путь на свою папку с префабами снарядов
    private const string PrefabsFolder = "Assets/Prefabs";

    [MenuItem("Tools/VFX/Apply Default Line Trail To Prefabs")]
    public static void ApplyDefaultLineTrailToPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder });

        int changed = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);

            try
            {
                // добавляем только если нет
                var comp = prefabRoot.GetComponent<DefaultLineTrail>();
                if (comp == null)
                {
                    comp = prefabRoot.AddComponent<DefaultLineTrail>();
                    comp.autoSetupInEditMode = true;
                    comp.ApplyOrCreate();
                    changed++;
                }
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        Debug.Log($"[VFX] DefaultLineTrail applied. Changed prefabs: {changed}. Folder: {PrefabsFolder}");
    }
}
#endif
