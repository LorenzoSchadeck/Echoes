using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RemedyPack : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Texto base que aparece quando há remédios disponíveis.")]
    [SerializeField] private string _interactionPrompt = "Usar Remédio";
    [Tooltip("Texto que aparece quando os remédios acabaram.")]
    [SerializeField] private string _emptyPrompt = "Vazio";
    
    [Header("Remedy Settings")]
    [SerializeField] private int remedyCount = 3;

    // Monta a string dinamicamente.
    public string InteractionPrompt
    {
        get
        {
            if (remedyCount > 0)
            {
                // Usa interpolação de string para formatar o texto. Ex: "Usar Remédio (x3)"
                return $"{_interactionPrompt} (x{remedyCount})";
            }
            else
            {
                return _emptyPrompt;
            }
        }
    }

    public bool Interact(Transform interactor)
    {
        if (remedyCount > 0)
        {
            remedyCount--;
            Debug.Log($"Remédio usado do pacote! Restam: {remedyCount}");
            
            GameEvents.TriggerRemedyUsed();
            
            return true;
        }
        else
        {
            Debug.Log("Não há mais remédios neste pacote!");
            return false;
        }
    }
}