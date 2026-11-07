using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Troca de cena com tela de carregamento: a tela escurece, o carimbo da delegacia gira enquanto a cena
/// carrega em segundo plano e, depois que a cena nova está pronta (e o checkpoint, se houver, aplicado),
/// a tela clareia. A tela vive num prefab em Resources/Loading e sobrevive à troca de cena.
/// Sem o prefab, cai no carregamento direto de antes.
/// </summary>
public static class SceneTransition
{
    private const string PrefabPath = "Loading/SceneLoadingScreen";

    /// <summary>Uma troca de cena está em andamento (ignore novos pedidos de troca).</summary>
    public static bool IsBusy { get; internal set; }

    /// <summary>
    /// Carrega a cena com a tela de carregamento. Falso se já havia uma troca em andamento.
    /// </summary>
    public static bool Load(string sceneName)
    {
        if (IsBusy) return false;

        var prefab = Resources.Load<SceneLoadingScreen>(PrefabPath);
        if (prefab == null || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return true;
        }

        IsBusy = true;
        SceneLoadingScreen screen = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(screen.gameObject);
        screen.Begin(sceneName);
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => IsBusy = false;
}
