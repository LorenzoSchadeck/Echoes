using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class ShaderInsanityController : MonoBehaviour
{
    [Header("Sanity Thresholds")]
    [Tooltip("A sanidade precisa cair ABAIXO deste valor para que o shader comece a se distorcer.")]
    [SerializeField, Range(0f, 1f)] private float shaderEffectStartThreshold = 0.5f;
    
    [Header("Remedy Settings")]
    [Tooltip("Duração da transição suave quando o remédio é usado.")]
    [SerializeField, Range(1f, 10f)] private float remedyTransitionDuration = 3f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private Material materialInstance;
    private static readonly int InsanityLevelID = Shader.PropertyToID("_InsanityLevel");
    private float currentSanity = 1.0f;
    private float targetInsanityLevel = 0f;
    private float currentInsanityLevel = 0f;
    
    // Controle de transição
    private bool isRemedyTransitionActive = false;
    private Coroutine remedyTransitionCoroutine;

    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        materialInstance = renderer.material; 
    }

    private void OnEnable()
    {
        InsanityManager.OnSanityChanged += HandleSanityChange;
        GameEvents.OnRemedyUsed += HandleRemedyUsed;
        GameEvents.OnDeathSequenceCancelled += HandleRemedyUsed;
    }

    private void OnDisable()
    {
        InsanityManager.OnSanityChanged -= HandleSanityChange;
        GameEvents.OnRemedyUsed -= HandleRemedyUsed;
        GameEvents.OnDeathSequenceCancelled -= HandleRemedyUsed;
    }

    private void Update()
    {
        // Durante transição de remédio, não faz cálculos baseados na sanidade
        if (isRemedyTransitionActive) return;
        
        // Calcula o 'insanityLevel' (0 a 1) para o shader.
        // O efeito vai de 0% a 100% conforme a sanidade cai do limiar até 0.
        targetInsanityLevel = Mathf.InverseLerp(shaderEffectStartThreshold, 0f, currentSanity);
        
        // Aplica o valor suavemente (ou imediatamente se não há transição)
        currentInsanityLevel = targetInsanityLevel;

        if (materialInstance != null)
        {
            materialInstance.SetFloat(InsanityLevelID, currentInsanityLevel);
        }
    }

    // Função chamada pelo evento. Apenas atualiza o valor alvo.
    private void HandleSanityChange(float newSanity)
    {
        currentSanity = newSanity;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] Sanity changed to {newSanity:F2}");
        }
    }
    
    private void HandleRemedyUsed()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] Remedy used - starting smooth transition");
        }
        
        // Para qualquer transição anterior
        if (remedyTransitionCoroutine != null)
        {
            StopCoroutine(remedyTransitionCoroutine);
        }
        
        // Inicia transição suave para estado limpo
        remedyTransitionCoroutine = StartCoroutine(RemedyTransitionRoutine());
    }
    
    private IEnumerator RemedyTransitionRoutine()
    {
        isRemedyTransitionActive = true;
        
        float startInsanityLevel = currentInsanityLevel;
        float targetInsanityLevel = 0f; // Estado limpo
        
        float elapsedTime = 0f;
        
        while (elapsedTime < remedyTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / remedyTransitionDuration;
            
            // Aplica curva suave
            t = Mathf.SmoothStep(0f, 1f, t);
            
            // Interpola suavemente para o estado limpo
            currentInsanityLevel = Mathf.Lerp(startInsanityLevel, targetInsanityLevel, t);
            
            if (materialInstance != null)
            {
                materialInstance.SetFloat(InsanityLevelID, currentInsanityLevel);
            }
            
            yield return null;
        }
        
        // Garante estado final limpo
        currentInsanityLevel = 0f;
        if (materialInstance != null)
        {
            materialInstance.SetFloat(InsanityLevelID, 0f);
        }
        
        isRemedyTransitionActive = false;
        remedyTransitionCoroutine = null;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] Remedy transition completed");
        }
    }

    private void OnDestroy()
    {
        if (remedyTransitionCoroutine != null)
        {
            StopCoroutine(remedyTransitionCoroutine);
        }
        
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}