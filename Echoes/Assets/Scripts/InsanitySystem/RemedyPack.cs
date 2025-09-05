using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(Collider))]
public class RemedyPack : MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Referência à chave para o prompt de 'usar remédio', que deve incluir '{count}'.")]
    [SerializeField] private LocalizedString useRemedyPrompt;
    [Tooltip("Referência à chave para o prompt de 'vazio'.")]
    [SerializeField] private LocalizedString emptyPrompt;
    
    [Header("Remedy Settings")]
    [SerializeField] private int remedyCount = 3;

    // Monta a string dinamicamente.
    public string InteractionPrompt
    {
        get
        {
            if (remedyCount > 0)
            {
                // Define o valor da variável '{count}' na string localizada
                useRemedyPrompt.Arguments = new object[] { new { count = this.remedyCount } };
                // Pede ao sistema para gerar a string final
                return useRemedyPrompt.GetLocalizedString();
            }
            else
            {
                // Pega a tradução simples para o estado "vazio"
                return emptyPrompt.GetLocalizedString();
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