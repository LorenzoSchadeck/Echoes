using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Video;

/// <summary>
/// Menu principal (cena Menu, MainMenu.uxml): continuar do último checkpoint, novo jogo,
/// opções e sair, sobre o cenário 3D da cena MenuCenario (ou, sem ele, um vídeo ou uma imagem).
/// Na primeira vez da sessão começa em "Pressione qualquer tecla"; na primeira vez do jogo,
/// passa pela calibração de brilho com o carimbo antes dos botões.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("Cenas")]
    [Tooltip("Cena do \"Novo jogo\": a abertura de 1995, que leva ao apartamento.")]
    [SerializeField] private string gameSceneName = "Abertura1995";

    [Header("Fundo")]
    [Tooltip("Cena 3D carregada atrás do menu (MenuBackdrop). Vazio ou fora do build: usa o vídeo ou a imagem.")]
    [SerializeField] private string backdropSceneName = "MenuCenario";
    [Tooltip("Vídeo em loop do apartamento (MP4 H.264 ou WebM). Tem prioridade sobre a imagem.")]
    [SerializeField] private VideoClip backgroundVideo;
    [Tooltip("Usada quando não há vídeo.")]
    [SerializeField] private Texture2D backgroundImage;

    [Header("Áudio")]
    [SerializeField] private EventReference clickSound;

    private VisualElement root;
    private VisualElement mainPanel;
    private VisualElement menuButtons;
    private Label pressAny;
    private bool waitingForPress;
    private static bool titleShown;
    private Button continueButton;
    private OptionsPanel options;
    private RenderTexture videoTexture;
    private bool starting;

    private void OnEnable()
    {
        // Voltando do jogo: tempo normal e cursor livre
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        root = GetComponent<UIDocument>().rootVisualElement;
        mainPanel = root.Q("main-panel");
        menuButtons = root.Q("menu-buttons");
        pressAny = root.Q<Label>("press-any");
        continueButton = root.Q<Button>("button-continue");

        continueButton.clicked += () => Click(ContinueGame);
        root.Q<Button>("button-new-game").clicked += () => Click(NewGame);
        root.Q<Button>("button-options").clicked += () => Click(ShowOptions);
        root.Q<Button>("button-quit").clicked += () => Click(Quit);

        options = new OptionsPanel(root.Q("options"), ShowMainPanel);
        root.RegisterCallback<NavigationCancelEvent>(_ => { if (options.IsVisible) options.Back(); });

        root.Q<Label>("version").text = $"v{Application.version}";
        MenuDecor.Apply(root);

        if (!LoadBackdrop()) SetupBackground();
        LocalizeTexts();
        MenuFocusStyle.Track(root);
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        if (titleShown) ShowMainPanel();
        else ShowTitle();
    }

    private void Update()
    {
        options.Tick();
        if (waitingForPress && AnyPress()) LeaveTitle();
    }

    // ---------- Tela inicial ----------

    private void ShowTitle()
    {
        waitingForPress = true;
        options.Hide();
        menuButtons.AddToClassList("hidden");
        pressAny.RemoveFromClassList("hidden");
        // Pisca devagar, como uma luz que demora a acender
        pressAny.schedule.Execute(() => pressAny.ToggleInClassList("press-any--dim")).Every(1100);
    }

    private void LeaveTitle()
    {
        waitingForPress = false;
        titleShown = true;
        pressAny.AddToClassList("hidden");
        if (!clickSound.IsNull) RuntimeManager.PlayOneShot(clickSound);

        if (!GameOptions.Current.brightnessCalibrated)
        {
            mainPanel.AddToClassList("hidden");
            options.ShowCalibration(ShowMainPanel);
        }
        else ShowMainPanel();
    }

    private static bool AnyPress()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
        Gamepad pad = Gamepad.current;
        return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
            || pad.buttonWest.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
    }

    private void ShowMainPanel()
    {
        options.Hide();
        root.RemoveFromClassList("options-open");
        mainPanel.RemoveFromClassList("hidden");
        if (MenuBackdrop.Current != null) MenuBackdrop.Current.OptionsOpen = false;

        // Os botões aparecem com um fade curto
        menuButtons.AddToClassList("menu-buttons--intro");
        menuButtons.RemoveFromClassList("hidden");
        menuButtons.schedule.Execute(() => menuButtons.RemoveFromClassList("menu-buttons--intro"));

        continueButton.SetEnabled(SaveSystem.HasSave);
        Button first = SaveSystem.HasSave ? continueButton : root.Q<Button>("button-new-game");
        root.schedule.Execute(() => first.Focus());
    }

    private void ShowOptions()
    {
        mainPanel.AddToClassList("hidden");
        root.AddToClassList("options-open");
        options.Show();
        if (MenuBackdrop.Current != null) MenuBackdrop.Current.OptionsOpen = true;
    }

    private void ContinueGame() => CheckpointManager.LoadLastCheckpoint();

    private void NewGame()
    {
        if (starting) return;
        starting = true;
        SaveSystem.Delete();
        if (MenuBackdrop.Current != null) StartCoroutine(EnterScene());
        else SceneTransition.Load(gameSceneName);
    }

    // A câmera do cenário avança enquanto o menu some e a tela escurece
    private IEnumerator EnterScene()
    {
        const float time = 1.6f;
        mainPanel.SetEnabled(false);
        StartCoroutine(MenuBackdrop.Current.PushIn(time));
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < time)
        {
            float k = (Time.unscaledTime - start) / time;
            mainPanel.style.opacity = 1f - Mathf.Clamp01(k * 2.5f);
            root.style.backgroundColor = new Color(0f, 0f, 0f, k * k);
            yield return null;
        }
        root.style.backgroundColor = Color.black;
        SceneTransition.Load(gameSceneName);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Click(System.Action action)
    {
        if (SceneTransition.IsBusy) return; // já carregando: ignora teclas e cliques
        if (!clickSound.IsNull) RuntimeManager.PlayOneShot(clickSound);
        action();
    }

    // Carrega o cenário 3D por cima da cena Menu: a câmera dele passa a desenhar o fundo e a
    // luz e a neblina vêm dele (cena ativa). Devolve falso quando não há cenário no build.
    private bool LoadBackdrop()
    {
        if (string.IsNullOrEmpty(backdropSceneName) || !Application.CanStreamedLevelBeLoaded(backdropSceneName)) return false;

        root.Q(className: "screen")?.AddToClassList("screen--scene");
        Scene loaded = SceneManager.GetSceneByName(backdropSceneName);
        if (loaded.isLoaded)
        {
            UseBackdrop(loaded);
            return true;
        }

        root.style.backgroundColor = Color.black;
        AsyncOperation operation = SceneManager.LoadSceneAsync(backdropSceneName, LoadSceneMode.Additive);
        operation.completed += _ =>
        {
            if (this == null) return;
            UseBackdrop(SceneManager.GetSceneByName(backdropSceneName));
            StartCoroutine(RevealBackdrop());
        };
        return true;
    }

    private void UseBackdrop(Scene backdrop)
    {
        if (!backdrop.IsValid()) return;
        SceneManager.SetActiveScene(backdrop);
        // A câmera própria da cena Menu só servia para o fundo preto
        foreach (GameObject rootObject in gameObject.scene.GetRootGameObjects())
            foreach (Camera sceneCamera in rootObject.GetComponentsInChildren<Camera>())
                sceneCamera.enabled = false;
    }

    private IEnumerator RevealBackdrop()
    {
        const float time = 1.5f;
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < time && !starting)
        {
            float k = (Time.unscaledTime - start) / time;
            root.style.backgroundColor = new Color(0f, 0f, 0f, 1f - k);
            yield return null;
        }
        if (!starting) root.style.backgroundColor = StyleKeyword.Null;
    }

    private void SetupBackground()
    {
        VisualElement background = root.Q("background");

        if (backgroundVideo != null)
        {
            videoTexture = new RenderTexture((int)backgroundVideo.width, (int)backgroundVideo.height, 0);

            VideoPlayer player = GetComponent<VideoPlayer>();
            if (player == null) player = gameObject.AddComponent<VideoPlayer>();
            player.clip = backgroundVideo;
            player.isLooping = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = videoTexture;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.Play();

            background.style.backgroundImage = Background.FromRenderTexture(videoTexture);
        }
        else if (backgroundImage != null)
        {
            background.style.backgroundImage = new StyleBackground(backgroundImage);
        }
    }

    private void HandleLocaleChanged(Locale _) => LocalizeTexts();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => titleShown = false;

    private void LocalizeTexts()
    {
        MenuText.Localize(root);
        if (options.IsVisible) options.Refresh();
    }
}
