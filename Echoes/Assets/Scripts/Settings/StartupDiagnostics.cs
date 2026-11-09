using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Informações locais no Player.log para diagnosticar falhas de inicialização de playtests.</summary>
public static class StartupDiagnostics
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Debug.Log($"[Startup] Echoes {Application.version} | Unity {Application.unityVersion} | "
            + $"OS {SystemInfo.operatingSystem} | GPU {SystemInfo.graphicsDeviceName} | "
            + $"API {SystemInfo.graphicsDeviceType} | Driver {SystemInfo.graphicsDeviceVersion} | "
            + $"VRAM {SystemInfo.graphicsMemorySize} MB | Shader level {SystemInfo.graphicsShaderLevel}");
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
        Debug.Log($"[Startup] Cena carregada: {scene.name} ({mode})");
}
