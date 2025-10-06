using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerencia o período seguro do jogo, controlando a visibilidade dos GameObjects de lembranças
/// até que a Track 2 do rádio termine. Durante o período seguro, mantém ativos os objetos
/// não-interagíveis (lembranças "bloqueadas") e desativa os objetos interagíveis.
/// Após Track 2 terminar, inverte os estados oferecendo ao jogador acesso às lembranças.
/// </summary>
public class SafePeriodManager : MonoBehaviour
{
    [Header("Configurações do Período Seguro")]
    [Tooltip("Se o sistema de período seguro deve estar ativo")]
    [SerializeField] private bool enableSafePeriod = true;
    
    [Header("GameObjects das Lembranças")]
    [Tooltip("GameObjects das lembranças (com script + cosméticos) que serão ATIVADOS quando o SafePeriod terminar")]
    [SerializeField] private GameObject[] memoryGameObjects;
    
    [Tooltip("GameObjects que representam lembranças não-interagíveis que serão DESATIVADOS quando o SafePeriod terminar")]
    [SerializeField] private GameObject[] nonInteractableMemoryObjects;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool safePeriodActive = true;

    // Propriedade pública para verificação externa
    public static bool IsFlashbackAllowed { get; private set; } = false;
    
    // Referência estática para acesso global
    private static SafePeriodManager instance;
    
    // Estados originais dos GameObjects para restauração
    private bool[] originalMemoryObjectsStates;
    private bool[] originalNonInteractableObjectsStates;

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

