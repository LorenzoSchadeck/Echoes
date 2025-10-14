using UnityEngine;
using UnityEngine.Localization;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FlashbackItem : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este flashback pode ser interagido")]
    [SerializeField] private float interactionDistance = 2f;
    
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt de interação (ex: PROMPT_REMEMBER).")]
    [SerializeField] private LocalizedString interactionPrompt;
    
    public string InteractionPrompt => CanInteract() ? interactionPrompt.GetLocalizedString() : string.Empty;
    public float InteractionDistance => interactionDistance;
    
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

        Debug.Log($"Interação com {gameObject.name} bem-sucedida. Iniciando flashback com cura de texturas.");  

        isActivated = true;
        
        // CORREÇÃO: Dispara evento de remédio ANTES do flashback para curar texturas
        Debug.Log($"[FlashbackItem] Disparando evento de remédio para curar texturas antes do flashback");
        GameEvents.TriggerRemedyUsed();
        
        // Aguarda um frame para permitir que os sistemas processem o evento de remédio
        StartCoroutine(StartFlashbackAfterRemedyFrame());
        
        isActivated = false;
        
        // this.enabled = false; 

        return true;
    }
    
    /// <summary>
    /// Corrotina que inicia o flashback após um frame, permitindo que o sistema de remédio processe primeiro
    /// </summary>
    private System.Collections.IEnumerator StartFlashbackAfterRemedyFrame()
    {
        // Aguarda um frame para garantir que o evento de remédio seja processado primeiro
        yield return null;
        
        Debug.Log($"[FlashbackItem] Iniciando flashback após processamento do remédio");
        GameEvents.TriggerFlashbackStarted();
    }
}