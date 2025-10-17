using FMODUnity;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class NeighborEventManager : MonoBehaviour
{
    public static NeighborEventManager Instance { get; private set; }

    [Header("Event Library")]
    [Tooltip("A lista de todos os possíveis eventos do vizinho que podem acontecer.")]
    [SerializeField] private List<NeighborEvent> eventLibrary;

    [Header("Timing Configuration")]
    [Tooltip("Intervalo em segundos entre cada evento (40 segundos conforme especificado).")]
    [SerializeField] private float eventInterval = 40.0f;
    
    [Header("Anti-Repetition System")]
    [Tooltip("Quantos eventos diferentes devem tocar antes de poder repetir o mesmo.")]
    [SerializeField] private int antiRepetitionBuffer = 3;

    [Header("State Management")]
    [Tooltip("Se os eventos do vizinho estão ativos.")]
    [SerializeField] private bool eventsActive = false;

    private Queue<NeighborEventType> recentEvents = new Queue<NeighborEventType>();
    private Coroutine eventRoutine;
    private Coroutine shutdownRoutine;
    private bool isShuttingDown = false;
    private bool hasActiveEvent = false; // Controla se há um evento ativo no momento
    private bool waitingForPeepholeFinalization = false; // Aguardando finalização via olho mágico
    private NeighborEvent? pendingJumpScareEvent = null; // Evento de JumpScare aguardando ativação via olho mágico
    private Dictionary<GameObject, Vector3> originalRotations = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, bool> originalActiveStates = new Dictionary<GameObject, bool>();

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
        // Salva estados originais dos objetos
        SaveOriginalStates();
    }

    /// <summary>
    /// Salva os estados originais de todos os objetos configurados nos eventos
    /// </summary>
    private void SaveOriginalStates()
    {
        foreach (var neighborEvent in eventLibrary)
        {
            // Salva rotações originais
            SaveObjectRotations(neighborEvent.objectsToRotate);
            SaveObjectRotations(neighborEvent.rotationObjects);

            // Salva estados originais das caixas
            SaveObjectStates(neighborEvent.boxesToEnable);

            // Salva estado original do objeto de susto
            if (neighborEvent.jumpScareObject != null)
            {
                SaveObjectState(neighborEvent.jumpScareObject);
            }
        }
    }

    private void SaveObjectRotations(List<GameObject> objects)
    {
        if (objects == null) return;
        
        foreach (var obj in objects)
        {
            if (obj != null && !originalRotations.ContainsKey(obj))
            {
                originalRotations[obj] = obj.transform.eulerAngles;
            }
        }
    }

    private void SaveObjectStates(List<GameObject> objects)
    {
        if (objects == null) return;
        
        foreach (var obj in objects)
        {
            if (obj != null && !originalActiveStates.ContainsKey(obj))
            {
                originalActiveStates[obj] = obj.activeInHierarchy;
            }
        }
    }

    private void SaveObjectState(GameObject obj)
    {
        if (obj != null && !originalActiveStates.ContainsKey(obj))
        {
            originalActiveStates[obj] = obj.activeInHierarchy;
        }
    }

    /// <summary>
    /// Inicia o sistema de eventos do vizinho
    /// Deve ser chamado após o evento de "bater na porta" (pós-rádio)
    /// </summary>
    public void StartNeighborEvents()
    {
        if (eventsActive)
        {
            Debug.LogWarning("[NeighborEventManager] Eventos do vizinho já estão ativos!");
            return;
        }

        eventsActive = true;
        isShuttingDown = false;
        hasActiveEvent = false; // Reset do estado de evento ativo
        waitingForPeepholeFinalization = false; // Reset do estado de aguardo
        pendingJumpScareEvent = null; // Reset de JumpScare pendente
        Debug.Log("[NeighborEventManager] 🏠 Sistema de eventos do vizinho INICIADO");
        
        if (eventRoutine != null)
        {
            StopCoroutine(eventRoutine);
        }
        
        eventRoutine = StartCoroutine(NeighborEventRoutine());
    }

    /// <summary>
    /// Ativa um JumpScare pendente quando o jogador começar a olhar pelo olho mágico
    /// Deve ser chamado quando o jogador iniciar a espiada
    /// </summary>
    public void ActivatePendingJumpScare()
    {
        if (pendingJumpScareEvent.HasValue)
        {
            Debug.Log("[NeighborEventManager] 👻 Jogador olhou pelo olho mágico - JumpScare será executado em 1.5s");
            
            // Inicia coroutine com delay de 1.5 segundos antes de executar
            StartCoroutine(ExecuteJumpScareWithDelay(pendingJumpScareEvent.Value));
            
            // Agora aguarda finalização como qualquer outro evento
            waitingForPeepholeFinalization = true;
            pendingJumpScareEvent = null;
        }
    }

    /// <summary>
    /// Finaliza o evento atual quando o jogador sair do olho mágico
    /// Deve ser chamado quando o jogador sair do olho mágico
    /// </summary>
    public void FinalizeCurrentEvent()
    {
        // Se há um JumpScare pendente, cancela ele
        if (pendingJumpScareEvent.HasValue)
        {
            Debug.Log("[NeighborEventManager] ❌ JumpScare cancelado - Jogador saiu do olho mágico antes de ativar");
            pendingJumpScareEvent = null;
            hasActiveEvent = false;
            // eventsActive deve permanecer TRUE para continuar o loop
            Debug.Log($"[NeighborEventManager] 🔄 JumpScare cancelado - Sistema continua ativo para próximos eventos");
            return;
        }

        if (!waitingForPeepholeFinalization)
        {
            Debug.Log("[NeighborEventManager] Nenhum evento aguardando finalização via olho mágico.");
            return;
        }

        isShuttingDown = true;
        waitingForPeepholeFinalization = false;
        Debug.Log("[NeighborEventManager] 🏠 Jogador saiu do olho mágico - Finalizando evento atual");

        // Para qualquer shutdown anterior
        if (shutdownRoutine != null)
        {
            StopCoroutine(shutdownRoutine);
        }

        // Inicia rotina de finalização com delay de 1 segundo
        shutdownRoutine = StartCoroutine(FinalizeEventWithDelay());
    }

    /// <summary>
    /// Rotina de finalização de evento com delay de 1 segundo
    /// Aguarda 1 segundo antes de restaurar os estados originais e liberar para próximo evento
    /// </summary>
    private IEnumerator FinalizeEventWithDelay()
    {
        Debug.Log("[NeighborEventManager] ⏱️ Aguardando 1 segundo antes de finalizar evento...");
        
        // Aguarda 1 segundo
        yield return new WaitForSeconds(1.0f);
        
        // Agora sim restaura os estados originais
        RestoreOriginalStates();
        
        // Reset das variáveis de controle para liberar próximo evento
        isShuttingDown = false;
        hasActiveEvent = false;
        // NÃO resetar eventsActive - deve continuar true para manter o loop
        shutdownRoutine = null;
        
        Debug.Log("[NeighborEventManager] ✅ Evento finalizado - Sistema liberado para próximo evento");
        Debug.Log($"[NeighborEventManager] 🔄 Estado atual: hasActiveEvent={hasActiveEvent}, eventsActive={eventsActive}");
        Debug.Log($"[NeighborEventManager] ⏰ Timer de {eventInterval}s CONTINUA RODANDO para próximo evento automático");
    }

    /// <summary>
    /// Para o sistema de eventos do vizinho completamente
    /// Usado para parar o sistema inteiro (não apenas finalizar um evento)
    /// </summary>
    public void StopNeighborEvents()
    {
        if (!eventsActive)
        {
            Debug.Log("[NeighborEventManager] Eventos do vizinho já estão inativos.");
            return;
        }

        eventsActive = false;
        isShuttingDown = false;
        hasActiveEvent = false;
        waitingForPeepholeFinalization = false;
        pendingJumpScareEvent = null; // Limpa JumpScare pendente
        
        Debug.Log("[NeighborEventManager] 🏠 Sistema de eventos do vizinho PARADO COMPLETAMENTE");

        if (eventRoutine != null)
        {
            StopCoroutine(eventRoutine);
            eventRoutine = null;
        }

        if (shutdownRoutine != null)
        {
            StopCoroutine(shutdownRoutine);
            shutdownRoutine = null;
        }

        // Restaura todos os objetos ao estado original imediatamente
        RestoreOriginalStates();
    }

    /// <summary>
    /// Restaura todos os objetos ao seu estado original
    /// </summary>
    private void RestoreOriginalStates()
    {
        // Restaura rotações originais
        foreach (var kvp in originalRotations)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.eulerAngles = kvp.Value;
            }
        }

        // Restaura estados ativos originais
        foreach (var kvp in originalActiveStates)
        {
            if (kvp.Key != null)
            {
                kvp.Key.SetActive(kvp.Value);
            }
        }

        Debug.Log("[NeighborEventManager] Estados originais restaurados");
    }

    /// <summary>
    /// Rotina principal dos eventos do vizinho
    /// </summary>
    private IEnumerator NeighborEventRoutine()
    {
        Debug.Log($"[NeighborEventManager] ⏰ Timer iniciado - Próximo evento em {eventInterval} segundos");
        
        while (eventsActive)
        {
            yield return new WaitForSeconds(eventInterval);
            
            if (eventsActive) // Verifica novamente após o delay
            {
                Debug.Log($"[NeighborEventManager] ⏰ Timer disparado - Tentando disparar evento (hasActiveEvent: {hasActiveEvent})");
                TriggerRandomNeighborEvent();
            }
        }
        
        Debug.Log("[NeighborEventManager] ⏰ Timer parado - Sistema inativo");
    }

    /// <summary>
    /// Dispara um evento aleatório do vizinho
    /// </summary>
    private void TriggerRandomNeighborEvent()
    {
        if (!eventsActive || isShuttingDown) 
        {
            return;
        }

        if (hasActiveEvent) 
        {
            Debug.Log("[NeighborEventManager] ⏸️ Evento bloqueado - Há um evento ativo aguardando finalização via olho mágico");
            return;
        }

        // 1. Filtra eventos que NÃO estão no buffer de repetição recente
        List<NeighborEvent> availableEvents = eventLibrary
            .Where(evt => !recentEvents.Contains(evt.type))
            .ToList();

        // 2. Se não há eventos disponíveis, limpa o buffer
        if (availableEvents.Count == 0)
        {
            Debug.Log("[NeighborEventManager] Todos os eventos foram usados recentemente, resetando buffer");
            recentEvents.Clear();
            availableEvents = new List<NeighborEvent>(eventLibrary);
        }

        if (availableEvents.Count == 0)
        {
            Debug.LogWarning("[NeighborEventManager] Nenhum evento configurado na biblioteca!");
            return;
        }

        // 3. Sorteia um evento da lista disponível
        NeighborEvent selectedEvent = availableEvents[Random.Range(0, availableEvents.Count)];

        // 4. Adiciona ao buffer de repetição
        recentEvents.Enqueue(selectedEvent.type);
        if (recentEvents.Count > antiRepetitionBuffer)
        {
            recentEvents.Dequeue();
        }

        Debug.Log($"[NeighborEventManager] 🏠 Evento selecionado: {selectedEvent.eventName} (Tipo: {selectedEvent.type})");
        Debug.Log($"[NeighborEventManager] Buffer atual: [{string.Join(", ", recentEvents)}]");

        // Marca que há um evento ativo ANTES da execução
        hasActiveEvent = true;
        eventsActive = true; // Garante que continue ativo
        
        // Verifica se é um evento de JumpScare
        if (selectedEvent.type == NeighborEventType.JumpScare)
        {
            // JumpScare aguarda o jogador olhar pelo olho mágico para ser ativado
            pendingJumpScareEvent = selectedEvent;
            waitingForPeepholeFinalization = false; // Aguarda ATIVAÇÃO, não finalização
            Debug.Log("[NeighborEventManager] 👻 JumpScare preparado - Aguardando jogador olhar pelo olho mágico para ATIVAR");
        }
        else
        {
            // Outros eventos são executados IMEDIATAMENTE (sem aguardar olho mágico)
            ExecuteNeighborEvent(selectedEvent);
            waitingForPeepholeFinalization = true; // Aguarda finalização quando jogador sair do olho
            Debug.Log("[NeighborEventManager] 🏠 Evento executado AUTOMATICAMENTE - Aguardando jogador usar olho mágico para finalizar");
        }
    }

    /// <summary>
    /// Executa um evento específico do vizinho
    /// </summary>
    private void ExecuteNeighborEvent(NeighborEvent neighborEvent)
    {
        switch (neighborEvent.type)
        {
            case NeighborEventType.RotationWithBoxesAndAudio:
                StartCoroutine(ExecuteRotationWithBoxesAndAudio(neighborEvent));
                break;

            case NeighborEventType.SoundWithRotation:
                StartCoroutine(ExecuteSoundWithRotation(neighborEvent));
                break;

            case NeighborEventType.JumpScare:
                StartCoroutine(ExecuteJumpScare(neighborEvent));
                break;

            case NeighborEventType.AudioOnly:
                StartCoroutine(ExecuteAudioOnly(neighborEvent));
                break;

            default:
                Debug.LogWarning($"[NeighborEventManager] Tipo de evento não implementado: {neighborEvent.type}");
                break;
        }
    }

    /// <summary>
    /// Executa evento de rotação + caixas + áudio
    /// </summary>
    private IEnumerator ExecuteRotationWithBoxesAndAudio(NeighborEvent evt)
    {
        Debug.Log($"[NeighborEventManager] 🔄 Executando: {evt.eventName}");

        // 1. Habilita caixas
        if (evt.boxesToEnable != null)
        {
            foreach (var box in evt.boxesToEnable)
            {
                if (box != null)
                {
                    box.SetActive(true);
                }
            }
        }

        // 2. Toca som aleatório
        if (evt.movingSounds != null && evt.movingSounds.Count > 0 && evt.audioTarget != null)
        {
            var randomSound = evt.movingSounds[Random.Range(0, evt.movingSounds.Count)];
            var eventInstance = RuntimeManager.CreateInstance(randomSound);
            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(evt.audioTarget));
            eventInstance.setVolume(1.0f);
            eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 90.0f);
            eventInstance.start();
            eventInstance.release();
        }

        // 3. Rotaciona objetos
        if (evt.objectsToRotate != null)
        {
            StartCoroutine(RotateObjects(evt.objectsToRotate, evt.rotationAmount, evt.rotationDuration));
        }

        yield return null;
    }

    /// <summary>
    /// Executa evento de som + rotação
    /// </summary>
    private IEnumerator ExecuteSoundWithRotation(NeighborEvent evt)
    {
        Debug.Log($"[NeighborEventManager] 🔊 Executando: {evt.eventName}");

        // 1. Toca som aleatório
        if (evt.randomSounds != null && evt.randomSounds.Count > 0 && evt.soundTarget != null)
        {
            var randomSound = evt.randomSounds[Random.Range(0, evt.randomSounds.Count)];
            var eventInstance = RuntimeManager.CreateInstance(randomSound);
            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(evt.soundTarget));
            eventInstance.setVolume(1.0f);
            eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 90.0f);
            eventInstance.start();
            eventInstance.release();
        }

        // 2. Rotaciona objetos
        if (evt.rotationObjects != null)
        {
            StartCoroutine(RotateObjects(evt.rotationObjects, evt.soundRotationAmount, evt.soundRotationDuration));
        }

        yield return null;
    }

    /// <summary>
    /// Executa evento de susto
    /// </summary>
    private IEnumerator ExecuteJumpScare(NeighborEvent evt)
    {
        Debug.Log($"[NeighborEventManager] 👻 Executando: {evt.eventName}");

        // 1. Toca som de susto (se configurado)
        if (!evt.jumpScareSound.IsNull && evt.jumpScareSoundTarget != null)
        {
            var eventInstance = RuntimeManager.CreateInstance(evt.jumpScareSound);
            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(evt.jumpScareSoundTarget));
            eventInstance.setVolume(1.0f);
            eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 90.0f);
            eventInstance.start();
            eventInstance.release();
        }

        // 2. Habilita objeto por 1 segundo
        if (evt.jumpScareObject != null)
        {
            evt.jumpScareObject.SetActive(true);
            yield return new WaitForSeconds(1.0f);
            evt.jumpScareObject.SetActive(false);
        }
    }

    /// <summary>
    /// Executa JumpScare com delay de 1.5 segundos após jogador iniciar olho mágico
    /// </summary>
    private IEnumerator ExecuteJumpScareWithDelay(NeighborEvent evt)
    {
        Debug.Log("[NeighborEventManager] ⏰ Aguardando 1.5s para executar JumpScare...");
        
        // Aguarda 1.5 segundos
        yield return new WaitForSeconds(1.5f);
        
        Debug.Log("[NeighborEventManager] 👻 Executando JumpScare após delay!");
        
        // Executa o JumpScare
        yield return StartCoroutine(ExecuteJumpScare(evt));
        
        Debug.Log("[NeighborEventManager] 👁️ JumpScare executado - Aguardando jogador sair do olho mágico para finalizar");
    }

    /// <summary>
    /// Executa evento exclusivo de áudio
    /// </summary>
    private IEnumerator ExecuteAudioOnly(NeighborEvent evt)
    {
        Debug.Log($"[NeighborEventManager] 🎵 Executando: {evt.eventName}");

        if (evt.audioOnlyEvents == null || evt.audioOnlyEvents.Count == 0 || evt.audioOnlyTarget == null)
        {
            yield break;
        }

        if (evt.playMultipleSounds)
        {
            // Toca múltiplos sons com delay
            foreach (var audioEvent in evt.audioOnlyEvents)
            {
                if (!audioEvent.IsNull)
                {
                    var eventInstance = RuntimeManager.CreateInstance(audioEvent);
                    eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(evt.audioOnlyTarget));
                    eventInstance.setVolume(1.0f);
                    eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 90.0f);
                    eventInstance.start();
                    eventInstance.release();
                    
                    if (evt.soundDelay > 0)
                    {
                        yield return new WaitForSeconds(evt.soundDelay);
                    }
                }
            }
        }
        else
        {
            // Toca um som aleatório
            var randomAudio = evt.audioOnlyEvents[Random.Range(0, evt.audioOnlyEvents.Count)];
            if (!randomAudio.IsNull)
            {
                var eventInstance = RuntimeManager.CreateInstance(randomAudio);
                eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(evt.audioOnlyTarget));
                eventInstance.setVolume(1.0f);
                eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 90.0f);
                eventInstance.start();
                eventInstance.release();
            }
        }
    }

    /// <summary>
    /// Rotaciona uma lista de objetos
    /// </summary>
    private IEnumerator RotateObjects(List<GameObject> objects, Vector3 rotationAmount, float duration)
    {
        if (objects == null || objects.Count == 0 || duration <= 0) yield break;

        Dictionary<GameObject, Vector3> startRotations = new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, Vector3> targetRotations = new Dictionary<GameObject, Vector3>();

        // Prepara rotações
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                startRotations[obj] = obj.transform.eulerAngles;
                targetRotations[obj] = startRotations[obj] + rotationAmount;
            }
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            
            foreach (var obj in objects)
            {
                if (obj != null && startRotations.ContainsKey(obj))
                {
                    obj.transform.eulerAngles = Vector3.Lerp(startRotations[obj], targetRotations[obj], progress);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Garante que chegue na rotação final
        foreach (var obj in objects)
        {
            if (obj != null && targetRotations.ContainsKey(obj))
            {
                obj.transform.eulerAngles = targetRotations[obj];
            }
        }
    }

    /// <summary>
    /// Propriedades públicas para verificação de estado
    /// </summary>
    public bool AreEventsActive => eventsActive;
    public bool IsShuttingDown => isShuttingDown;
    public bool HasActiveEvent => hasActiveEvent;
    public bool IsWaitingForPeepholeFinalization => waitingForPeepholeFinalization;
    public bool HasPendingJumpScare => pendingJumpScareEvent.HasValue;

    /// <summary>
    /// Força um evento específico para testes (apenas em editor)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceSpecificEvent(NeighborEventType eventType)
    {
        if (!eventsActive || isShuttingDown || hasActiveEvent)
        {
            Debug.LogWarning("[NeighborEventManager] TESTE: Eventos não estão ativos, em shutdown ou há evento ativo");
            return;
        }

        NeighborEvent? eventToTrigger = eventLibrary.FirstOrDefault(evt => evt.type == eventType);
        if (eventToTrigger.HasValue)
        {
            Debug.Log($"[NeighborEventManager] TESTE: Forçando evento {eventType}");
            
            // Adiciona ao buffer mesmo sendo forçado
            recentEvents.Enqueue(eventType);
            if (recentEvents.Count > antiRepetitionBuffer)
            {
                recentEvents.Dequeue();
            }
            
            // Marca que há um evento ativo
            if (eventToTrigger.Value.type == NeighborEventType.JumpScare)
            {
                pendingJumpScareEvent = eventToTrigger.Value;
                hasActiveEvent = true;
                waitingForPeepholeFinalization = false;
                Debug.Log("[NeighborEventManager] TESTE: JumpScare preparado - Use olho mágico para ativar");
            }
            else
            {
                hasActiveEvent = true;
                waitingForPeepholeFinalization = true;
                ExecuteNeighborEvent(eventToTrigger.Value);
            }
        }
        else
        {
            Debug.LogWarning($"[NeighborEventManager] TESTE: Evento {eventType} não encontrado na biblioteca");
        }
    }

    /// <summary>
    /// Força disparo de um evento aleatório para testes (apenas em editor)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceRandomEvent()
    {
        if (!eventsActive || isShuttingDown || hasActiveEvent)
        {
            Debug.LogWarning("[NeighborEventManager] TESTE: Eventos não estão ativos, em shutdown ou há evento ativo");
            return;
        }

        Debug.Log("[NeighborEventManager] TESTE: Forçando evento aleatório...");
        TriggerRandomNeighborEvent();
    }
}