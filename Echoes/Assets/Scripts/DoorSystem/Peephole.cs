using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Peephole: MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt para 'espiar' (ex: PROMPT_PEEK).")]
    [SerializeField] private LocalizedString interactionPrompt;
    public string InteractionPrompt => interactionPrompt.GetLocalizedString();

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
        StartCoroutine(InProgressPanelActivated());
        GameEvents.TriggerSanityLost(sanityLossAmount);
        playerInteractor?.SetInspectionMode(true);
        if (peekCamera != null) peekCamera.Priority = 2;
    }

    private void StopPeeking()
    {
        isPeeking = false;
        playerInteractor.SetInspectionMode(false);
        if (peekCamera != null) peekCamera.Priority = 0;
        
        // Restaura a distorção de lente original
        if (postProcessingManager != null)
        {
            StartCoroutine(RestoreOriginalDistortion());
        }
    }

    IEnumerator InProgressPanelActivated()
    {
        // Aguarda o fim completo da transição de câmera (2 segundos)
        yield return _blendCamera;
        
        if (postProcessingManager != null)
        {
            // Armazena o valor original da distorção de lente
            originalLensDistortion = postProcessingManager.GetSaneProfileLensDistortionScale();
            
            // Aplica a distorção de espiada APÓS a transição completa da câmera
            StartCoroutine(ApplyPeekDistortion());
        }
    }
    
    private IEnumerator ApplyPeekDistortion()
    {
        if (postProcessingManager != null)
        {
            // Para qualquer efeito visual ativo
            postProcessingManager.StopAllVisualEffects();
            
            // Aplica a distorção de lente personalizada para a espiada
            // Aqui você pode implementar uma transição suave se desejar
            // Por enquanto, aplica instantaneamente
            StartCoroutine(SetLensDistortionEffect(peekLensDistortion));
        }
        yield return null;
    }
    
    private IEnumerator SetLensDistortionEffect(float targetDistortion)
    {
        if (postProcessingManager == null) yield break;
        
        // Para qualquer transição de distorção anterior
        if (lensDistortionCoroutine != null)
        {
            StopCoroutine(lensDistortionCoroutine);
        }
        
        // Inicia a transição suave para a distorção alvo
        lensDistortionCoroutine = StartCoroutine(SmoothLensDistortionTransition(originalLensDistortion, targetDistortion, lensDistortionTransitionTime));
        
        yield return lensDistortionCoroutine;
        
        Debug.Log($"Distorção de lente aplicada e mantida ativa: {targetDistortion}");
    }
    
    private IEnumerator RestoreOriginalDistortion()
    {
        if (postProcessingManager == null) yield break;
        
        // Para qualquer transição de distorção anterior
        if (lensDistortionCoroutine != null)
        {
            StopCoroutine(lensDistortionCoroutine);
        }
        
        // Inicia a transição suave de volta ao estado baseado na sanidade
        lensDistortionCoroutine = StartCoroutine(SmoothLensDistortionTransition(peekLensDistortion, originalLensDistortion, lensDistortionTransitionTime));
        
        yield return lensDistortionCoroutine;
        
        // Restaura completamente ao sistema de sanidade
        postProcessingManager.RestoreLensDistortionToSanityState();
        
        Debug.Log($"Distorção de lente restaurada ao sistema de sanidade");
    }
    
    private IEnumerator SmoothLensDistortionTransition(float startValue, float targetValue, float duration)
    {
        if (postProcessingManager == null || duration <= 0f)
        {
            yield break;
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Usa uma curva suave para a transição (ease-in-out)
            t = Mathf.SmoothStep(0f, 1f, t);
            
            float currentDistortion = Mathf.Lerp(startValue, targetValue, t);
            
            // Aplica a distorção usando o método público que criaremos
            // Por enquanto, usamos um método placeholder
            ApplyLensDistortionValue(currentDistortion);
            
            yield return null;
        }
        
        // Garante o valor final exato
        ApplyLensDistortionValue(targetValue);
        
        lensDistortionCoroutine = null;
    }
    
    private void ApplyLensDistortionValue(float distortionValue)
    {
        if (postProcessingManager != null)
        {
            // Aplica a distorção temporária usando o método público do PostProcessingManager
            postProcessingManager.ApplyTemporaryLensDistortion(distortionValue);
        }
    }
}