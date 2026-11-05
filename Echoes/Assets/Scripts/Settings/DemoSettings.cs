using UnityEngine;

/// <summary>
/// Liga o modo demo: a mesma cena do capítulo, com o conteúdo do capítulo completo escondido
/// (faixa 3, puzzle do rádio, carta do assassino) e o cartão final depois da primeira hora perdida.
/// O asset fica em Resources/DemoSettings; desmarque "Is Demo" para o capítulo completo.
/// </summary>
[CreateAssetMenu(fileName = "DemoSettings", menuName = "Echoes/Demo Settings")]
public class DemoSettings : ScriptableObject
{
    [SerializeField] private bool isDemo = true;

    private static DemoSettings instance;
    private static bool loaded;

    public static bool IsDemo
    {
        get
        {
            if (!loaded)
            {
                instance = Resources.Load<DemoSettings>("DemoSettings");
                loaded = true;
            }
            return instance != null && instance.isDemo;
        }
    }

    // Com o domain reload desligado, recarrega o asset a cada play mode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => loaded = false;
}
