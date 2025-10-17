using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

/// <summary>
/// Exemplo de configuração para o sistema de eventos do vizinho.
/// Este script demonstra como configurar cada tipo de evento no Inspector.
/// </summary>
[RequireComponent(typeof(NeighborEventManager))]
public class NeighborEventSetup : MonoBehaviour
{
    [Header("Configuração de Exemplo - Sistema de Eventos do Vizinho")]
    [Space(10)]
    
    [Header("🔄 Objetos para Rotação")]
    [Tooltip("Porta principal que será rotacionada nos eventos")]
    [SerializeField] private GameObject mainDoor;
    
    [Header("📦 Caixas de Mudança")]
    [Tooltip("Lista de caixas que serão habilitadas para simular mudança")]
    [SerializeField] private List<GameObject> movingBoxes;
    
    [Header("👻 Objeto de Susto")]
    [Tooltip("Objeto que aparecerá por 1 segundo no evento de susto")]
    [SerializeField] private GameObject jumpScareObject;
    
    [Header("🔊 Posições de Áudio")]
    [Tooltip("GameObject onde os sons do vizinho serão reproduzidos")]
    [SerializeField] private GameObject neighborAudioSource;
    
    [Header("🎵 Eventos FMOD")]
    [Tooltip("Sons de mudança (arrastar móveis, caixas, etc.)")]
    [SerializeField] private List<EventReference> movingSounds;
    
    [Tooltip("Sons diversos do vizinho")]
    [SerializeField] private List<EventReference> neighborSounds;
    
    [Tooltip("Som de susto")]
    [SerializeField] private EventReference scareSound;
    
    [Tooltip("Sons exclusivos de áudio")]
    [SerializeField] private List<EventReference> audioOnlySounds;

    private void Start()
    {
        ConfigureExampleEvents();
    }

    /// <summary>
    /// Configura eventos de exemplo no NeighborEventManager
    /// </summary>
    private void ConfigureExampleEvents()
    {
        var neighborEventManager = GetComponent<NeighborEventManager>();
        
        if (neighborEventManager == null)
        {
            Debug.LogError("[NeighborEventSetup] NeighborEventManager não encontrado!");
            return;
        }

        Debug.Log("[NeighborEventSetup] 🏠 Sistema de eventos do vizinho configurado com eventos de exemplo");
        Debug.Log("Certifique-se de configurar os eventos diretamente no Inspector do NeighborEventManager");
        
        // Lista de exemplo de como os eventos devem ser configurados:
        Debug.Log(@"
📋 CONFIGURAÇÃO DE EVENTOS DE EXEMPLO:

1. 🔄 RotationWithBoxesAndAudio:
   - objectsToRotate: [Porta Principal]
   - rotationAmount: (0, 15, 0) graus
   - rotationDuration: 2.0 segundos
   - boxesToEnable: [Caixas de Mudança]
   - movingSounds: [Sons de Arrastar, Sons de Caixas]
   - audioTarget: [Fonte de Áudio do Vizinho]

2. 🔊 SoundWithRotation:
   - randomSounds: [Sons Diversos do Vizinho]
   - soundTarget: [Fonte de Áudio do Vizinho]
   - rotationObjects: [Porta Principal]
   - soundRotationAmount: (0, 10, 0) graus
   - soundRotationDuration: 1.5 segundos

3. 👻 JumpScare:
   - jumpScareObject: [Objeto de Susto]
   - jumpScareSound: [Som de Susto]
   - jumpScareSoundTarget: [Fonte de Áudio do Vizinho]

4. 🎵 AudioOnly:
   - audioOnlyEvents: [Sons Exclusivos]
   - audioOnlyTarget: [Fonte de Áudio do Vizinho]
   - playMultipleSounds: false (um som aleatório)
   - soundDelay: 0.5 segundos (se múltiplos sons)
        ");
    }

    /// <summary>
    /// Método para testar eventos manualmente (apenas em modo de desenvolvimento)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Update()
    {
        if (Application.isPlaying && NeighborEventManager.Instance != null)
        {
            // Teclas de teste para desenvolvedor
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                NeighborEventManager.Instance.ForceSpecificEvent(NeighborEventType.RotationWithBoxesAndAudio);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                NeighborEventManager.Instance.ForceSpecificEvent(NeighborEventType.SoundWithRotation);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                NeighborEventManager.Instance.ForceSpecificEvent(NeighborEventType.JumpScare);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                NeighborEventManager.Instance.ForceSpecificEvent(NeighborEventType.AudioOnly);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                NeighborEventManager.Instance.ForceRandomEvent();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                NeighborEventManager.Instance.StartNeighborEvents();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                NeighborEventManager.Instance.StopNeighborEvents();
            }
        }
    }

    private void OnValidate()
    {
        // Validação básica dos objetos necessários
        if (mainDoor == null)
            Debug.LogWarning("[NeighborEventSetup] Porta principal não configurada!");
            
        if (movingBoxes == null || movingBoxes.Count == 0)
            Debug.LogWarning("[NeighborEventSetup] Caixas de mudança não configuradas!");
            
        if (jumpScareObject == null)
            Debug.LogWarning("[NeighborEventSetup] Objeto de susto não configurado!");
            
        if (neighborAudioSource == null)
            Debug.LogWarning("[NeighborEventSetup] Fonte de áudio do vizinho não configurada!");
    }
}