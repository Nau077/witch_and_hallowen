using UnityEngine;

public class RuntimePerfProbe : MonoBehaviour
{
    public bool logEverySecond = true;

    float t;
    int frames;

    void Start()
    {
        Debug.Log(
            $"[PerfProbe] Start | " +
            $"targetFrameRate={Application.targetFrameRate} " +
            $"vSync={QualitySettings.vSyncCount} " +
            $"fixedDeltaTime={Time.fixedDeltaTime} " +
            $"maxDeltaTime={Time.maximumDeltaTime} " +
            $"timeScale={Time.timeScale}"
        );

        LogSceneState("Start");
    }

    void Update()
    {
        frames++;
        t += Time.unscaledDeltaTime;

        if (logEverySecond && t >= 1f)
        {
            float fps = frames / t;

            Debug.Log(
                $"[PerfProbe] FPS≈{fps:0.0} | " +
                $"delta={Time.deltaTime:0.0000} unscaled={Time.unscaledDeltaTime:0.0000} | " +
                $"targetFrameRate={Application.targetFrameRate} vSync={QualitySettings.vSyncCount} | " +
                $"fixedDeltaTime={Time.fixedDeltaTime:0.0000} timeScale={Time.timeScale}"
            );

            frames = 0;
            t = 0f;
        }
    }

    [ContextMenu("Log Scene State")]
    public void LogSceneStateContext() => LogSceneState("ContextMenu");

    void LogSceneState(string reason)
    {
        var runs = FindObjectsOfType<RunLevelManager>(true);
        var cameras = FindObjectsOfType<Camera>(true);
        var canvases = FindObjectsOfType<Canvas>(true);

        Debug.Log(
            $"[PerfProbe] SceneState({reason}) | " +
            $"RunLevelManager={runs.Length} Cameras={cameras.Length} Canvases={canvases.Length}"
        );

        // если камер больше 1 — уже подозрительно (если ты не планировал)
        foreach (var cam in cameras)
            Debug.Log($"[PerfProbe] Camera: {cam.name} enabled={cam.enabled} depth={cam.depth}");
    }
}
