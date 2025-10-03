using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerencia o período seguro do jogo, desabilitando o FlashbackEffectController
/// até que a Track 1 do rádio termine, bloqueando teleportes para flashback
/// e oferecendo ao jogador um início sem pressão.
/// </summary>
public class SafePeriodManager : MonoBehaviour
{
    [Header("Configurações do Período Seguro")]
    [Tooltip("Se o sistema de período seguro deve estar ativo")]
    [SerializeField] private bool enableSafePeriod = true;
    
    [Header("Componentes a Controlar")]
    [Tooltip("FlashbackEffectController que será desabilitado durante o período seguro")]
    [SerializeField] private FlashbackEffectController flashbackEffectController;
    
    [Header("Auto-Discovery")]
    [Tooltip("Se deve buscar automaticamente o FlashbackEffectController na cena")]
    [SerializeField] private bool autoFindFlashbackController = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool safePeriodActive = true;

    // Propriedade pública para verificação externa
    public static bool IsFlashbackAllowed { get; private set; } = false;
    
    // Referência estática para acesso global
    private static SafePeriodManager instance;

    private void Awake()
    {
        // Configura singleton
        if (instance == null)
        {
            instance = this;
            IsFlashbackAllowed = false; // Inicia com flashback bloqueado
        }
        else
        {
            Debug.LogWarning("SafePeriodManager: Múltiplas instâncias detectadas! Destruindo duplicata.");
            Destroy(gameObject);
            return;
        }

        if (autoFindFlashbackController)
        {
            FindFlashbackEffectController();
        }
    }

    private void Start()
    {
        if (enableSafePeriod)
        {
            // Aguarda um frame para garantir que todos os objetos foram inicializados
            StartCoroutine(StartSafePeriodDelayed());
        }
    }

    /// <summary>
    /// Inicia o período seguro com delay para garantir inicialização
    /// </summary>
    private IEnumerator StartSafePeriodDelayed()
    {
        yield return null; // Aguarda um frame
        
        if (showDebugLogs)
        {
            Debug.Log("SafePeriodManager: Iniciando período seguro - FlashbackItems serão desabilitados até Track 1 terminar");
        }
        
        StartSafePeriod();
    }

    private void OnEnable()
    {
        if (enableSafePeriod)
        {
            GameEvents.OnRadioTrack1Completed += EndSafePeriod;
        }
    }

    private void OnDisable()
    {
        if (enableSafePeriod)
        {
            GameEvents.OnRadioTrack1Completed -= EndSafePeriod;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            IsFlashbackAllowed = true; // Libera por segurança quando destruído
        }
    }

    /// <summary>
    /// Encontra automaticamente o FlashbackEffectController na cena
    /// </summary>
    private void FindFlashbackEffectController()
    {
        FlashbackEffectController foundController = FindAnyObjectByType<FlashbackEffectController>();
        flashbackEffectController = foundController;
        
        if (showDebugLogs)
        {
            if (flashbackEffectController != null)
            {
                Debug.Log($"SafePeriodManager: 🔍 BUSCA AUTOMÁTICA - FlashbackEffectController encontrado: '{flashbackEffectController.gameObject.name}' (enabled: {flashbackEffectController.enabled})");
            }
            else
            {
                Debug.LogWarning("SafePeriodManager: ⚠️ NENHUM FlashbackEffectController encontrado na busca automática!");
                Debug.LogWarning("SafePeriodManager: Verifique se há um FlashbackEffectController na cena ou configure manualmente no Inspector");
            }
        }
    }

    /// <summary>
    /// Inicia o período seguro desabilitando o FlashbackEffectController
    /// </summary>
    private void StartSafePeriod()
    {
        if (!safePeriodActive) return;

        // Define estado global para bloquear interações
        IsFlashbackAllowed = false;

        if (flashbackEffectController != null)
        {
            // Força a desabilitação do FlashbackEffectController
            flashbackEffectController.enabled = false;
            
            if (showDebugLogs)
            {
                Debug.Log($"SafePeriodManager: FlashbackEffectController '{flashbackEffectController.gameObject.name}' FORÇADAMENTE desabilitado (enabled = false)");
                Debug.Log($"SafePeriodManager: ✅ PERÍODO SEGURO ATIVO - Flashbacks bloqueados (teleporte + interação) até Track 1 terminar");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("SafePeriodManager: ⚠️ ATENÇÃO - FlashbackEffectController não encontrado!");
                Debug.LogWarning("SafePeriodManager: Verifique se há um FlashbackEffectController na cena ou configure manualmente no Inspector");
            }
        }
    }

    /// <summary>
    /// Termina o período seguro habilitando o FlashbackEffectController
    /// </summary>
    private void EndSafePeriod()
    {
        if (!safePeriodActive) return;

        safePeriodActive = false;
        
        // Libera estado global para permitir interações
        IsFlashbackAllowed = true;

        if (flashbackEffectController != null)
        {
            flashbackEffectController.enabled = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"SafePeriodManager: FlashbackEffectController '{flashbackEffectController.gameObject.name}' HABILITADO (enabled = true)");
                Debug.Log($"SafePeriodManager: 🎯 PERÍODO SEGURO TERMINADO - Flashbacks liberados (teleporte + interação) agora estão disponíveis!");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("SafePeriodManager: ⚠️ FlashbackEffectController não encontrado durante EndSafePeriod!");
            }
        }
    }

    /// <summary>
    /// Configura manualmente o FlashbackEffectController
    /// </summary>
    public void SetFlashbackEffectController(FlashbackEffectController controller)
    {
        flashbackEffectController = controller;
        
        // Se estamos no período seguro, desabilita o controller imediatamente
        if (safePeriodActive && enableSafePeriod && controller != null)
        {
            controller.enabled = false;
        }
    }

    /// <summary>
    /// Força o fim do período seguro (para debug/testes)
    /// </summary>
    [ContextMenu("Forçar Fim do Período Seguro")]
    public void ForceEndSafePeriod()
    {
        EndSafePeriod();
    }

    /// <summary>
    /// Reinicia o período seguro desabilitando o FlashbackEffectController (para debug/testes)
    /// </summary>
    [ContextMenu("Reiniciar Período Seguro")]
    public void RestartSafePeriod()
    {
        safePeriodActive = true;
        StartSafePeriod();
    }
}