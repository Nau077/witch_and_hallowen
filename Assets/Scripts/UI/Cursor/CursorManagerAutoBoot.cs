using UnityEngine;

public static class CursorManagerAutoBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        // если уже существует — не создаём второй
        if (Object.FindFirstObjectByType<CursorManager>() != null)
            return;

        var go = new GameObject("CursorManager");
        go.AddComponent<CursorManager>();
        Object.DontDestroyOnLoad(go);
    }
}
