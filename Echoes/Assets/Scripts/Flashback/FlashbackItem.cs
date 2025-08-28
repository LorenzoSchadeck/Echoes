using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FlashbackItem : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Texto que aparece quando o jogador olha para este item.")]
    [SerializeField] private string _interactionPrompt = "Lembrar";

    // Propriedade da interface IInteractable
    public string InteractionPrompt => _interactionPrompt;
    private bool isActivated = false;

    // Método da interface IInteractable
    public bool Interact(Transform interactor)
    {
        if (isActivated)
        {
            return false;
        }

        Debug.Log($"Interação com {gameObject.name} bem-sucedida. Iniciando flashback sem retorno automático.");  

        isActivated = true;
        GameEvents.TriggerFlashbackStarted();
        isActivated = false;
        
        // this.enabled = false; 

        return true;
    }
}