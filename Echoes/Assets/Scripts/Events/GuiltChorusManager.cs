using UnityEngine;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gerencia o sistema de Coro da Culpa para eventos de horror no Limiar 4.
/// Reproduz áudio assombrado em localizações específicas da cena para criar terror psicológico.
/// </summary>
public class GuiltChorusManager : MonoBehaviour
{
    public static GuiltChorusManager Instance { get; private set; }

    [Header("Chorus Settings")]
    [Tooltip("Volume global do Coro da Culpa (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float chorusVolume = 0.8f;
    
    [Tooltip("Distância máxima para ouvir o coro (padronizada igual ao rádio).")]
    [SerializeField] private float maxAudibleDistance = 70f; // Padronizado igual ao rádio
    
    [Tooltip("Se deve escolher múltiplas localizações simultaneamente.")]
    [SerializeField] private bool allowMultipleLocations = false;
    
    [Tooltip("Número máximo de localizações simultâneas se múltiplas forem permitidas.")]
    [SerializeField] private int maxSimultaneousLocations = 3;

    // [Header("Sanity Integration")] - DESABILITADO: Eventos não afetam mais sanidade
    // [Tooltip("Sanidade perdida quando o coro da culpa é ativado.")]
    // [Range(0f, 0.1f)]
    // [SerializeField] private float sanityCostOnActivation = 0.04f;
    
    // [Tooltip("Sanidade perdida por segundo enquanto o coro está ativo.")]
    // [Range(0f, 0.02f)]
    // [SerializeField] private float sanityCostPerSecond = 0.005f;

    [Header("Debug")]
    [Tooltip("Mostra logs detalhados do sistema de coro da culpa.")]
    [SerializeField] private bool enableDebugLogs = true;

    // Estado interno
    private List<ChorusPlaybackInfo> activePlaybacks = new List<ChorusPlaybackInfo>();
    private Coroutine sanityCostCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Eventos agora são tratados diretamente via FMOD no GameEvents
    // Este manager pode ser usado para funcionalidades avançadas futuras

    // Método removido - funcionalidade substituída pelo sistema dual no GameEvents

    // Métodos complexos removidos - funcionalidade agora é tratada diretamente no GameEvents
    // Este manager mantém apenas funcionalidades básicas para compatibilidade

    /// <summary>
    /// Para imediatamente todos os coros ativos.
    /// </summary>
    public void StopAllChorus()
    {
        StopAllCoroutines();
        activePlaybacks.Clear();
        
        if (enableDebugLogs)
            Debug.Log("Todos os Coros da Culpa foram interrompidos.");
    }

    /// <summary>
    /// Verifica se há coros ativos no momento.
    /// </summary>
    public bool HasActiveChorus => activePlaybacks.Count > 0;

    /// <summary>
    /// Retorna o número de coros ativos.
    /// </summary>
    public int ActiveChorusCount => activePlaybacks.Count;

    /// <summary>
    /// Método público para triggerar o coro manualmente.
    /// Agora delega para o sistema dual do GameEvents.
    /// </summary>
    public void TriggerGuiltChorus(EventReference chorusEvent1, EventReference chorusEvent2, 
                                  GameObject target1, GameObject target2)
    {
        GameEvents.TriggerDualGuiltChorus(chorusEvent1, chorusEvent2, target1, target2);
    }

    private void OnDestroy()
    {
        // Garante limpeza se o manager for destruído
        StopAllCoroutines();
        activePlaybacks.Clear();
    }

#if UNITY_EDITOR
    [Header("Editor Tools")]
    [SerializeField] private bool showDebugInfo = true;

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 580, 300, 140));
        GUILayout.Label($"Coros Ativos: {activePlaybacks.Count}");
        GUILayout.Label($"Volume: {chorusVolume:F2}");
        GUILayout.Label($"Múltiplas Localizações: {allowMultipleLocations}");
        
        if (GUILayout.Button("Stop All Chorus"))
        {
            StopAllChorus();
        }
        
        if (GUILayout.Button("Test Chorus (Need Setup)"))
        {
            if (enableDebugLogs)
                Debug.Log("Configure evento FMOD e localizações para testar!");
        }
        
        GUILayout.EndArea();
    }
#endif
}

/// <summary>
/// Estrutura para armazenar informações sobre uma reprodução de coro ativa.
/// </summary>
[System.Serializable]
public struct ChorusPlaybackInfo
{
    public FMOD.Studio.EventInstance eventInstance;
    public Transform location;
    public EventReference eventReference;
}