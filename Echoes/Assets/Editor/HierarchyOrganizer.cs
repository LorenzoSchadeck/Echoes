using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Organiza a raiz das cenas abertas em grupos fixos (_Systems, Player, UI, Environment, Lighting,
/// Interactables, Story, Audio, Flashbacks). Os objetos mantêm a posição no mundo, a operação pode
/// ser desfeita com Ctrl+Z e pode ser repetida sempre que a cena bagunçar.
/// Objetos que nenhuma regra reconhece ficam na raiz e são listados no Console.
/// </summary>
public static class HierarchyOrganizer
{
    // Ordem em que os grupos aparecem na hierarquia
    private static readonly string[] GroupOrder =
    {
        "_Systems", "Player", "UI", "Environment", "Lighting", "Interactables", "Story", "Audio", "Flashbacks"
    };

    // Regras por nome, avaliadas em ordem; a primeira que casar define o destino.
    // Prefixos terminam com '*'; o resto é nome exato.
    private static readonly (string pattern, string path)[] NameRules =
    {
        ("F_*", "Flashbacks"),
        ("Player", "Player"),
        ("Main Camera", "Player"),
        ("MenuCam", "Player"),
        ("Peephole_Camera", "Player"),
        ("RadioCam", "Player"),
        ("CANVAS_*", "UI"),
        ("DeathFadeCanvas", "UI"),
        ("EventSystem", "UI"),
        ("R_REAL", "Environment"),
        ("L_*", "Environment"),
        ("Light", "Lighting"),
        ("Post-Processing Volume", "Lighting"),
        ("GAVETA_*", "Interactables/Gavetas"),
        ("Porta_Armario", "Interactables/Armarios"),
        ("Armario", "Interactables/Armarios"),
        ("Portas_Casa", "Interactables/Portas"),
        ("R_INTERATIVOS", "Interactables"),
        ("TRIGGER_*", "Story/Triggers"),
        ("TP_*", "Story/Teleports"),
        ("Choir_*", "Story/Coro"),
        ("Vizinhos", "Story/Eventos"),
        ("W_*", "Story/Eventos"),
        ("SFX_*", "Audio"),
        ("GameSettings", "_Systems"),
        ("*Manager", "_Systems"),
    };

    private const string MenuRoot = "Echoes/Organizar hierarquia";

    [MenuItem(MenuRoot + "/Simular (só mostra o plano)")]
    private static void Preview() => Run(apply: false);

    [MenuItem(MenuRoot + "/Aplicar")]
    private static void Apply() => Run(apply: true);

    private static void Run(bool apply)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[HierarchyOrganizer] Saia do Play Mode antes de organizar a hierarquia.");
            return;
        }

        var report = new StringBuilder();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Organizar hierarquia");

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) OrganizeScene(scene, apply, report);
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[HierarchyOrganizer] {(apply ? "Aplicado" : "Simulação")}:\n{report}");
    }

    private static void OrganizeScene(Scene scene, bool apply, StringBuilder report)
    {
        report.AppendLine($"== Cena {scene.name}");
        var groupCache = new Dictionary<string, Transform>();
        var unmatched = new List<string>();
        int moved = 0, removed = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (GroupOrder.Contains(root.name)) continue;

            if (IsEmptySeparator(root))
            {
                report.AppendLine($"  remover separador vazio: {root.name}");
                if (apply) Undo.DestroyObjectImmediate(root);
                removed++;
                continue;
            }

            if (HasDoorUsingParentAsPivot(root))
            {
                report.AppendLine($"  PULADO (porta usa o pai como pivô; defina o pivô antes): {root.name}");
                continue;
            }

            string path = ResolvePath(root);
            if (path == null)
            {
                unmatched.Add(root.name);
                continue;
            }

            report.AppendLine($"  {root.name} -> {path}");
            if (apply)
            {
                Transform group = GetOrCreateGroup(scene, path, groupCache);
                Undo.SetTransformParent(root.transform, group, true, "Organizar hierarquia");
            }
            moved++;
        }

        if (apply)
        {
            SortGroups(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        report.AppendLine($"  {moved} movidos, {removed} separadores removidos, {unmatched.Count} sem regra");
        foreach (string name in unmatched) report.AppendLine($"  SEM REGRA (fica na raiz): {name}");
    }

    /// <summary>
    /// Destino de um objeto: primeiro pelo nome, depois pelos componentes.
    /// </summary>
    private static string ResolvePath(GameObject go)
    {
        foreach (var (pattern, path) in NameRules)
        {
            if (Matches(go.name, pattern)) return path;
        }

        if (go.GetComponent<Camera>() != null) return "Player";
        if (go.GetComponent<Canvas>() != null || go.GetComponent<EventSystem>() != null) return "UI";
        if (go.GetComponent<Light>() != null || go.GetComponent<Volume>() != null) return "Lighting";
        if (go.GetComponent<DrawerController>() != null) return "Interactables/Gavetas";
        if (go.GetComponent<CabinetDoor>() != null) return "Interactables/Armarios";
        if (go.GetComponent<DoorController>() != null) return "Interactables/Portas";
        if (go.GetComponent<IInteractable>() != null) return "Interactables";
        if (go.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name.EndsWith("Manager"))) return "_Systems";
        if (go.GetComponent<AudioSource>() != null || go.GetComponent<FMODUnity.StudioEventEmitter>() != null) return "Audio";
        return null;
    }

    private static bool Matches(string name, string pattern)
    {
        if (pattern.StartsWith("*")) return name.EndsWith(pattern.Substring(1));
        if (pattern.EndsWith("*")) return name.StartsWith(pattern.Substring(0, pattern.Length - 1));
        return name == pattern;
    }

    // Objetos como "-------------REAL-----------------": sem componentes além do Transform e sem filhos
    private static bool IsEmptySeparator(GameObject go)
    {
        return go.name.StartsWith("---")
            && go.transform.childCount == 0
            && go.GetComponents<Component>().Length == 1;
    }

    // O DoorController usa o pai como pivô quando o campo "pivot" está vazio;
    // mover essa porta para um grupo faria o grupo inteiro girar.
    private static bool HasDoorUsingParentAsPivot(GameObject go)
    {
        foreach (DoorController door in go.GetComponents<DoorController>())
        {
            var pivot = new SerializedObject(door).FindProperty("pivot");
            if (pivot != null && pivot.objectReferenceValue == null) return true;
        }
        return false;
    }

    private static Transform GetOrCreateGroup(Scene scene, string path, Dictionary<string, Transform> cache)
    {
        if (cache.TryGetValue(path, out Transform cached)) return cached;

        Transform parent = null;
        string current = "";
        foreach (string part in path.Split('/'))
        {
            current = current.Length == 0 ? part : current + "/" + part;
            if (cache.TryGetValue(current, out Transform existing))
            {
                parent = existing;
                continue;
            }

            Transform found = parent == null
                ? scene.GetRootGameObjects().FirstOrDefault(g => g.name == part)?.transform
                : parent.Find(part);

            if (found == null)
            {
                var go = new GameObject(part);
                Undo.RegisterCreatedObjectUndo(go, "Organizar hierarquia");
                SceneManager.MoveGameObjectToScene(go, scene);
                if (parent != null) Undo.SetTransformParent(go.transform, parent, false, "Organizar hierarquia");
                found = go.transform;
            }

            cache[current] = found;
            parent = found;
        }

        return parent;
    }

    // Grupos na ordem fixa e, dentro de cada grupo, filhos em ordem alfabética
    private static void SortGroups(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        int index = 0;
        foreach (string groupName in GroupOrder)
        {
            GameObject group = roots.FirstOrDefault(g => g.name == groupName);
            if (group == null) continue;

            Undo.RegisterFullObjectHierarchyUndo(group, "Organizar hierarquia");
            group.transform.SetSiblingIndex(index++);
            SortChildrenRecursive(group.transform);
        }
    }

    private static void SortChildrenRecursive(Transform group)
    {
        var children = group.Cast<Transform>().OrderBy(t => t.name, System.StringComparer.OrdinalIgnoreCase).ToList();
        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
            // Só desce em subgrupos criados por este script (ex.: Interactables/Gavetas)
            if (IsSubgroup(children[i])) SortChildrenRecursive(children[i]);
        }
    }

    private static bool IsSubgroup(Transform t)
    {
        return t.GetComponents<Component>().Length == 1
            && !PrefabUtility.IsPartOfPrefabInstance(t)
            && NameRules.Any(r => r.path.EndsWith("/" + t.name));
    }
}
