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
        GameEvents.OnFlashbackStarted += HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded += HandleFlashbackEnded;
    }

    private void OnDisable()
    {
        InsanityManager.OnSanityChanged -= HandleSanityChange;
        GameEvents.OnRemedyUsed -= HandleRemedyUsed;
        GameEvents.OnDeathSequenceCancelled -= HandleRemedyUsed;
        GameEvents.OnFlashbackStarted -= HandleFlashbackStarted;
        GameEvents.OnFlashbackEnded -= HandleFlashbackEnded;
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
        // Durante transição de remédio, ignora mudanças de sanidade para não interferir na transição suave
        if (isRemedyTransitionActive)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ShaderInsanityController] ❌ Sanity change to {newSanity:F2} BLOCKED - remedy transition active");
            }
            return;
        }
        
        currentSanity = newSanity;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] ✅ Sanity changed to {newSanity:F2}");
        }
    }
    
    private void HandleRemedyUsed()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] Remedy used - starting smooth transition");
        }
        
        // IMPORTANTE: Ativa a flag IMEDIATAMENTE para bloquear mudanças de sanidade
        isRemedyTransitionActive = true;
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] 🔒 Remedy transition flag activated - blocking sanity changes");
        }
        
        // Para qualquer transição anterior
        if (remedyTransitionCoroutine != null)
        {
            StopCoroutine(remedyTransitionCoroutine);
        }
        
        // Inicia transição suave para estado limpo
        remedyTransitionCoroutine = StartCoroutine(RemedyTransitionRoutine());
    }
    
    private void HandleFlashbackStarted()
    {
        // Durante flashback, reseta imediatamente para estado limpo
        if (materialInstance != null)
        {
            materialInstance.SetFloat(InsanityLevelID, 0f);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] Flashback started - shader reset to clean state");
        }
    }
    
    private void HandleFlashbackEnded()
    {
        // Quando sai do flashback, força atualização baseada na sanidade atual
        // A sanidade já foi resetada para 1.0 pelo InsanityManager
        if (enableDebugLogs)
        {
            Debug.Log($"[ShaderInsanityController] Flashback ended - resuming normal shader behavior");
        }
    }
    
    private IEnumerator RemedyTransitionRoutine()
    {
        // A flag isRemedyTransitionActive já foi ativada em HandleRemedyUsed
        
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