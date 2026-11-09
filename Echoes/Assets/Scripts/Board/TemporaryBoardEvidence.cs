using UnityEngine;

/// <summary>Papel de teste para coletar uma pista sem depender da progressão narrativa.</summary>
[RequireComponent(typeof(Collider))]
public class TemporaryBoardEvidence : MonoBehaviour, IInteractable
{
    [SerializeField] private Evidence evidence;
    [SerializeField] private float interactionDistance = 6f;

    public float InteractionDistance => interactionDistance;
    public string InteractionPrompt => evidence == null ? string.Empty :
        CaseFile.Has(evidence) ? MenuText.Get("M_BOARD_TEST_COLLECTED") :
        MenuText.Get("M_BOARD_TEST_COLLECT").Replace("{clue}", evidence.title.GetLocalizedString());

    public bool Interact(Transform interactor)
    {
        CaseFile.RegisterVisualSource(evidence, gameObject);
        return evidence != null && CaseFile.Add(evidence);
    }

#if UNITY_EDITOR
    public void EditorSetup(Evidence clue) => evidence = clue;
#endif
}
