using UnityEngine;
using System.Collections;

/// <summary>
/// Gerencia efeitos de Visual Flash usando o InsanityManager para afetar TODOS os materiais com _InsanityLevel.
/// Só funciona se sanidade > 70% e preserva o valor original após o flash.
/// </summary>
public class VisualFlashManager : MonoBehaviour
{
    public static VisualFlashManager Instance { get; private set; }

    [Header("Debug")]
    [Tooltip("Mostra logs detalhados do sistema de visual flash.")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Flash Configuration")]
    [Tooltip("Sanidade mínima necessária para executar visual flash.")]
    [SerializeField] private float minSanityRequired = 0.7f; // 70%
    
    [Tooltip("Duração da transição de volta ao normal (ida é instantânea).")]
    [SerializeField] private float restoreTransitionDuration = 0.3f;

    // Estado interno
    private Coroutine activeFlashCoroutine;
    private float originalSanityValue;
    private InsanityManager insanityManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Encontra o InsanityManager na cena
        insanityManager = FindFirstObjectByType<InsanityManager>();
        if (insanityManager == null)
        {
            Debug.LogError("[VisualFlash] InsanityManager não encontrado na cena!");
        }
    }

    private void OnEnable()
    {
        GameEvents.OnTriggerVisualFlash += OnVisualFlashTriggered;
    }

    private void OnDisable()
    {
        GameEvents.OnTriggerVisualFlash -= OnVisualFlashTriggered;
    }

    private void OnVisualFlashTriggered(float peakInsanity, float duration)
    {
        // Verifica se há InsanityManager disponível
        if (insanityManager == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[VisualFlash] InsanityManager não encontrado! Visual Flash cancelado.");
            return;
        }

        // Verifica se sanidade está acima do mínimo requerido
        float currentSanity = insanityManager.CurrentSanity;
        if (currentSanity <= minSanityRequired)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[VisualFlash] Sanidade muito baixa ({currentSanity:F2}) - necessário > {minSanityRequired:F2}. Visual Flash cancelado.");
            return;
        }

        // Verifica se já há um flash ativo (evita interferência)
        if (activeFlashCoroutine != null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[VisualFlash] Flash já ativo - cancelando novo flash para evitar interferência.");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[VisualFlash] ⚡ Flash triggered - Peak: {peakInsanity}, Duration: {duration}s, Current Sanity: {currentSanity:F2}");

        // Inicia novo flash
        activeFlashCoroutine = StartCoroutine(ExecuteVisualFlash(peakInsanity, duration, currentSanity));
    }

    private IEnumerator ExecuteVisualFlash(float peakInsanity, float duration, float originalSanity)
    {
        // Armazena valor original da sanidade
        originalSanityValue = originalSanity;

        if (enableDebugLogs)
            Debug.Log($"[VisualFlash] Salvando sanidade original: {originalSanityValue:F2}");

        // === FASE 1: FLASH INSTANTÂNEO ===
        // Define sanidade para 0 (máxima insanidade) - isso afeta TODOS os materiais automaticamente
        float targetSanity = 1.0f - peakInsanity; // peakInsanity = 1.0f -> sanidade = 0.0f
        insanityManager.CurrentSanity = targetSanity;
        
        if (enableDebugLogs)
            Debug.Log($"[VisualFlash] Sanidade alterada INSTANTANEAMENTE para: {targetSanity:F2} (todos os materiais com _InsanityLevel afetados)");

        // === FASE 2: MANTÉM O FLASH ===
        float holdTime = duration - restoreTransitionDuration; // Subtrai tempo de restauração
        if (holdTime > 0)
        {
            yield return new WaitForSeconds(holdTime);
        }

        // === FASE 3: RESTAURAÇÃO GRADUAL ===
        yield return StartCoroutine(RestoreSanityGradually(targetSanity, originalSanityValue, restoreTransitionDuration));

        // === LIMPEZA ===
        activeFlashCoroutine = null;
        
        if (enableDebugLogs)
            Debug.Log($"[VisualFlash] Flash concluído - sanidade restaurada para: {originalSanityValue:F2}");
    }

    private IEnumerator RestoreSanityGradually(float fromSanity, float toSanity, float transitionDuration)
    {
        float elapsedTime = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[VisualFlash] Iniciando restauração gradual: {fromSanity:F2} → {toSanity:F2} em {transitionDuration}s");
        
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            // Aplica curva suave como no sistema original
            t = Mathf.SmoothStep(0f, 1f, t);
            
            // Interpola sanidade gradualmente
            float currentSanity = Mathf.Lerp(fromSanity, toSanity, t);
            insanityManager.CurrentSanity = currentSanity;
            
            yield return null;
        }
        
        // Garante valor final preciso
        insanityManager.CurrentSanity = toSanity;
        
        if (enableDebugLogs)
            Debug.Log($"[VisualFlash] Restauração concluída - sanidade final: {toSanity:F2}");
    }

    /// <summary>
    /// Para o flash ativo e restaura sanidade original imediatamente.
    /// </summary>
    public void StopFlash()
    {
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
            activeFlashCoroutine = null;
            
            // Restaura sanidade original instantaneamente
            if (insanityManager != null)
            {
                insanityManager.CurrentSanity = originalSanityValue;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[VisualFlash] Flash interrompido - sanidade restaurada para: {originalSanityValue:F2}");
        }
    }

    /// <summary>
    /// Verifica se há flash ativo no momento.
    /// </summary>
    public bool HasActiveFlash => activeFlashCoroutine != null;

    /// <summary>
    /// Retorna a sanidade original armazenada (para debug).
    /// </summary>
    public float OriginalSanityValue => originalSanityValue;

    private void OnDestroy()
    {
        // Garante que sanidade seja restaurada se o manager for destruído
        StopFlash();
    }

#if UNITY_EDITOR
    [Header("Editor Tools")]
    [SerializeField] private bool showDebugInfo = true;

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 320, 300, 120));
        GUILayout.Label($"Visual Flash Ativo: {HasActiveFlash}");
        GUILayout.Label($"Sanidade Original: {originalSanityValue:F2}");
        GUILayout.Label($"Sanidade Atual: {(insanityManager != null ? insanityManager.CurrentSanity.ToString("F2") : "N/A")}");
        GUILayout.Label($"Sanidade Mín. Requerida: {minSanityRequired:F2}");
        
        if (GUILayout.Button("Stop Flash"))
        {
            StopFlash();
        }
        
        GUILayout.EndArea();
    }
#endif
}