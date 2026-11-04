using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

/// <summary>
/// Cria a tabela de localização "inner_voice" com as falas iniciais de Ethan e um objeto VozInterna
/// com os gatilhos dos momentos globais (colapso, tempo perdido, remédio, sanidade baixa).
/// Falas que já existem na tabela não são sobrescritas.
/// </summary>
public static class InnerVoiceSetupTools
{
    // Chave, português, inglês
    private static readonly (string key, string pt, string en)[] Lines =
    {
        ("IV_WAKE_1", "Quanto tempo eu fiquei apagado?", "How long was I out?"),
        ("IV_WAKE_2", "De novo. O sofá de novo.", "Again. The couch again."),
        ("IV_WAKE_3", "Eu não lembro de ter sentado aqui.", "I don't remember sitting down."),
        ("IV_LOST_1", "Que horas são? Não... não pode ser.", "What time is it? No... that can't be right."),
        ("IV_LOST_2", "Onde eu estava?", "Where was I?"),
        ("IV_REMEDY_1", "Só mais um. Pra não pensar.", "Just one more. So I don't have to think."),
        ("IV_REMEDY_2", "Tá ficando quieto. Bom.", "It's getting quiet. Good."),
        ("IV_SANITY_1", "Respira, Ethan. Respira.", "Breathe, Ethan. Just breathe."),
        ("IV_SANITY_2", "As paredes... estão mais perto?", "The walls... are they closer?"),
    };

    [MenuItem("Echoes/Voz interna/Criar textos e gatilhos básicos")]
    private static void Setup()
    {
        CreateStrings();
        CreateTriggers();
    }

    private static void CreateStrings() => LocalizationSetup.AddStrings(InnerVoice.Table, Lines);

    private static void CreateTriggers()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Any(t => t.name == "VozInterna"))
        {
            Debug.Log("[InnerVoiceSetupTools] VozInterna já existe na cena; gatilhos não recriados.");
            return;
        }

        var go = new GameObject("VozInterna");
        Undo.RegisterCreatedObjectUndo(go, "Criar voz interna");
        SceneManager.MoveGameObjectToScene(go, scene);
        GameObject systems = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "_Systems");
        if (systems != null) Undo.SetTransformParent(go.transform, systems.transform, false, "Criar voz interna");

        Add(go, InnerVoiceTrigger.Moment.WokeFromCollapse, true, 0f, "IV_WAKE_1", "IV_WAKE_2", "IV_WAKE_3");
        Add(go, InnerVoiceTrigger.Moment.LostTime, true, 0f, "IV_LOST_1", "IV_LOST_2");
        Add(go, InnerVoiceTrigger.Moment.RemedyUsed, false, 60f, "IV_REMEDY_1", "IV_REMEDY_2");
        Add(go, InnerVoiceTrigger.Moment.SanityBelow, true, 30f, "IV_SANITY_1", "IV_SANITY_2");

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[InnerVoiceSetupTools] VozInterna criada em _Systems com 4 gatilhos. Salve a cena.");
    }

    // Um filho por gatilho: o save guarda o estado por objeto e tipo de componente
    private static void Add(GameObject parent, InnerVoiceTrigger.Moment moment, bool repeatLast, float cooldown, params string[] keys)
    {
        var go = new GameObject(moment.ToString());
        Undo.RegisterCreatedObjectUndo(go, "Criar voz interna");
        Undo.SetTransformParent(go.transform, parent.transform, false, "Criar voz interna");
        Undo.AddComponent<SaveableId>(go);

        var trigger = Undo.AddComponent<InnerVoiceTrigger>(go);
        var lines = keys.Select(key => new InnerVoiceLine { text = new LocalizedString(InnerVoice.Table, key) }).ToArray();
        trigger.EditorSetup(moment, lines, repeatLast, cooldown);
        EditorUtility.SetDirty(trigger);
    }
}
