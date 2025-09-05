using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(Collider))]
public class FlashbackItem : MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt de interação (ex: PROMPT_REMEMBER).")]
    [SerializeField] private LocalizedString interactionPrompt;
    
    public string InteractionPrompt => interactionPrompt.GetLocalizedString();
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