using FMODUnity;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Video;

/// <summary>
/// Menu principal (cena Menu, MainMenu.uxml): continuar do último checkpoint, novo jogo,
/// opções e sair, sobre um vídeo em loop do apartamento.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("Cenas")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Fundo")]
    [Tooltip("Vídeo em loop do apartamento (MP4 H.264 ou WebM). Tem prioridade sobre a imagem.")]
    [SerializeField] private VideoClip backgroundVideo;
    [Tooltip("Usada quando não há vídeo.")]
    [SerializeField] private Texture2D backgroundImage;

    [Header("Áudio")]
    [SerializeField] private EventReference clickSound;

    private VisualElement root;
    private VisualElement mainPanel;
    private Button continueButton;
    private OptionsPanel options;
    private RenderTexture videoTexture;

    private void OnEnable()
    {
        // Voltando do jogo: tempo normal e cursor livre
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        root = GetComponent<UIDocument>().rootVisualElement;
        mainPanel = root.Q("main-panel");
        continueButton = root.Q<Button>("button-continue");

        continueButton.clicked += () => Click(ContinueGame);
        root.Q<Button>("button-new-game").clicked += () => Click(NewGame);
        root.Q<Button>("button-options").clicked += () => Click(ShowOptions);
        root.Q<Button>("button-quit").clicked += () => Click(Quit);

        options = new OptionsPanel(root.Q("options"), ShowMainPanel);
        root.RegisterCallback<NavigationCancelEvent>(_ => { if (options.IsVisible) ShowMainPanel(); });

        root.Q<Label>("version").text = $"v{Application.version}";

        SetupBackground();
        LocalizeTexts();
        MenuFocusStyle.Track(root);
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        ShowMainPanel();
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
        mainPanel.RemoveFromClassList("hidden");

        continueButton.SetEnabled(SaveSystem.HasSave);
        Button first = SaveSystem.HasSave ? continueButton : root.Q<Button>("button-new-game");
        root.schedule.Execute(() => first.Focus());
    }

    private void ShowOptions()
    {
        mainPanel.AddToClassList("hidden");
        options.Show();
    }

    private void ContinueGame() => CheckpointManager.LoadLastCheckpoint();

    private void NewGame()
    {
        SaveSystem.Delete();
        SceneManager.LoadScene(gameSceneName);
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
        if (!clickSound.IsNull) RuntimeManager.PlayOneShot(clickSound);
        action();
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

    private void LocalizeTexts()
    {
        MenuText.Localize(root);
        if (options.IsVisible) options.Refresh();
    }
}
