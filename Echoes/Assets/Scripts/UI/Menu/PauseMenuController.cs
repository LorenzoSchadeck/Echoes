using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

/// <summary>
/// Menu de pausa (PauseMenu.uxml) na cena do jogo: Esc ou Start pausam e retomam.
/// Pausar congela o tempo, o áudio e o jogador; voltar ao menu descarta o progresso desde o
/// último checkpoint.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";

    [Tooltip("Controle de olhar do jogador, desligado enquanto o jogo está pausado.")]
    [SerializeField] private CustomInputAxisController lookController;

    public static bool IsPaused { get; private set; }

    private PlayerInputActions inputActions;
    private VisualElement root;
    private VisualElement mainPanel;
    private OptionsPanel options;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Pause.performed += _ => TogglePause();
    }

    private void OnEnable()
    {
        inputActions.Player.Pause.Enable();

        root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = DisplayStyle.None; // começa escondido, antes de qualquer outra configuração
        mainPanel = root.Q("main-panel");

        root.Q<Button>("button-resume").clicked += Resume;
        root.Q<Button>("button-options").clicked += ShowOptions;
        root.Q<Button>("button-main-menu").clicked += ReturnToMainMenu;

        options = new OptionsPanel(root.Q("options"), ShowMainPanel);
        root.RegisterCallback<NavigationCancelEvent>(_ =>
        {
            if (options.IsVisible) options.Back();
            else Resume();
        });

        MenuDecor.Apply(root);
        MenuText.Localize(root);
        MenuFocusStyle.Track(root);
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        inputActions.Player.Pause.Disable();
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (IsPaused) Resume();
    }

    private void Update()
    {
        if (IsPaused) options.Tick();
    }

    private void TogglePause()
    {
        if (IsPaused)
        {
            // Esc dentro das opções volta para a pausa; fora delas, retoma o jogo
            if (options.IsVisible) options.Back();
            else Resume();
        }
        else if (CanPause())
        {
            Pause();
        }
    }

    // Não pausa enquanto um checkpoint carrega, durante um apagão nem com o mural aberto (Esc fecha o mural)
    private static bool CanPause() => !CheckpointManager.IsRestoring && !LostTimeEvent.IsActive && !EvidenceBoardUI.BlocksPause;

    private void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        SetGameAudioPaused(true);
        PlayerControlLock.Acquire(this);
        if (lookController != null) lookController.enabled = false;

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        // Tira a seleção de qualquer elemento uGUI, para teclado e gamepad navegarem na pausa
        EventSystem.current?.SetSelectedGameObject(null);

        root.style.display = DisplayStyle.Flex;
        ShowMainPanel();
    }

    private void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SetGameAudioPaused(false);
        PlayerControlLock.Release(this);
        if (lookController != null) lookController.enabled = true;

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        root.style.display = DisplayStyle.None;
    }

    private void ShowMainPanel()
    {
        options.Hide();
        mainPanel.RemoveFromClassList("hidden");
        root.schedule.Execute(() => root.Q<Button>("button-resume").Focus());
    }

    private void ShowOptions()
    {
        mainPanel.AddToClassList("hidden");
        options.Show();
    }

    private void ReturnToMainMenu()
    {
        if (SceneTransition.IsBusy) return;
        IsPaused = false;
        Time.timeScale = 1f;

        // O FMOD sobrevive à troca de cena: para tudo antes de sair
        if (RuntimeManager.StudioSystem.getBus(GameOptions.MasterBus, out FMOD.Studio.Bus master) == FMOD.RESULT.OK)
        {
            master.setPaused(false);
            master.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        SceneTransition.Load(menuSceneName);
    }

    private static void SetGameAudioPaused(bool paused)
    {
        if (RuntimeManager.StudioSystem.getBus(GameOptions.MasterBus, out FMOD.Studio.Bus master) == FMOD.RESULT.OK)
        {
            master.setPaused(paused);
        }
    }

    private void HandleLocaleChanged(Locale _)
    {
        MenuText.Localize(root);
        if (options.IsVisible) options.Refresh();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => IsPaused = false;
}
