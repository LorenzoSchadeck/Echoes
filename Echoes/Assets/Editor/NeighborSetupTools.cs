using System.Linq;
using FMODUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta o bilhete do vizinho na cena aberta: textos, o papel atrás da porta de entrada (cópia da
/// Carta_OLA), a pista "Bilhete do vizinho" passando para ele e o sistema que bate na porta.
/// </summary>
public static class NeighborSetupTools
{
    private const string ItemsTable = "items";
    private const string NoteName = "Bilhete_Vizinho";
    private const string NoteEvidencePath = "Assets/Data/Board/EV_TEST_NOTE.asset";

    // Nome e apartamento provisórios, até o roteiro da Fase 3
    private static readonly (string key, string pt, string en)[] ItemLines =
    {
        ("ITEM_NEIGHBOR_NOTE", "Bilhete do vizinho", "Neighbor's note"),
        ("ITEM_NEIGHBOR_NOTE_DESC",
            "\"Seu rádio de novo, às três da manhã. Tem gente aqui que trabalha cedo. Da próxima vez eu chamo o síndico. — W. Kessler, 3B\"\nNo verso, uma lista de compras e um endereço: Rua Halley, 41.",
            "\"Your radio again, at three in the morning. Some of us work early. Next time I'm calling the super. — W. Kessler, 3B\"\nOn the back, a shopping list and an address: 41 Halley Street."),
    };

    private static readonly (string key, string pt, string en)[] ThoughtLines =
    {
        ("IV_NEIGHBOR_NOTE", "Às três da manhã? Eu não liguei o rádio essa noite.", "Three in the morning? I didn't turn the radio on last night."),
    };

    [MenuItem("Echoes/Vizinho/Criar bilhete do vizinho")]
    private static void Setup()
    {
        LocalizationSetup.AddStrings(ItemsTable, ItemLines);
        LocalizationSetup.AddStrings(InnerVoice.Table, ThoughtLines);

        Scene scene = SceneManager.GetActiveScene();
        if (Object.FindAnyObjectByType<NeighborNoteDelivery>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[NeighborSetupTools] A cena já tem o bilhete do vizinho; nada recriado.");
            return;
        }

        ItemInteract template = Object.FindObjectsByType<ItemInteract>(FindObjectsInactive.Include).FirstOrDefault(i => i.name == "Carta_OLA");
        DoorController entrance = FindEntrance(template);
        if (template == null || entrance == null)
        {
            Debug.LogError("[NeighborSetupTools] Carta_OLA ou a porta de entrada não encontradas na cena.");
            return;
        }

        GameObject note = CreateNote(template);
        MoveNoteEvidence(note);

        var system = new GameObject("Vizinho");
        Undo.RegisterCreatedObjectUndo(system, "Criar bilhete do vizinho");
        SceneManager.MoveGameObjectToScene(system, scene);
        GameObject systems = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "_Systems");
        if (systems != null) Undo.SetTransformParent(system.transform, systems.transform, false, "Criar bilhete do vizinho");

        var delivery = Undo.AddComponent<NeighborNoteDelivery>(system);
        delivery.EditorSetup(entrance.transform, note,
            EventReference.Find("event:/Effects/Door_Knock"),
            EventReference.Find("event:/Grab/Pages"),
            new InnerVoiceLine { text = new LocalizedString(InnerVoice.Table, "IV_NEIGHBOR_NOTE") });
        Undo.AddComponent<SaveableId>(system);
        EditorUtility.SetDirty(delivery);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = note;
        Debug.Log("[NeighborSetupTools] Bilhete do vizinho criado. Confira a posição do papel e salve a cena.");
    }

    // A porta de entrada mais próxima da Carta_OLA (a cena tem uma cópia da casa no flashback)
    private static DoorController FindEntrance(ItemInteract template)
    {
        if (template == null) return null;
        return Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include)
            .Where(d => d.name == "Pivot_Entrada")
            .OrderBy(d => Vector3.Distance(d.transform.position, template.transform.position))
            .FirstOrDefault();
    }

    private static GameObject CreateNote(ItemInteract template)
    {
        GameObject note = Object.Instantiate(template.gameObject, template.transform.parent);
        note.name = NoteName;
        Undo.RegisterCreatedObjectUndo(note, "Criar bilhete do vizinho");

        // Ao lado da carta do rádio, um pouco virado, para os dois papéis não se sobreporem
        note.transform.position = template.transform.position + new Vector3(0.35f, 0.002f, 0.25f);
        note.transform.rotation = template.transform.rotation * Quaternion.Euler(0f, 0f, 18f);

        var item = note.GetComponent<ItemInteract>();
        var data = new SerializedObject(item);
        SetLocalized(data.FindProperty("itemNameString"), ItemsTable, "ITEM_NEIGHBOR_NOTE");
        SetLocalized(data.FindProperty("itemDescriptionString"), ItemsTable, "ITEM_NEIGHBOR_NOTE_DESC");
        data.FindProperty("triggerRadioEvents").boolValue = false;
        data.FindProperty("radioTriggerType").enumValueIndex = (int)ItemInteract.RadioTriggerType.None;
        data.ApplyModifiedPropertiesWithoutUndo();

        var id = Undo.AddComponent<SaveableId>(note);
        var idData = new SerializedObject(id);
        idData.FindProperty("saveActiveState").boolValue = true;
        idData.ApplyModifiedPropertiesWithoutUndo();

        note.SetActive(false);
        return note;
    }

    // A pista do bilhete estava num item provisório; agora sai do papel que o vizinho passou
    private static void MoveNoteEvidence(GameObject note)
    {
        var evidence = AssetDatabase.LoadAssetAtPath<Evidence>(NoteEvidencePath);
        if (evidence == null) return;

        foreach (EvidenceSource source in Object.FindObjectsByType<EvidenceSource>(FindObjectsInactive.Include))
        {
            if (new SerializedObject(source).FindProperty("evidence").objectReferenceValue == evidence)
                Undo.DestroyObjectImmediate(source);
        }

        var newSource = Undo.AddComponent<EvidenceSource>(note);
        newSource.EditorSetup(evidence, new InnerVoiceLine { text = new LocalizedString("evidence", "EV_FOUND_THOUGHT") });
        EditorUtility.SetDirty(newSource);
    }

    private static void SetLocalized(SerializedProperty property, string table, string key)
    {
        var value = new LocalizedString(table, key);
        property.boxedValue = value;
    }
}
