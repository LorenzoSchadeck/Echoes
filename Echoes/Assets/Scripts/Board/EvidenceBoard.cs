using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// O mural físico no apartamento: interagir abre a tela do mural.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EvidenceBoard : MonoBehaviour, IInteractable
{
    [SerializeField] private EvidenceBoardUI boardUI;
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private LocalizedString interactionPrompt = new LocalizedString();

    public string InteractionPrompt => interactionPrompt.IsEmpty ? string.Empty : interactionPrompt.GetLocalizedString();
    public float InteractionDistance => interactionDistance;

    public bool Interact(Transform interactor)
    {
        if (boardUI == null || EvidenceBoardUI.IsOpen) return false;

        boardUI.Open(interactor.GetComponent<PlayerInteractor>());
        return true;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Configuração por ferramenta de editor.
    /// </summary>
    public void EditorSetup(EvidenceBoardUI ui, LocalizedString prompt)
    {
        boardUI = ui;
        interactionPrompt = prompt;
    }
#endif
}
