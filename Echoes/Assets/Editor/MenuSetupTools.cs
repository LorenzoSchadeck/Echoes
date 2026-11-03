using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
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
/// cena Menu, ordem das cenas no build e a troca do menu diegético pela pausa na cena Game.
/// </summary>
public static class MenuSetupTools
{
    private const string MenuRoot = "Echoes/Menu/";
    private const string UiFolder = "Assets/UI/Menu";
    private const string PanelSettingsPath = UiFolder + "/EchoesPanelSettings.asset";
    private const string ThemePath = UiFolder + "/EchoesTheme.tss";
    private const string MainMenuUxmlPath = UiFolder + "/MainMenu.uxml";
    private const string PauseMenuUxmlPath = UiFolder + "/PauseMenu.uxml";
    private const string PreLoadScenePath = "Assets/Scenes/PreLoad.unity";
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    // Chave, português, inglês. Entradas que já existem na tabela não são sobrescritas.
    private static readonly (string key, string pt, string en)[] MenuStrings =
    {
        ("M_CHAPTER", "Capítulo 1", "Chapter 1"),
        ("M_PAUSED", "Pausado", "Paused"),
        ("M_MAIN_MENU", "Voltar ao menu", "Main menu"),
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

    [MenuItem(MenuRoot + "3. Trocar menu diegético por pausa (cena aberta)")]
    private static void ConvertGameScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameScenePath)
        {
            Debug.LogError($"[MenuSetupTools] Abra {GameScenePath} como cena ativa antes de rodar esta etapa.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Trocar menu diegético por pausa");

        MenuUIManager oldMenu = Object.FindAnyObjectByType<MenuUIManager>(FindObjectsInactive.Include);
        var oldMenuData = oldMenu != null ? new SerializedObject(oldMenu) : null;
        var playerCamera = oldMenuData?.FindProperty("playerCamera").objectReferenceValue as CinemachineCamera;
        var menuCamera = oldMenuData?.FindProperty("menuCamera").objectReferenceValue as CinemachineCamera;
        var lookController = oldMenuData?.FindProperty("customInputAxisController").objectReferenceValue as CustomInputAxisController;
        var crosshair = oldMenuData?.FindProperty("crosshairObject").objectReferenceValue as GameObject;

        // Inicia o jogo direto, sem passar pelo menu da cena
        if (Object.FindAnyObjectByType<GameplayStarter>(FindObjectsInactive.Include) == null)
        {
            GameObject starter = CreateChild(scene, "GameplayStarter", "_Systems");
            var starterData = new SerializedObject(Undo.AddComponent<GameplayStarter>(starter));
            starterData.FindProperty("playerCamera").objectReferenceValue = playerCamera;
            starterData.FindProperty("lookController").objectReferenceValue = lookController;
            starterData.FindProperty("crosshair").objectReferenceValue = crosshair;
            SerializedProperty lowered = starterData.FindProperty("camerasToLower");
            lowered.arraySize = menuCamera != null ? 1 : 0;
            if (menuCamera != null) lowered.GetArrayElementAtIndex(0).objectReferenceValue = menuCamera;
            starterData.ApplyModifiedPropertiesWithoutUndo();
        }

        // Menu de pausa em UI Toolkit
        if (Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include) == null)
        {
            GameObject pause = CreateChild(scene, "PauseMenu", "UI");
            var document = Undo.AddComponent<UIDocument>(pause);
            document.panelSettings = EnsurePanelSettings();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PauseMenuUxmlPath);
            document.sortingOrder = 100;

            var pauseData = new SerializedObject(Undo.AddComponent<PauseMenuController>(pause));
            pauseData.FindProperty("lookController").objectReferenceValue = lookController;
            pauseData.ApplyModifiedPropertiesWithoutUndo();
        }

        // O menu diegético sai de cena: desativado (não apagado) para poder voltar com Ctrl+Z ou à mão
        var disabled = new List<string>();
        if (oldMenu != null && oldMenu.gameObject.activeSelf)
        {
            Undo.RecordObject(oldMenu.gameObject, "Desativar menu diegético");
            oldMenu.gameObject.SetActive(false);
            disabled.Add(oldMenu.name);
        }
        if (menuCamera != null && menuCamera.gameObject.activeSelf)
        {
            Undo.RecordObject(menuCamera.gameObject, "Desativar câmera do menu");
            menuCamera.gameObject.SetActive(false);
            disabled.Add(menuCamera.name);
        }
        foreach (MenuInteractable interactable in Object.FindObjectsByType<MenuInteractable>(FindObjectsInactive.Include))
        {
            disabled.Add($"{interactable.name} (componente MenuInteractable removido)");
            Undo.DestroyObjectImmediate(interactable);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[MenuSetupTools] Cena Game: GameplayStarter e PauseMenu prontos. Desativados: {string.Join(", ", disabled)}. Salve a cena.");
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

    private static GameObject CreateChild(Scene scene, string name, string groupName)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        SceneManager.MoveGameObjectToScene(go, scene);

        GameObject group = scene.GetRootGameObjects().FirstOrDefault(root => root.name == groupName);
        if (group != null) Undo.SetTransformParent(go.transform, group.transform, false, name);
        return go;
    }
}
