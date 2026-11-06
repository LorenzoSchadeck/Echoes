using System.Linq;
using FMODUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a cena MenuCenario, o fundo 3D do menu principal: uma cópia da Abertura1995 sem jogador,
/// HUD e roteiro, com a câmera do menu e os dois enquadramentos (quarto 6 e corredor).
/// Rode de novo depois de remontar a abertura para o menu acompanhar as mudanças.
/// </summary>
public static class MenuBackdropSetupTools
{
    private const string SourcePath = "Assets/Scenes/Abertura1995.unity";
    private const string ScenePath = "Assets/Scenes/MenuCenario.unity";
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";

    [MenuItem("Echoes/Menu/Montar cenário do menu")]
    private static void Build() => Build(MenuBackdrop.Shot.Quarto);

    [MenuItem("Echoes/Menu/Enquadramento: quarto 6")]
    private static void UseRoom() => SetShot(MenuBackdrop.Shot.Quarto);

    [MenuItem("Echoes/Menu/Enquadramento: corredor")]
    private static void UseCorridor() => SetShot(MenuBackdrop.Shot.Corredor);

    public static void Build(MenuBackdrop.Shot startShot)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[MenuBackdropSetupTools] Saia do play mode antes de montar o cenário do menu.");
            return;
        }
        if (!System.IO.File.Exists(SourcePath))
        {
            Debug.LogError("[MenuBackdropSetupTools] Monte a abertura 1995 primeiro (Echoes/Roteiro/Montar abertura 1995).");
            return;
        }

        CloseIfOpen(ScenePath);
        AssetDatabase.DeleteAsset(ScenePath);
        AssetDatabase.CopyAsset(SourcePath, ScenePath);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        // Fora: jogador, HUD, sistemas e o roteiro (Clara, zonas, diretor)
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name is "Player" or "UI" or "_Systems" or "Story") Object.DestroyImmediate(root);

        // Nada de interação no cenário: portas, pistas e itens viram só cenografia
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IInteractable || behaviour is DoorController || behaviour is PlayerZone)
                    Object.DestroyImmediate(behaviour);

        GameObject lighting = scene.GetRootGameObjects().First(r => r.name == "Lighting");
        GameObject interactables = scene.GetRootGameObjects().First(r => r.name == "Interactables");
        PoliceLights police = lighting.GetComponentInChildren<PoliceLights>(true);
        // Abertura montada antes do clarão na janela: acrescenta aqui
        if (police != null && police.transform.Find("Clarao_Janela") == null) OpeningSetupTools.AddPoliceGlow(police);
        FlickeringLight roomLight = lighting.transform.Find("Luminaria_Teto")?.GetComponent<FlickeringLight>();
        FlickeringLight corridorLight = lighting.transform.Find("Lampada_Corredor")?.GetComponent<FlickeringLight>();
        Transform door6 = interactables.transform.Find("Portas/Porta_6");

        // Mais escuro que a abertura: no menu só as lâmpadas, a rua e o giroflex desenham a cena
        SceneManager.SetActiveScene(scene);
        // A cópia herda a luz assada da abertura: solta, para assar a do menu sem sobrescrever a outra
        Lightmapping.lightingDataAsset = null;
        RenderSettings.ambientLight = new Color(0.07f, 0.075f, 0.095f);
        // A luz fria da rua um pouco mais forte, para a cama e o lençol aparecerem no escuro
        Light street = lighting.transform.Find("Luz_Rua_Janela")?.GetComponent<Light>();
        if (street != null) street.intensity = 60f;

        var menuRoot = new GameObject("MenuCenario");
        SceneManager.MoveGameObjectToScene(menuRoot, scene);

        // Câmera do menu, com pós-processamento e o ouvinte do FMOD
        var cameraObject = new GameObject("Camera_Menu");
        cameraObject.transform.SetParent(menuRoot.transform, false);
        var menuCamera = cameraObject.AddComponent<Camera>();
        menuCamera.nearClipPlane = 0.1f;
        menuCamera.farClipPlane = 400f;
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = Color.black;
        var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        cameraObject.AddComponent<StudioListener>();

        Transform shots = new GameObject("Enquadramentos").transform;
        shots.SetParent(menuRoot.transform, false);
        Transform Point(string name, Vector3 position, Vector3 lookAt)
        {
            var point = new GameObject(name).transform;
            point.SetParent(shots, false);
            point.position = position;
            point.LookAt(lookAt);
            return point;
        }

        // Composição 2: o quarto 6 visto da porta aberta, a cama coberta e a janela com o giroflex
        var room = new MenuBackdrop.ShotSetup
        {
            from = Point("Quarto_De", new Vector3(0.7f, 6.7f, 60.6f), new Vector3(-1.0f, 7.4f, 78f)),
            to = Point("Quarto_Para", new Vector3(1.1f, 6.4f, 64.4f), new Vector3(-1.5f, 7.0f, 78f)),
            options = Point("Quarto_Opcoes", new Vector3(2.6f, 5.6f, 68.5f), new Vector3(-3.6f, 3.0f, 75.5f)),
            duration = 55f,
            fieldOfView = 56f,
            policeLights = true,
            roomLight = false,
            corridorLight = false,
            door6Angle = -80f,
        };

        // Composição 3: o corredor visto do alto da escada, a porta 6 encostada no fim
        var corridor = new MenuBackdrop.ShotSetup
        {
            from = Point("Corredor_De", new Vector3(0.7f, 6.9f, 1.5f), new Vector3(-0.2f, 5.4f, 62f)),
            to = Point("Corredor_Para", new Vector3(0.9f, 6.6f, 9.5f), new Vector3(0.1f, 5.2f, 62f)),
            options = Point("Corredor_Opcoes", new Vector3(2.3f, 6.6f, 37.5f), new Vector3(-3.85f, 4.2f, 52.3f)),
            duration = 60f,
            fieldOfView = 44f,
            policeLights = true,
            roomLight = true,
            door6Angle = -8f,
        };

        var backdrop = menuRoot.AddComponent<MenuBackdrop>();
        EventReference ambience = EventManager.EventFromPath("event:/Opening/Rain") != null
            ? EventReference.Find("event:/Opening/Rain")
            : default;
        backdrop.EditorSetup(menuCamera, police, roomLight, corridorLight, door6, startShot, room, corridor, ambience);

        // Prévia no editor já no começo do movimento
        cameraObject.transform.SetPositionAndRotation(
            (startShot == MenuBackdrop.Shot.Quarto ? room : corridor).from.position,
            (startShot == MenuBackdrop.Shot.Quarto ? room : corridor).from.rotation);
        menuCamera.fieldOfView = (startShot == MenuBackdrop.Shot.Quarto ? room : corridor).fieldOfView;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);
        AddToBuildSettings();
        Debug.Log($"[MenuBackdropSetupTools] Cenário do menu montado em {ScenePath} (enquadramento: {startShot}).");
    }

    // Troca o enquadramento salvo na cena (o que o menu mostra ao abrir)
    private static void SetShot(MenuBackdrop.Shot shot)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[MenuBackdropSetupTools] Saia do play mode; no jogo, F2 troca o enquadramento.");
            return;
        }
        if (!System.IO.File.Exists(ScenePath))
        {
            Build(shot);
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasOpen = scene.isLoaded;
        if (!wasOpen) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        MenuBackdrop backdrop = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<MenuBackdrop>()).FirstOrDefault(b => b != null);
        if (backdrop != null)
        {
            var data = new SerializedObject(backdrop);
            data.FindProperty("shot").enumValueIndex = (int)shot;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        Debug.Log($"[MenuBackdropSetupTools] Enquadramento do menu: {shot}.");
    }

    private static void CloseIfOpen(string path)
    {
        Scene open = SceneManager.GetSceneByPath(path);
        if (open.isLoaded && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(open, true);
    }

    private static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s => s.path == ScenePath);
        int menuIndex = scenes.FindIndex(s => s.path == MenuScenePath);
        var entry = new EditorBuildSettingsScene(ScenePath, true);
        if (menuIndex >= 0) scenes.Insert(menuIndex + 1, entry);
        else scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
