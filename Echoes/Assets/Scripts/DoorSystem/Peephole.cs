using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Peephole: MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Distância máxima em que este peephole pode ser interagido")]
    [SerializeField] private float interactionDistance = 1.5f;
    
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt para 'espiar' (ex: PROMPT_PEEK).")]
    [SerializeField] private LocalizedString interactionPrompt;
    
    public string InteractionPrompt => interactionPrompt.GetLocalizedString();
    public float InteractionDistance => interactionDistance;

    [Header("Mechanics")]
    [Tooltip("A quantidade de sanidade (0 a 1) perdida ao começar a espiar.")]
    [SerializeField, Range(0f, 1f)] private float sanityLossAmount = 0.1f;
    
    [Header("Visual Effects")]
    [Tooltip("Intensidade da distorção de lente aplicada durante a espiada.")]
    [SerializeField, Range(0f, 1f)] private float peekLensDistortion = 0.65f;
    [Tooltip("Tempo em segundos para transição suave da distorção de lente.")]
    [SerializeField] private float lensDistortionTransitionTime = 0.5f;

    [Header("Dependencies")]
    [Tooltip("A Câmera Virtual da Cinemachine posicionada na fechadura.")]
    [SerializeField] private CinemachineCamera peekCamera;
    [SerializeField] private PostProcessingManager postProcessingManager;

    private bool isPeeking = false;
    private PlayerInteractor playerInteractor;
    private static WaitForSeconds _blendCamera = new(2f);
    private const float CAMERA_BLEND_TIME = 2f;
    private float originalLensDistortion = 0f;
    private float sanityBasedLensDistortion = 0f; // Valor baseado na sanidade no momento da interação
    private Coroutine lensDistortionCoroutine;

    private void Start()
    {
        playerInteractor = FindAnyObjectByType<PlayerInteractor>();
        
        // Se não foi atribuído no inspector, tenta encontrar automaticamente
        if (postProcessingManager == null)
            postProcessingManager = FindAnyObjectByType<PostProcessingManager>();
    }
    
    private void OnDestroy()
    {
        // Limpa qualquer corrotina ativa ao destruir o objeto
        if (lensDistortionCoroutine != null)
        {
            StopCoroutine(lensDistortionCoroutine);
            lensDistortionCoroutine = null;
        }
        
        // Garantir que o override seja removido se o objeto for destruído durante a espiada
        if (isPeeking && postProcessingManager != null)
        {
            postProcessingManager.RestoreLensDistortionToSanityState();
        }
    }

    private void Update()
    {
        if (isPeeking)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                StopPeeking();
            }
        }
        // CORREÇÃO: Verificação de segurança para garantir que não há override "órfão"
        else if (postProcessingManager != null && postProcessingManager.HasLensDistortionOverride())
        {
            // Se não estamos espiando mas há um override ativo, isso indica um bug - limpa automaticamente
            Debug.LogWarning("Detectado override de distorção órfão, limpando automaticamente...");
            postProcessingManager.RestoreLensDistortionToSanityState();
        }
    }

    public bool Interact(Transform interactor)
    {
        if (!isPeeking)
        {
            StartPeeking();
            return true;
        }

        return false;
    }

    private void StartPeeking()
    {
        isPeeking = true;
        StartCoroutine(StartPeekingSequence());
        GameEvents.TriggerSanityLost(sanityLossAmount);
        playerInteractor?.SetInspectionMode(true);
        if (peekCamera != null) peekCamera.Priority = 2;
    }

    private void StopPeeking()
    {
        isPeeking = false;
        playerInteractor.SetInspectionMode(false);
        if (peekCamera != null) peekCamera.Priority = 0;
        
        // Para qualquer transição de distorção em andamento
        if (lensDistortionCoroutine != null)
        {
            StopCoroutine(lensDistortionCoroutine);
            lensDistortionCoroutine = null;
        }
        
        Debug.Log($"Parando espiada - Distorção atual: {postProcessingManager?.GetCurrentLensDistortionIntensity():F3} → Distorção baseada na sanidade atual: {sanityBasedLensDistortion:F3}");
        
        // CORREÇÃO: Inicia diretamente a transição suave sem forçar limpeza imediata
        // Isso garante interpolação suave mesmo se cancelado durante a entrada
        if (postProcessingManager != null)
        {
            StartCoroutine(RestoreOriginalDistortion());
        }
    }

    private IEnumerator StartPeekingSequence()
    {
        if (postProcessingManager != null)
        {
            // Para qualquer efeito visual ativo
            postProcessingManager.StopAllVisualEffects();
            
            // Armazena o valor atual da distorção de lente (baseado na sanidade atual)
            originalLensDistortion = postProcessingManager.GetCurrentLensDistortionIntensity();
            
            // Armazena também o valor que deveria ser baseado na sanidade para restauração posterior
            sanityBasedLensDistortion = postProcessingManager.GetSanityBasedLensDistortionIntensity();
            
            Debug.Log($"Iniciando espiada - Distorção atual: {originalLensDistortion:F3} → Alvo: {peekLensDistortion:F3} (Duração: {CAMERA_BLEND_TIME}s)");
            
            // Inicia a interpolação da distorção SIMULTANEAMENTE com a transição da câmera
            lensDistortionCoroutine = StartCoroutine(postProcessingManager.InterpolateLensDistortion(
                originalLensDistortion,    // Valor atual (baseado na sanidade)
                peekLensDistortion,        // Valor alvo (distorção do olho mágico)
                CAMERA_BLEND_TIME,         // Duração igual à transição da câmera (2 segundos)
                1f,                        // Scale inicial
                1f                         // Scale alvo
            ));
        }
        
        // Aguarda o fim completo da transição (câmera + distorção)
        yield return _blendCamera;
        
        Debug.Log($"Transição completa - Câmera e distorção de lente sincronizadas: {peekLensDistortion}");
    }
    

    
    /// <summary>
    /// Restaura apenas o EFEITO VISUAL da distorção de lente ao estado baseado na sanidade atual.
    /// IMPORTANTE: Não restaura a sanidade em si - a perda de sanidade é permanente (punição por espiar).
    /// Apenas remove o override visual do peephole e volta ao efeito visual correspondente à sanidade atual.
    /// </summary>
    private IEnumerator RestoreOriginalDistortion()
    {
        if (postProcessingManager == null) yield break;
        
        // Obtém o valor atual da distorção (que pode estar em qualquer estado devido à interrupção)
        float currentLensDistortionValue = postProcessingManager.GetCurrentLensDistortionIntensity();
        
        // Obtém o valor atual da distorção baseado na sanidade (pode ter diminuído mais devido à punição do peephole)  
        float currentSanityBasedValue = postProcessingManager.GetSanityBasedLensDistortionIntensity();
        
        // Para qualquer transição de distorção anterior
        if (lensDistortionCoroutine != null)
        {
            StopCoroutine(lensDistortionCoroutine);
            lensDistortionCoroutine = null;
        }
        
        // Se os valores são muito próximos, não precisa fazer transição
        if (Mathf.Abs(currentLensDistortionValue - currentSanityBasedValue) < 0.01f)
        {
            postProcessingManager.RestoreLensDistortionToSanityState();
            Debug.Log($"Distorção já próxima do valor baseado na sanidade atual, sem necessidade de transição: {currentSanityBasedValue:F3}");
            yield break;
        }
        
        Debug.Log($"Interpolação suave: {currentLensDistortionValue:F3} → {currentSanityBasedValue:F3} (duração: {lensDistortionTransitionTime}s)");
        
        // IMPORTANTE: Garante que há override ativo antes da interpolação
        // Isso permite que a interpolação funcione corretamente mesmo se cancelado durante entrada
        if (!postProcessingManager.HasLensDistortionOverride())
        {
            postProcessingManager.ApplyTemporaryLensDistortion(currentLensDistortionValue);
        }
        
        // Inicia a interpolação suave de volta ao efeito visual baseado na sanidade atual
        lensDistortionCoroutine = StartCoroutine(postProcessingManager.InterpolateLensDistortion(
            currentLensDistortionValue,     // Valor atual (pode estar parcialmente aplicado)
            currentSanityBasedValue,        // Efeito visual baseado na sanidade atual (reflete a punição)
            lensDistortionTransitionTime,   // Duração da transição de saída
            1f,                            // Scale inicial
            1f                             // Scale alvo
        ));
        
        yield return lensDistortionCoroutine;
        
        // Garantia final: restaura o controle da distorção visual ao sistema de sanidade
        postProcessingManager.RestoreLensDistortionToSanityState();
        
        Debug.Log($"Efeito visual da distorção restaurado ao sistema de sanidade (refletindo punição): {currentSanityBasedValue:F3}");
    }
    

}