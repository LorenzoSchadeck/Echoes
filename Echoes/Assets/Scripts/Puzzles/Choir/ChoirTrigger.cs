using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(Collider))]
public class ChoirTrigger : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este trigger pode ser interagido")]
    [SerializeField] private float interactionDistance = 2.5f;
    
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt de interação (ex: PUZZLE_START_PROMPT).")]
    [SerializeField] private LocalizedString interactionPrompt;
    
    public string InteractionPrompt => hasBeenTriggered ? string.Empty : interactionPrompt.GetLocalizedString();
    public float InteractionDistance => interactionDistance;

    private bool hasBeenTriggered = false;

    public bool Interact(Transform interactor)
    {
        if (hasBeenTriggered) return false;

        hasBeenTriggered = true;
        GameEvents.TriggerAudioPuzzleStarted();
        Debug.Log("A caixa emite um som... vozes começam...");
        
        return true;
    }
}