        // Salva os estados originais dos GameObjects
        SaveOriginalGameObjectStates();
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
            Debug.Log("SafePeriodManager: Iniciando período seguro - FlashbackItems serão desabilitados até Track 2 terminar");
        }
        
        StartSafePeriod();
    }

    private void OnEnable()
    {
        if (enableSafePeriod)
        {
            // Mudança no fluxo: agora escuta Track 2 ao invés de Track 1
            GameEvents.OnRadioTrack2Completed += EndSafePeriod;
        }
    }

    private void OnDisable()
    {
        if (enableSafePeriod)
        {
            GameEvents.OnRadioTrack2Completed -= EndSafePeriod;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            IsFlashbackAllowed = true; // Libera por segurança quando destruído
            
            // Restaura estados originais dos GameObjects por segurança
            RestoreOriginalGameObjectStates();
        }
    }



    /// <summary>
    /// Salva os estados originais dos GameObjects para restauração posterior
    /// </summary>
    private void SaveOriginalGameObjectStates()
    {
        // Salva estados dos GameObjects de lembranças interagíveis
        if (memoryGameObjects != null)
        {
            originalMemoryObjectsStates = new bool[memoryGameObjects.Length];
            for (int i = 0; i < memoryGameObjects.Length; i++)
            {
                if (memoryGameObjects[i] != null)
                {
                    originalMemoryObjectsStates[i] = memoryGameObjects[i].activeInHierarchy;
                }
            }
        }

        // Salva estados dos GameObjects de lembranças não-interagíveis
        if (nonInteractableMemoryObjects != null)
        {
            originalNonInteractableObjectsStates = new bool[nonInteractableMemoryObjects.Length];
            for (int i = 0; i < nonInteractableMemoryObjects.Length; i++)
            {
                if (nonInteractableMemoryObjects[i] != null)
                {
                    originalNonInteractableObjectsStates[i] = nonInteractableMemoryObjects[i].activeInHierarchy;
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"SafePeriodManager: Estados originais salvos - {originalMemoryObjectsStates?.Length ?? 0} objetos de lembrança e {originalNonInteractableObjectsStates?.Length ?? 0} objetos não-interagíveis");
        }
    }

    /// <summary>
    /// Inicia o período seguro configurando a visibilidade dos GameObjects de lembranças
    /// </summary>
    private void StartSafePeriod()
    {
        if (!safePeriodActive) return;

        // Define estado global para bloquear interações
        IsFlashbackAllowed = false;

        // DESATIVA os GameObjects de lembranças interagíveis (ficam ocultos durante SafePeriod)
        SetMemoryGameObjectsActive(false);
        
        // MANTÉM ATIVOS os GameObjects não-interagíveis (representam lembranças bloqueadas)
        SetNonInteractableMemoryObjectsActive(true);
        
        if (showDebugLogs)
        {
            Debug.Log($"SafePeriodManager: ✅ PERÍODO SEGURO ATIVO - Lembranças interagíveis DESATIVADAS e não-interagíveis ATIVAS até Track 2 terminar");
        }
    }

    /// <summary>
    /// Termina o período seguro liberando as lembranças interagíveis
    /// </summary>
    private void EndSafePeriod()
    {
        if (!safePeriodActive) return;

        safePeriodActive = false;
        
        // Libera estado global para permitir interações
        IsFlashbackAllowed = true;

        // ATIVA os GameObjects de lembranças interagíveis (tornam-se disponíveis)
        SetMemoryGameObjectsActive(true);
        
        // DESATIVA os GameObjects não-interagíveis (não são mais necessários)
        SetNonInteractableMemoryObjectsActive(false);
        
        if (showDebugLogs)
        {
            Debug.Log($"SafePeriodManager: 🎯 PERÍODO SEGURO TERMINADO - Lembranças interagíveis ATIVADAS e não-interagíveis DESATIVADAS!");
        }
    }

    /// <summary>
    /// Configura manualmente os GameObjects de lembranças
    /// </summary>
    public void SetMemoryGameObjects(GameObject[] memoryObjects, GameObject[] nonInteractableObjects)
    {
        memoryGameObjects = memoryObjects;
        nonInteractableMemoryObjects = nonInteractableObjects;
        
        // Salva novos estados originais
        SaveOriginalGameObjectStates();
        
        // Se estamos no período seguro, aplica as configurações imediatamente
        if (safePeriodActive && enableSafePeriod)
        {
            SetMemoryGameObjectsActive(false);
            SetNonInteractableMemoryObjectsActive(true);
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

    #region GameObject Control Methods

    /// <summary>
    /// Ativa/desativa os GameObjects de lembranças interagíveis
    /// </summary>
    private void SetMemoryGameObjectsActive(bool active)
    {
        if (memoryGameObjects == null) return;

        for (int i = 0; i < memoryGameObjects.Length; i++)
        {
            if (memoryGameObjects[i] != null)
            {
                memoryGameObjects[i].SetActive(active);
                
                if (showDebugLogs)
                {
                    Debug.Log($"SafePeriodManager: GameObject de lembrança '{memoryGameObjects[i].name}' definido como {(active ? "ATIVO" : "INATIVO")}");
                }
            }
        }
    }

    /// <summary>
    /// Ativa/desativa os GameObjects de lembranças não-interagíveis
    /// </summary>
    private void SetNonInteractableMemoryObjectsActive(bool active)
    {
        if (nonInteractableMemoryObjects == null) return;

        for (int i = 0; i < nonInteractableMemoryObjects.Length; i++)
        {
            if (nonInteractableMemoryObjects[i] != null)
            {
                nonInteractableMemoryObjects[i].SetActive(active);
                
                if (showDebugLogs)
                {
                    Debug.Log($"SafePeriodManager: GameObject não-interagível '{nonInteractableMemoryObjects[i].name}' definido como {(active ? "ATIVO" : "INATIVO")}");
                }
            }
        }
    }

    /// <summary>
    /// Restaura os estados originais de todos os GameObjects controlados
    /// </summary>
    private void RestoreOriginalGameObjectStates()
    {
        // Restaura estados dos GameObjects de lembranças interagíveis
        if (memoryGameObjects != null && originalMemoryObjectsStates != null)
        {
            for (int i = 0; i < memoryGameObjects.Length && i < originalMemoryObjectsStates.Length; i++)
            {
                if (memoryGameObjects[i] != null)
                {
                    memoryGameObjects[i].SetActive(originalMemoryObjectsStates[i]);
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"SafePeriodManager: Estado original restaurado para '{memoryGameObjects[i].name}': {originalMemoryObjectsStates[i]}");
                    }
                }
            }
        }

        // Restaura estados dos GameObjects não-interagíveis
        if (nonInteractableMemoryObjects != null && originalNonInteractableObjectsStates != null)
        {
            for (int i = 0; i < nonInteractableMemoryObjects.Length && i < originalNonInteractableObjectsStates.Length; i++)
            {
                if (nonInteractableMemoryObjects[i] != null)
                {
                    nonInteractableMemoryObjects[i].SetActive(originalNonInteractableObjectsStates[i]);
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"SafePeriodManager: Estado original restaurado para '{nonInteractableMemoryObjects[i].name}': {originalNonInteractableObjectsStates[i]}");
                    }
                }
            }
        }
    }

    #endregion
}