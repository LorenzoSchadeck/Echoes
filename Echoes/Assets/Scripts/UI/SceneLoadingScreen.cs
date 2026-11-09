using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// A tela de carregamento em si (veja <see cref="SceneTransition"/>): véu preto, carimbo girando e a
/// palavra "Carregando". Toda a interface é montada aqui, em UI Toolkit, por cima de tudo.
/// Usa tempo não escalado: funciona mesmo com o jogo pausado.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SceneLoadingScreen : MonoBehaviour
{
    [SerializeField] private Texture2D stamp;
    [Tooltip("Fonte da palavra \"Carregando\".")]
    [SerializeField] private Font labelFont;
    [SerializeField] private float fadeInTime = 0.6f;
    [Tooltip("Tempo mínimo com o carimbo girando, para a tela não piscar quando a cena carrega rápido.")]
    [SerializeField] private float minimumShowTime = 1.6f;
    [SerializeField] private float fadeOutTime = 0.8f;
    [SerializeField] private float spinDegreesPerSecond = 100f;
    [Tooltip("Tamanho do carimbo, em pixels da tela de referência (1920x1080).")]
    [SerializeField] private float stampSize = 80f;
    [Tooltip("Segundos máximos esperando o checkpoint ser aplicado na cena nova.")]
    [SerializeField] private float restoreTimeout = 10f;

    private VisualElement veil;
    private VisualElement stampElement;
    private Label label;
    private float spin;
    private float shownAt;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        root.Clear();
        root.style.position = Position.Absolute;
        root.style.left = 0; root.style.top = 0; root.style.right = 0; root.style.bottom = 0;
        root.pickingMode = PickingMode.Ignore;

        // O véu também engole cliques: o menu por baixo não reage durante o carregamento
        veil = new VisualElement { pickingMode = PickingMode.Position };
        veil.style.position = Position.Absolute;
        veil.style.left = 0; veil.style.top = 0; veil.style.right = 0; veil.style.bottom = 0;
        veil.style.backgroundColor = Color.black;
        veil.style.opacity = 0f;
        root.Add(veil);

        stampElement = new VisualElement { pickingMode = PickingMode.Ignore };
        stampElement.style.position = Position.Absolute;
        stampElement.style.right = 48f;
        stampElement.style.bottom = 72f;
        stampElement.style.width = stampSize;
        stampElement.style.height = stampSize;
        if (stamp != null) stampElement.style.backgroundImage = new StyleBackground(stamp);
        stampElement.style.opacity = 0f;
        veil.Add(stampElement);

        label = new Label(Localized());
        label.pickingMode = PickingMode.Ignore;
        label.style.position = Position.Absolute;
        label.style.right = 32f;
        label.style.bottom = 32f;
        label.style.unityTextAlign = TextAnchor.LowerRight;
        label.style.fontSize = 16;
        label.style.letterSpacing = 2;
        label.style.color = new Color(0.91f, 0.886f, 0.839f, 0.75f);
        if (labelFont != null) label.style.unityFontDefinition = FontDefinition.FromFont(labelFont);
        label.style.opacity = 0f;
        veil.Add(label);
    }

    private static string Localized()
    {
        string text = MenuText.Get("M_LOADING");
        return string.IsNullOrEmpty(text) || text.StartsWith("No translation") ? "CARREGANDO" : text;
    }

    public void Begin(string sceneName) => StartCoroutine(Run(sceneName));

    private void Update()
    {
        if (stampElement == null) return;
        spin = (spin + spinDegreesPerSecond * Time.unscaledDeltaTime) % 360f;
        stampElement.style.rotate = new Rotate(new Angle(spin, AngleUnit.Degree));
    }

    private IEnumerator Run(string sceneName)
    {
        // 1. Escurece, e o carimbo aparece
        yield return Fade(veil, 1f, fadeInTime);
        shownAt = Time.unscaledTime;
        StartCoroutine(Fade(stampElement, 0.9f, 0.4f));
        StartCoroutine(Fade(label, 1f, 0.4f));

        // 2. Carrega em segundo plano; só ativa a cena depois do tempo mínimo
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation != null)
        {
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f || Time.unscaledTime - shownAt < minimumShowTime) yield return null;
            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;
        }

        // 3. Cena nova no ar: espera o checkpoint ser aplicado (a restauração começa no primeiro Start)
        yield return null;
        yield return null;
        for (float waited = 0f; CheckpointManager.WillRestore && waited < restoreTimeout; waited += Time.unscaledDeltaTime) yield return null;

        // 4. Carimbo some e a tela clareia
        StartCoroutine(Fade(stampElement, 0f, fadeOutTime * 0.6f));
        StartCoroutine(Fade(label, 0f, fadeOutTime * 0.6f));
        veil.pickingMode = PickingMode.Ignore;
        yield return Fade(veil, 0f, fadeOutTime);

        SceneTransition.IsBusy = false;
        Destroy(gameObject);
    }

    private static IEnumerator Fade(VisualElement element, float to, float duration)
    {
        // O valor inline vale mesmo antes do primeiro layout
        StyleFloat inline = element.style.opacity;
        float from = inline.keyword == StyleKeyword.Undefined ? element.resolvedStyle.opacity : inline.value;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            element.style.opacity = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        element.style.opacity = to;
    }

    private void OnDestroy()
    {
        // Se alguém destruir a tela no meio, não deixa o jogo achando que ainda está carregando
        SceneTransition.IsBusy = false;
    }
}
