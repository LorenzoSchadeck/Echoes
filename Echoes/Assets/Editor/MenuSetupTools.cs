using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Video;

/// <summary>
/// Monta o menu em UI Toolkit (menu Echoes > Menu): textos na tabela de localização, PanelSettings,
/// cena Menu, ordem das cenas no build e a troca do menu diegético pela pausa na cena ApartamentoDemo.
/// </summary>
public static class MenuSetupTools
{
    private const string MenuRoot = "Echoes/Menu/";
    private const string UiFolder = "Assets/UI/Menu";
    private const string PanelSettingsPath = UiFolder + "/EchoesPanelSettings.asset";
    private const string ThemePath = UiFolder + "/EchoesTheme.tss";
    private const string MainMenuUxmlPath = UiFolder + "/MainMenu.uxml";
    private const string PreLoadScenePath = "Assets/Scenes/PreLoad.unity";
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string GameScenePath = "Assets/Scenes/ApartamentoDemo.unity";

    // Chave, português, inglês. Entradas que já existem na tabela não são sobrescritas.
    private static readonly (string key, string pt, string en)[] MenuStrings =
    {
        ("M_CHAPTER", "Capítulo 1", "Chapter 1"),
        ("M_PAUSED", "Pausado", "Paused"),
        ("M_MAIN_MENU", "VOLTAR AO MENU", "MAIN MENU"),
        ("M_LEAVE_WARNING", "O progresso desde o último checkpoint será perdido.", "Progress since the last checkpoint will be lost."),
        ("M_TAB_AUDIO", "Áudio", "Audio"),
        ("M_TAB_VIDEO", "Vídeo", "Video"),
        ("M_TAB_CONTROLS", "Controles", "Controls"),
        ("M_TAB_ACCESS", "Acessibilidade", "Accessibility"),
        ("M_MUSIC", "Música", "Music"),
        ("M_SFX", "Efeitos", "Effects"),
        ("M_VOICE", "Vozes", "Voices"),
        ("M_RESOLUTION", "Resolução", "Resolution"),
        ("M_WINDOW", "Modo de janela", "Window mode"),
        ("M_FULLSCREEN", "Tela cheia", "Fullscreen"),
        ("M_BORDERLESS", "Janela sem borda", "Borderless window"),
        ("M_WINDOWED", "Janela", "Windowed"),
        ("M_QUALITY", "Qualidade", "Quality"),
        ("M_FOV", "Campo de visão", "Field of view"),
        ("M_BRIGHTNESS", "Brilho", "Brightness"),
        ("M_INVERT_Y", "Inverter eixo Y", "Invert Y axis"),
        ("M_SUB_SIZE", "Tamanho das legendas", "Subtitle size"),
        ("M_REDUCE_FLASH", "Reduzir flashes", "Reduce flashes"),
        ("M_SHAKE", "Tremor de câmera", "Camera shake"),
        ("M_DEFAULTS", "Restaurar padrões", "Restore defaults"),
        ("M_PRESS_ANY", "Pressione qualquer tecla", "Press any button"),
        ("M_LOADING", "CARREGANDO", "LOADING"),
        ("M_TAB_GAME", "Jogo", "Game"),
        ("M_SEC_SUBTITLES", "Legendas", "Subtitles"),
        ("M_SUBTITLES", "Legendas", "Subtitles"),
        ("M_SEC_DISPLAY", "Tela", "Display"),
        ("M_SEC_IMAGE", "Imagem", "Image"),
        ("M_SEC_VOLUME", "Volume", "Volume"),
        ("M_SEC_MOUSE", "Mouse e controle", "Mouse and controller"),
        ("M_SEC_COMFORT", "Conforto", "Comfort"),
        ("M_ON", "Ligado", "On"),
        ("M_OFF", "Desligado", "Off"),
        ("M_CALIBRATE", "Calibrar brilho", "Calibrate brightness"),
        ("M_CAL_HINT", "Ajuste até o carimbo ficar quase invisível.", "Adjust until the stamp is barely visible."),
        ("M_ADJUST", "Ajustar", "Adjust"),
        ("M_CONFIRM", "Confirmar", "Confirm"),
        ("M_CHANGE", "Alterar", "Change"),
        ("M_D_LANGUAGE", "Idioma dos textos, das legendas e da interface.", "Language for text, subtitles and the interface."),
        ("M_D_SUBTITLES", "Mostra as falas e os pensamentos na tela.", "Shows dialogue and thoughts on screen."),
        ("M_D_SUB_SIZE", "Tamanho do texto das legendas.", "Size of the subtitle text."),
        ("M_D_RESOLUTION", "Resolução da tela do jogo.", "Game screen resolution."),
        ("M_D_WINDOW", "Tela cheia, janela sem borda ou janela.", "Fullscreen, borderless window or windowed."),
        ("M_D_QUALITY", "Qualidade das sombras, da iluminação e dos efeitos. Baixe se o jogo travar.", "Quality of shadows, lighting and effects. Lower it if the game stutters."),
        ("M_D_FOV", "Quanto da cena cabe na tela. Valores maiores ajudam quem sente enjoo.", "How much of the scene fits on screen. Higher values help with motion sickness."),
        ("M_D_BRIGHTNESS", "Brilho da imagem. O jogo foi feito para ser escuro.", "Image brightness. The game is meant to be dark."),
        ("M_D_CALIBRATE", "Ajusta o brilho com uma imagem de referência.", "Sets the brightness using a reference image."),
        ("M_D_MASTER", "Volume de todo o som do jogo.", "Volume of all game sound."),
        ("M_D_MUSIC", "Volume da música.", "Music volume."),
        ("M_D_SFX", "Volume dos efeitos e do ambiente.", "Volume of effects and ambience."),
        ("M_D_VOICE", "Volume das vozes.", "Voice volume."),
        ("M_D_SENSITIVITY", "Velocidade da câmera ao mover o mouse ou o analógico.", "Camera speed when moving the mouse or stick."),
        ("M_D_INVERT_Y", "Inverte o movimento vertical da câmera.", "Inverts the camera's vertical movement."),
        ("M_D_FLASHES", "Suaviza luzes piscando, sirenes e clarões. Recomendado para quem tem fotossensibilidade.", "Softens flickering lights, sirens and flashes. Recommended for photosensitive players."),
        ("M_D_SHAKE", "Intensidade do tremor da câmera em sustos e eventos.", "Strength of camera shake during scares and events."),
    };

