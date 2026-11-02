using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ferramentas do sistema de save (menu Echoes > Save).
/// </summary>
public static class SaveTools
{
    private const string MenuRoot = "Echoes/Save/";

    /// <summary>
    /// Deixa as cenas abertas prontas para checkpoints: SaveableId em todo componente salvável,
    /// um CheckpointManager em _Systems e um ponto de despertar (a poltrona) em Story.
    /// </summary>
    [MenuItem(MenuRoot + "Preparar cena para checkpoints")]
    private static void PrepareScenes()
    {
        if (EditorApplication.isPlaying) return;

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Preparar cena para checkpoints");

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            int added = AddMissingIds(scene);
            bool createdManager = EnsureComponent<CheckpointManager>(scene, "CheckpointManager", "_Systems");
            bool createdSpawn = EnsureComponent<CheckpointSpawn>(scene, "Checkpoint_Poltrona", "Story");

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[SaveTools] {scene.name}: {added} SaveableId adicionados" +
                      (createdManager ? ", CheckpointManager criado em _Systems" : "") +
                      (createdSpawn ? ", Checkpoint_Poltrona criado em Story (mova-o para a poltrona)" : ""));
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    [MenuItem(MenuRoot + "Apagar checkpoint salvo")]
    private static void DeleteSave()
    {
        SaveSystem.Delete();
        Debug.Log($"[SaveTools] Checkpoint apagado ({SaveSystem.FilePath})");
    }

    [MenuItem(MenuRoot + "Abrir pasta do save")]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(SaveSystem.FilePath);
    }

    private static int AddMissingIds(Scene scene)
    {
        int added = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            var owners = root.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(m => m is ISaveable)
                .Select(m => m.gameObject)
                .Distinct();

            foreach (GameObject owner in owners)
            {
                if (owner.GetComponent<SaveableId>() != null) continue;

                Undo.AddComponent<SaveableId>(owner);
                added++;
            }
        }
        return added;
    }

    // Cria um GameObject com o componente dentro do grupo indicado, se a cena ainda não tiver um
    private static bool EnsureComponent<T>(Scene scene, string objectName, string groupName) where T : Component
    {
        bool exists = scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<T>(true) != null);
        if (exists) return false;

        var go = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(go, "Preparar cena para checkpoints");
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.AddComponent<T>(go);

        GameObject group = scene.GetRootGameObjects().FirstOrDefault(root => root.name == groupName);
        if (group != null) Undo.SetTransformParent(go.transform, group.transform, false, "Preparar cena para checkpoints");

        return true;
    }
}
