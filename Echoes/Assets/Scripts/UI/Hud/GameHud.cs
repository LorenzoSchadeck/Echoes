using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD do jogo em UI Toolkit: mira, prompt de interação, legendas, painel de inspeção,
/// tela preta dos fades e texto final. Os scripts usam as peças estáticas abaixo
/// (por exemplo <c>GameHud.Subtitle.text = "..."</c>) sem guardar referência a objetos de UI.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GameHud : MonoBehaviour
{
    private const float SubtitleFontSize = 30f;

    public static readonly HudElement Crosshair = new HudElement(true);
    public static readonly HudElement Prompt = new HudElement(false);
    public static readonly HudElement Speaker = new HudElement(false, SubtitleFontSize);
    public static readonly HudElement Subtitle = new HudElement(false, SubtitleFontSize);
    public static readonly HudElement Inspect = new HudElement(false);
    public static readonly HudElement InspectTitle = new HudElement(true);
    public static readonly HudElement InspectBody = new HudElement(true);
    /// <summary>Tela preta por cima do jogo (morte, apagões, final). alpha 0 = transparente.</summary>
    public static readonly HudElement FadeScreen = new HudElement(true) { alpha = 0f };
    /// <summary>Clarão branco por cima de tudo (flash da câmera, relâmpago). alpha 0 = transparente.</summary>
    public static readonly HudElement FlashScreen = new HudElement(true) { alpha = 0f };
    public static readonly HudElement EndText = new HudElement(true) { alpha = 0f };
    /// <summary>Aviso de "segure para pular" no canto (abertura).</summary>
    public static readonly HudElement SkipHint = new HudElement(false);

    private void OnEnable()
    {
        // Cena nova (inclusive recarregada num checkpoint): nada de texto sobrando da anterior.
        // A tela preta fica como está, porque o DeathManager decide se a cena começa escura.
        ClearTransient();

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        Crosshair.Bind(root.Q("crosshair"));
        Prompt.Bind(root.Q<Label>("prompt"));
        Speaker.Bind(root.Q<Label>("speaker"));
        Subtitle.Bind(root.Q<Label>("subtitle"));
        Inspect.Bind(root.Q("inspect"));
        InspectTitle.Bind(root.Q<Label>("inspect-title"));
        InspectBody.Bind(root.Q<Label>("inspect-body"));
        FadeScreen.Bind(root.Q("fade"));
        FlashScreen.Bind(root.Q("flash"));
        EndText.Bind(root.Q<Label>("end-text"));
        SkipHint.Bind(root.Q<Label>("skip-hint"));

        ApplySubtitleScale();
        GameOptions.Changed += ApplySubtitleScale;
    }

    private void OnDisable()
    {
        GameOptions.Changed -= ApplySubtitleScale;
        foreach (HudElement element in All) element.Bind(null);
    }

    private static HudElement[] All => new[] { Crosshair, Prompt, Speaker, Subtitle, Inspect, InspectTitle, InspectBody, FadeScreen, FlashScreen, EndText, SkipHint };

    private static void ApplySubtitleScale()
    {
        float scale = GameOptions.Current.subtitleScale;
        Subtitle.FontScale = scale;
        Speaker.FontScale = scale;
        Subtitle.Suppressed = !GameOptions.Current.subtitles;
        Speaker.Suppressed = !GameOptions.Current.subtitles;
    }

    // Com o domain reload desligado, os estáticos sobrevivem entre sessões de play mode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        ClearTransient();
        Crosshair.enabled = true;
        FadeScreen.alpha = 0f;
    }

    private static void ClearTransient()
    {
        Prompt.enabled = false;
        Speaker.enabled = false;
        Subtitle.enabled = false;
        Inspect.enabled = false;
        FlashScreen.alpha = 0f;
        EndText.alpha = 0f;
        SkipHint.enabled = false;
    }
}
