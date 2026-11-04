using UnityEngine;

/// <summary>
/// Faz um item virar pista: na primeira vez que o jogador o examina, a pista vai para o mural.
/// </summary>
public class EvidenceSource : MonoBehaviour
{
    [SerializeField] private Evidence evidence;
    [Tooltip("Item que precisa ser examinado. Se vazio, usa o deste objeto.")]
    [SerializeField] private ItemInteract item;
    [Tooltip("O que Ethan pensa ao achar a pista. Opcional.")]
    [SerializeField] private InnerVoiceLine thought = new InnerVoiceLine();

    private void Awake()
    {
        if (item == null) item = GetComponent<ItemInteract>();
    }

    private void OnEnable() => GameEvents.OnItemExamined += HandleItemExamined;

    private void OnDisable() => GameEvents.OnItemExamined -= HandleItemExamined;

    private void HandleItemExamined(ItemInteract examined)
    {
        if (examined == item) Collect();
    }

    public void Collect()
    {
        if (CaseFile.Add(evidence)) InnerVoice.Say(thought);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Configuração por ferramenta de editor.
    /// </summary>
    public void EditorSetup(Evidence newEvidence, InnerVoiceLine newThought)
    {
        evidence = newEvidence;
        thought = newThought;
    }
#endif
}
