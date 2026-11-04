using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Monta o mural na cena aberta: textos da interface, três pistas e uma dedução de teste,
/// a tela do mural ao lado da pausa e um mural provisório (um bloco) na frente da câmera da Scene View.
/// As pistas de teste ainda precisam de um EvidenceSource num item examinável.
/// </summary>
public static class BoardSetupTools
{
    private const string DataFolder = "Assets/Data/Board";
    private const string UxmlPath = "Assets/UI/Board/EvidenceBoard.uxml";
    private const string EvidenceTable = "evidence";

    private static readonly (string key, string pt, string en)[] MenuLines =
    {
        ("M_BOARD", "Mural", "Board"),
        ("M_BOARD_EMPTY", "Nada no mural ainda. Examine o apartamento.", "Nothing on the board yet. Search the apartment."),
        ("M_BOARD_HINT", "Escolha duas pistas para ligá-las. Só vale se data e lugar baterem.", "Pick two clues to link them. It only holds if the date and place match."),
        ("M_BOARD_PICK_SECOND", "Ligar com qual?", "Link it to what?"),
        ("M_BOARD_NO_MATCH", "Não bate. A data ou o lugar não fecham.", "It doesn't fit. The date or the place is off."),
        ("M_BOARD_ALREADY", "Essas duas já estão ligadas.", "Those two are already linked."),
        ("M_BOARD_CONCLUSIONS", "Conclusões", "Conclusions"),
        ("M_BOARD_NO_CONCLUSIONS", "Nenhuma ainda.", "None yet."),
        ("M_BOARD_CLOSE", "Fechar", "Close"),
    };

    // Conteúdo provisório, só para testar o mural; o definitivo entra na fase de conteúdo
    private static readonly (string key, string pt, string en)[] EvidenceLines =
    {
        ("EV_TEST_CLIPPING", "Recorte: terceira vítima", "Clipping: third victim"),
        ("EV_TEST_CLIPPING_TEXT", "Mulher encontrada morta na Rua Halley. A polícia não tem suspeitos.", "Woman found dead on Halley Street. Police have no suspects."),
        ("EV_TEST_CLIPPING_PLACE", "Rua Halley", "Halley Street"),
        ("EV_TEST_NOTE", "Bilhete do vizinho", "Neighbor's note"),
        ("EV_TEST_NOTE_TEXT", "Caiu por baixo da porta. Uma lista de compras; no verso, um endereço.", "It slid under the door. A shopping list; an address on the back."),
        ("EV_TEST_NOTE_PLACE", "Rua Halley, 41", "41 Halley Street"),
        ("EV_TEST_PRESCRIPTION", "Receita médica", "Prescription"),
        ("EV_TEST_PRESCRIPTION_TEXT", "Diazepam, 10 mg. Paciente: E. Myers. A assinatura do médico está borrada.", "Diazepam, 10 mg. Patient: E. Myers. The doctor's signature is smudged."),
        ("EV_TEST_PRESCRIPTION_PLACE", "Clínica São Lucas", "St. Luke's Clinic"),
        ("DED_TEST_HALLEY", "O vizinho esteve na Rua Halley no dia da terceira morte.", "The neighbor was on Halley Street the day of the third murder."),
        ("DED_TEST_HALLEY_THOUGHT", "Eu sabia. Eu sabia que era ele.", "I knew it. I knew it was him."),
        ("EV_FOUND_THOUGHT", "Isso vai pro mural.", "This goes on the board."),
    };

    [MenuItem("Echoes/Mural/Criar mural de teste")]
    private static void Setup()
    {
        LocalizationSetup.AddStrings(MenuText.Table, MenuLines);
        LocalizationSetup.AddStrings("interaction_prompts", ("PROMPT_BOARD", "(E) Ver o mural", "(E) Look at the board"));
        LocalizationSetup.AddStrings(EvidenceTable, EvidenceLines);

        Evidence clipping = CreateEvidence("EV_TEST_CLIPPING", "14.03.1995", new Vector2(0.28f, 0.3f));
        Evidence note = CreateEvidence("EV_TEST_NOTE", "14.03.1995", new Vector2(0.68f, 0.36f));
        Evidence prescription = CreateEvidence("EV_TEST_PRESCRIPTION", "02.11.1995", new Vector2(0.46f, 0.72f));
        Deduction halley = CreateDeduction("DED_TEST_HALLEY", clipping, note);
        AssetDatabase.SaveAssets();

        CreateSceneObjects(new[] { clipping, note, prescription }, new[] { halley });
    }

