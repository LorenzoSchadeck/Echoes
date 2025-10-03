using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(Collider))]
public class FlashbackItem : MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt de interação (ex: PROMPT_REMEMBER).")]
    [SerializeField] private LocalizedString interactionPrompt;
    
    public string InteractionPrompt => CanInteract() ? interactionPrompt.GetLocalizedString() : string.Empty;
    private bool isActivated = false;

    /// <summary>
    /// Verifica se a interação com flashback está permitida
    /// </summary>
    private bool CanInteract()
    {
        // Verifica se ainda não foi ativado e se flashbacks estão permitidos pelo período seguro
        return !isActivated && SafePeriodManager.IsFlashbackAllowed;
    }

    // Método da interface IInteractable
    public bool Interact(Transform interactor)
    {
        if (!CanInteract())
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