    [MenuItem(MenuRoot + "1. Atualizar textos do menu")]
    private static void UpdateMenuStrings()
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("menu");
        if (collection == null)
        {
            Debug.LogError("[MenuSetupTools] Tabela de localização 'menu' não encontrada.");
            return;
        }

        int added = 0;
        foreach (var (key, pt, en) in MenuStrings)
        {
            if (collection.SharedData.GetEntry(key) == null)
            {
                collection.SharedData.AddKey(key);
                added++;
            }

            foreach (StringTable table in collection.StringTables)
            {
                if (table.GetEntry(key) != null) continue;

                table.AddEntry(key, table.LocaleIdentifier.Code.StartsWith("pt") ? pt : en);
                EditorUtility.SetDirty(table);
            }
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MenuSetupTools] Tabela 'menu': {added} chaves novas.");
    }

    [MenuItem(MenuRoot + "2. Criar cena Menu e ordem das cenas")]
    private static void CreateMenuScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        PanelSettings panelSettings = EnsurePanelSettings();

        if (File.Exists(MenuScenePath))
        {
            Debug.LogWarning($"[MenuSetupTools] {MenuScenePath} já existe; não foi recriada.");
        }
        else
        {
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene); // objetos novos nascem na cena Menu

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(FMODUnity.StudioListener));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.tag = "MainCamera";

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var menuObject = new GameObject("MainMenu", typeof(UIDocument), typeof(VideoPlayer), typeof(MainMenuController));
            var document = menuObject.GetComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
            menuObject.GetComponent<VideoPlayer>().playOnAwake = false;

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            if (previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"[MenuSetupTools] Cena criada: {MenuScenePath}");
        }

        SetBuildOrder();
        PointPreLoadToMenu();
    }

    private static PanelSettings EnsurePanelSettings()
    {
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (panelSettings != null) return panelSettings;

        panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(1920, 1080);
        panelSettings.match = 0.5f;
        // Acima dos Canvas antigos (o DeathFadeCanvas usa 1000): senão eles recebem os cliques primeiro
        panelSettings.sortingOrder = 2000;
        AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
        AssetDatabase.SaveAssets();
        return panelSettings;
    }

    // PreLoad (avisos) -> Menu -> Game, mantendo outras cenas que estiverem na lista
    private static void SetBuildOrder()
    {
        string[] ordered = { PreLoadScenePath, MenuScenePath, GameScenePath };
        var scenes = ordered.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => !ordered.Contains(s.path)));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[MenuSetupTools] Ordem das cenas no build: PreLoad, Menu, Game.");
    }

    // A tela de avisos passa a carregar o Menu em vez do jogo
    private static void PointPreLoadToMenu()
    {
        bool wasOpen = SceneManager.GetSceneByPath(PreLoadScenePath).isLoaded;
        Scene preLoad = EditorSceneManager.OpenScene(PreLoadScenePath, OpenSceneMode.Additive);
        var warning = Object.FindObjectsByType<Echoes.UI.WarningScreen.WarningScreenManager>(FindObjectsInactive.Include)
            .FirstOrDefault(w => w.gameObject.scene == preLoad);

        if (warning != null && warning.targetSceneName != "Menu")
        {
            Undo.RecordObject(warning, "Apontar PreLoad para o Menu");
            warning.targetSceneName = "Menu";
            EditorSceneManager.MarkSceneDirty(preLoad);
            EditorSceneManager.SaveScene(preLoad);
            Debug.Log("[MenuSetupTools] PreLoad agora carrega a cena Menu.");
        }

        if (!wasOpen && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(preLoad, true);
    }
}