    private static Evidence CreateEvidence(string key, string date, Vector2 position)
    {
        string path = $"{DataFolder}/{key}.asset";
        var evidence = AssetDatabase.LoadAssetAtPath<Evidence>(path);
        if (evidence != null) return evidence;

        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            AssetDatabase.CreateFolder("Assets/Data", "Board");
        }

        evidence = ScriptableObject.CreateInstance<Evidence>();
        evidence.title = new LocalizedString(EvidenceTable, key);
        evidence.details = new LocalizedString(EvidenceTable, key + "_TEXT");
        evidence.place = new LocalizedString(EvidenceTable, key + "_PLACE");
        evidence.date = date;
        evidence.boardPosition = position;
        AssetDatabase.CreateAsset(evidence, path);
        return evidence;
    }

    private static Deduction CreateDeduction(string key, Evidence first, Evidence second)
    {
        string path = $"{DataFolder}/{key}.asset";
        var deduction = AssetDatabase.LoadAssetAtPath<Deduction>(path);
        if (deduction != null) return deduction;

        deduction = ScriptableObject.CreateInstance<Deduction>();
        deduction.first = first;
        deduction.second = second;
        deduction.conclusion = new LocalizedString(EvidenceTable, key);
        deduction.reaction = new InnerVoiceLine { text = new LocalizedString(EvidenceTable, key + "_THOUGHT") };
        AssetDatabase.CreateAsset(deduction, path);
        return deduction;
    }

    private static void CreateSceneObjects(Evidence[] evidence, Deduction[] deductions)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Object.FindAnyObjectByType<EvidenceBoardUI>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[BoardSetupTools] A cena já tem um mural; objetos não recriados.");
            return;
        }

        PauseMenuController pause = Object.FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
        if (pause == null)
        {
            Debug.LogError("[BoardSetupTools] Sem PauseMenu na cena: rode antes Echoes/Menu/3.");
            return;
        }

        // Tela do mural, ao lado da pausa e logo abaixo dela
        var uiObject = new GameObject("MuralUI");
        Undo.RegisterCreatedObjectUndo(uiObject, "Criar mural");
        SceneManager.MoveGameObjectToScene(uiObject, scene);
        Undo.SetTransformParent(uiObject.transform, pause.transform.parent, false, "Criar mural");

        var pauseDocument = pause.GetComponent<UIDocument>();
        var document = Undo.AddComponent<UIDocument>(uiObject);
        document.panelSettings = pauseDocument.panelSettings;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        document.sortingOrder = pauseDocument.sortingOrder - 1;

        var boardUI = Undo.AddComponent<EvidenceBoardUI>(uiObject);
        var uiData = new SerializedObject(boardUI);
        SetArray(uiData.FindProperty("evidenceCatalog"), evidence);
        SetArray(uiData.FindProperty("deductionCatalog"), deductions);
        uiData.ApplyModifiedPropertiesWithoutUndo();

        // Mural provisório: um bloco escuro, para trocar depois por um modelo
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "TESTE_Mural";
        Undo.RegisterCreatedObjectUndo(board, "Criar mural");
        SceneManager.MoveGameObjectToScene(board, scene);
        GameObject story = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Story");
        if (story != null) Undo.SetTransformParent(board.transform, story.transform, false, "Criar mural");

        board.transform.localScale = new Vector3(1.4f, 0.9f, 0.04f);
        if (SceneView.lastActiveSceneView != null) board.transform.position = SceneView.lastActiveSceneView.pivot;
        board.layer = InteractLayer();

        var boardComponent = Undo.AddComponent<EvidenceBoard>(board);
        boardComponent.EditorSetup(boardUI, new LocalizedString("interaction_prompts", "PROMPT_BOARD"));
        EditorUtility.SetDirty(boardComponent);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = board;
        Debug.Log("[BoardSetupTools] MuralUI e TESTE_Mural criados. Posicione o mural numa parede e salve a cena.");
    }

    // A camada que o PlayerInteractor enxerga, para o raio de interação acertar o mural
    private static int InteractLayer()
    {
        PlayerInteractor interactor = Object.FindAnyObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        if (interactor == null) return 0;

        int mask = new SerializedObject(interactor).FindProperty("interactLayer").intValue;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask & (1 << layer)) != 0) return layer;
        }
        return 0;
    }

    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
