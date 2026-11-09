using System.Collections.Generic;
using System.Linq;
using Echoes.UI.WarningScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Separa o aviso da história. A PreLoad fica só com o aviso de conteúdo; os textos de contexto
/// passam para depois do "Novo jogo", em duas telas: Historia_1996 (o caso do Açougueiro, antes da
/// abertura) e Historia_2003 (o Ethan de hoje, depois de "Oito anos depois", antes do apartamento).
/// </summary>
public static class StoryScreensTools
{
    private const string PreLoadPath = "Assets/Scenes/PreLoad.unity";
    private const string MenuPath = "Assets/Scenes/Menu.unity";
    private const string OpeningPath = "Assets/Scenes/Abertura1995.unity";
    private const string Story1996Path = "Assets/Scenes/Historia_1996.unity";
    private const string Story2003Path = "Assets/Scenes/Historia_2003.unity";
    private const string PanelFolder = "Assets/Resources/WarningPanels";

    [MenuItem("Echoes/Abertura do estúdio/Separar aviso e história")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[StoryScreensTools] Saia do play mode."); return; }

        WarningPanelData Panel(string lang, string name) => AssetDatabase.LoadAssetAtPath<WarningPanelData>($"{PanelFolder}/{lang}/{name}.asset");
        var warning = new[] { Panel("ptbr", "Warning_pt-br"), Panel("en", "Warning_en") };
        var lore1996 = new[] { Panel("ptbr", "Lore-pt-br"), Panel("en", "Lore-en") };
        var lore2003 = new[] { Panel("ptbr", "Lore-pt-br-2"), Panel("en", "Lore-en-2") };

        // Telas de história: cópias da PreLoad sem a abertura do estúdio
        MakeStoryScene(Story1996Path, lore1996, "Abertura1995");
        MakeStoryScene(Story2003Path, lore2003, "ApartamentoDemo");

        // PreLoad: só o aviso, depois o menu
        EditScene(PreLoadPath, scene =>
        {
            WarningScreenManager manager = FindManager(scene);
            SetPanels(manager, warning, "Menu");
        });

        // Novo jogo começa pela história de 1996
        EditScene(MenuPath, scene =>
        {
            var menu = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<MainMenuController>(true)).First(m => m != null);
            var data = new SerializedObject(menu);
            data.FindProperty("gameSceneName").stringValue = "Historia_1996";
            data.ApplyModifiedPropertiesWithoutUndo();
        });

        // Depois de "Oito anos depois", a história de 2003 antes do apartamento
        EditScene(OpeningPath, scene =>
        {
            var director = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<OpeningDirector>(true)).First(d => d != null);
            var data = new SerializedObject(director);
            data.FindProperty("nextScene").stringValue = "Historia_2003";
            data.ApplyModifiedPropertiesWithoutUndo();
        });

        // Ordem do build: PreLoad, Menu, (cenário do menu), Historia_1996, Abertura1995, Historia_2003, ApartamentoDemo
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s => s.path == Story1996Path || s.path == Story2003Path);
        int opening = scenes.FindIndex(s => s.path == OpeningPath);
        scenes.Insert(opening, new EditorBuildSettingsScene(Story1996Path, true));
        int game = scenes.FindIndex(s => s.path == "Assets/Scenes/ApartamentoDemo.unity");
        scenes.Insert(game, new EditorBuildSettingsScene(Story2003Path, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[StoryScreensTools] Aviso na PreLoad; história em Historia_1996 e Historia_2003.");
    }

    private static void MakeStoryScene(string path, WarningPanelData[] panels, string next)
    {
        CloseIfOpen(path);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(PreLoadPath, path);
        EditScene(path, scene =>
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<StudioIntro>(true) != null) Object.DestroyImmediate(root);
            WarningScreenManager manager = FindManager(scene);
            manager.enabled = true;
            SetPanels(manager, panels, next);
        });
    }

    private static WarningScreenManager FindManager(Scene scene) =>
        scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<WarningScreenManager>(true)).First(m => m != null);

    private static void SetPanels(WarningScreenManager manager, IEnumerable<WarningPanelData> panels, string next)
    {
        Undo.RecordObject(manager, "Painéis");
        manager.warningPanels = panels.Where(p => p != null).ToList();
        manager.targetSceneName = next;
        EditorUtility.SetDirty(manager);
    }

    // Abre a cena por cima (sem fechar a atual), aplica a mudança, salva e fecha
    private static void EditScene(string path, System.Action<Scene> edit)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool wasOpen = scene.isLoaded;
        if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        edit(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
    }

    private static void CloseIfOpen(string path)
    {
        Scene open = SceneManager.GetSceneByPath(path);
        if (open.isLoaded && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(open, true);
    }
}
