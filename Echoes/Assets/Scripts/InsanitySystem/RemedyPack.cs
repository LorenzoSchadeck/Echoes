using UnityEngine;
using UnityEngine.Localization;
using FMODUnity;

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

    [Header("🔊 Audio Settings")]
    [Tooltip("Evento FMOD tocado quando o remédio é usado")]
    [SerializeField] private EventReference remedyUseSoundEvent;



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
            // Toca o som de uso do remédio
            PlayRemedyUseSound();
            
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
    
    /// <summary>
    /// Toca o som de uso do remédio como áudio 2D (não espacial)
    /// </summary>
    private void PlayRemedyUseSound()
    {
        if (remedyUseSoundEvent.IsNull) return;
        
        try
        {
            // Cria uma instância 2D do evento FMOD (não espacial)
            var remedySoundInstance = RuntimeManager.CreateInstance(remedyUseSoundEvent);
            
            // Inicia o som imediatamente (sem posicionamento 3D)
            remedySoundInstance.start();
            
            // Libera a instância automaticamente após tocar
            remedySoundInstance.release();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[RemedyPack] {name}: Erro ao tocar som de uso do remédio: {e.Message}");
        }
    }
}