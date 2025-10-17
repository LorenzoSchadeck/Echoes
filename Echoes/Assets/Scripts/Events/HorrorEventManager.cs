using FMODUnity;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class HorrorEventManager : MonoBehaviour
{
    public static HorrorEventManager Instance { get; private set; }

    [Header("Event Library")]
    [Tooltip("A lista de todos os possíveis eventos de horror que podem acontecer.")]
    [SerializeField] private List<HorrorEvent> eventLibrary;

    [Header("Trigger Logic")]
    [Tooltip("Intervalo em segundos entre cada rolagem (0 ou 1) para eventos.")]
    [SerializeField] private float checkInterval = 20.0f;
    
    [Header("Anti-Repetition System")]
    [Tooltip("Quantos eventos diferentes devem tocar antes de poder repetir o mesmo.")]
    [SerializeField] private int antiRepetitionBuffer = 3;
    
    private bool isInFlashback = false;
    private Queue<HorrorEventType> recentEvents = new Queue<HorrorEventType>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnFlashbackStarted += OnFlashbackStart;
        GameEvents.OnFlashbackEnded += OnFlashbackEnd;
    }

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= OnFlashbackStart;
        GameEvents.OnFlashbackEnded -= OnFlashbackEnd;
    }

    private void Start()
    {
        StartCoroutine(HorrorCheckRoutine());
    }

    private void OnFlashbackStart()
    {
        isInFlashback = true;
        Debug.Log("[HorrorEventManager] Flashback iniciado - Eventos de horror BLOQUEADOS");
    }

    private void OnFlashbackEnd()
    {
        isInFlashback = false;
        Debug.Log("[HorrorEventManager] Flashback finalizado - Eventos de horror LIBERADOS");
    }
    
    /// <summary>
    /// Verifica se eventos de rádio podem ser executados (rádio deve estar OFF).
    /// </summary>
    private bool CanExecuteRadioEvent(string eventName)
    {
        if (RadioController.Instance != null && !RadioController.Instance.IsRadioOff)
        {
            Debug.Log($"[HorrorEventManager] ❌ Evento {eventName} cancelado - rádio não está desligado (estado necessário: Off)");
            return false;
        }
        return true;
    } 

    private IEnumerator HorrorCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        // 1. BLOQUEIO: Flashback
        if (isInFlashback) 
        {
            Debug.Log("[HorrorEventManager] Tentativa de evento bloqueada - Player está em flashback");
            return;
        }

        // 2. BLOQUEIO: Track 1 do rádio ainda não foi encerrada
        if (RadioController.Instance != null && !RadioController.Instance.HasTrack1Ended)
        {
            Debug.Log("[HorrorEventManager] Tentativa de evento bloqueada - Track 1 do rádio ainda não foi encerrada");
            return;
        }

        // 3. BLOQUEIO: Estado do menu (exceto light flicker que tem chance especial)
        MenuUIManager menuManager = FindFirstObjectByType<MenuUIManager>();
        if (menuManager != null && menuManager.IsInMainMenu)
        {
            // Durante o menu, apenas light flicker pode tocar com chance reduzida
            Debug.Log("[HorrorEventManager] Player está no menu - apenas light flicker permitido com chance especial");
            TryTriggerMenuLightFlicker();
            return;
        }

        // Rolagem com 60% de chance de SIM (0-5) e 40% de NÃO (6-9)
        int roll = Random.Range(0, 10);
        bool eventTriggered = roll <= 5; // 0,1,2,3,4,5 = SIM (60%)
        
        Debug.Log($"[HorrorEventManager] Rolagem de evento: {roll} {(eventTriggered ? "(SIM - Evento será tocado)" : "(NÃO - Nenhum evento)")} [60% chance]");
        
        if (eventTriggered)
        {
            TriggerRandomEvent();
        }
    }
    
    /// <summary>
    /// Tentativa especial de disparar light flicker durante o menu com chance normal
    /// </summary>
    private void TryTriggerMenuLightFlicker()
    {
        // Filtrar apenas eventos de light flicker disponíveis
        List<HorrorEvent> lightFlickerEvents = eventLibrary
            .Where(evt => evt.type == HorrorEventType.LightFlicker && !recentEvents.Contains(evt.type))
            .ToList();
            
        if (lightFlickerEvents.Count == 0)
        {
            Debug.Log("[HorrorEventManager] Menu: Nenhum evento de light flicker disponível (pode estar no buffer)");
            return;
        }
        
        // Rolagem com 60% de chance de SIM (0-5) e 40% de NÃO (6-9)
        int roll = Random.Range(0, 10);
        bool flickerTriggered = roll <= 5; // 0,1,2,3,4,5 = SIM (60%)
        Debug.Log($"[HorrorEventManager] Menu - Rolagem light flicker: {roll} {(flickerTriggered ? "(SIM)" : "(NÃO)")} [60% chance]");
        
        if (flickerTriggered)
        {
            // Seleciona um light flicker aleatório
            HorrorEvent selectedFlicker = lightFlickerEvents[Random.Range(0, lightFlickerEvents.Count)];
            
            // Adiciona ao buffer
            recentEvents.Enqueue(selectedFlicker.type);
            if (recentEvents.Count > antiRepetitionBuffer)
            {
                recentEvents.Dequeue();
            }
            
            Debug.Log($"[HorrorEventManager] Menu: Light flicker selecionado: {selectedFlicker.eventName}");
            
            // Executa apenas o light flicker
            GameEvents.TriggerSimpleLightFlicker(selectedFlicker.flickerLights, selectedFlicker.flickerSoundEvent, selectedFlicker.flickerSoundTarget);
        }
    }

    private void TriggerRandomEvent()
    {
        // Verificação de segurança adicional
        if (isInFlashback) 
        {
            Debug.LogWarning("[HorrorEventManager] AVISO: TriggerRandomEvent chamado durante flashback!");
            return;
        }

        // 1. Filtra eventos que NÃO estão no buffer de repetição recente
        List<HorrorEvent> availableEvents = eventLibrary
            .Where(evt => !recentEvents.Contains(evt.type))
            .ToList();

        // 2. Se não há eventos disponíveis (todos foram usados recentemente), usa todos
        if (availableEvents.Count == 0)
        {
            Debug.Log("[HorrorEventManager] Todos os eventos foram usados recentemente, resetando buffer");
            recentEvents.Clear();
            availableEvents = new List<HorrorEvent>(eventLibrary);
        }

        if (availableEvents.Count == 0) 
        {
            Debug.LogWarning("[HorrorEventManager] Nenhum evento configurado na biblioteca!");
            return;
        }

        // 3. Sorteia um evento da lista disponível
        HorrorEvent selectedEvent = availableEvents[Random.Range(0, availableEvents.Count)];

        // 4. Adiciona ao buffer de repetição
        recentEvents.Enqueue(selectedEvent.type);
        if (recentEvents.Count > antiRepetitionBuffer)
        {
            recentEvents.Dequeue(); // Remove o mais antigo
        }

        Debug.Log($"[HorrorEventManager] Evento selecionado: {selectedEvent.eventName} (Tipo: {selectedEvent.type})");
        Debug.Log($"[HorrorEventManager] Buffer atual: [{string.Join(", ", recentEvents)}]");

        // 5. Executa o evento selecionado
        ExecuteEvent(selectedEvent);
    }

    /// <summary>
    /// Propriedade para verificar se o player está em flashback
    /// </summary>
    public bool IsInFlashback => isInFlashback;

    /// <summary>
    /// Método público para forçar parada de eventos durante situações especiais
    /// </summary>
    public void SetEventsBlocked(bool blocked)
    {
        isInFlashback = blocked;
        Debug.Log($"[HorrorEventManager] Eventos de horror {(blocked ? "BLOQUEADOS" : "LIBERADOS")} manualmente");
    }

    /// <summary>
    /// Verifica se é seguro disparar eventos (sempre true agora que eventos não afetam sanidade)
    /// </summary>
    public bool IsSafeToTriggerEvents()
    {
        // Eventos agora são puramente atmosféricos, sempre é seguro dispará-los
        // A única restrição é não estar em flashback
        return !isInFlashback;
    }

    /// <summary>
    /// Força uma rolagem imediata para testes (ignora o timer de 20s)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceEventRoll()
    {
        Debug.Log("[HorrorEventManager] TESTE: Forçando rolagem de evento...");
        TryTriggerEvent();
    }

    /// <summary>
    /// Força disparo de um evento específico para testes
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceSpecificEvent(HorrorEventType eventType)
    {
        if (isInFlashback)
        {
            Debug.LogWarning("[HorrorEventManager] TESTE: Não é possível forçar evento durante flashback");
            return;
        }

        HorrorEvent? eventToTrigger = eventLibrary.FirstOrDefault(evt => evt.type == eventType);
        if (eventToTrigger.HasValue)
        {
            Debug.Log($"[HorrorEventManager] TESTE: Forçando evento {eventType}");
            
            // Adiciona ao buffer mesmo sendo forçado
            recentEvents.Enqueue(eventType);
            if (recentEvents.Count > antiRepetitionBuffer)
            {
                recentEvents.Dequeue();
            }
            
            ExecuteEvent(eventToTrigger.Value);
        }
        else
        {
            Debug.LogWarning($"[HorrorEventManager] TESTE: Evento {eventType} não encontrado na biblioteca");
        }
    }

    /// <summary>
    /// Executa um evento específico
    /// </summary>
    private void ExecuteEvent(HorrorEvent selectedEvent)
    {
        // 5. Dispara o evento correspondente
        switch (selectedEvent.type)
        {
            // Limiar 2: Ansiedade
            case HorrorEventType.PlaySpatialSound:
                GameEvents.TriggerSpatialSound(selectedEvent.spatialSoundEvent, selectedEvent.spatialSoundTarget);
                break;
            case HorrorEventType.RadioStaticBurst:
                // Verifica se pode executar evento de rádio
                if (!CanExecuteRadioEvent(selectedEvent.eventName))
                    return; // Cancela o evento
                    
                GameEvents.TriggerRadioStatic(selectedEvent.staticBurstEvent, selectedEvent.radioTarget, selectedEvent.staticBurstDuration);
                break;
            case HorrorEventType.LightFlicker:
                GameEvents.TriggerSimpleLightFlicker(selectedEvent.flickerLights, selectedEvent.flickerSoundEvent, selectedEvent.flickerSoundTarget);
                break;

            // Limiar 3: Angústia
            case HorrorEventType.VisualFlash:
                GameEvents.TriggerVisualFlash(1.0f, selectedEvent.visualFlashDuration); // Pico máximo de insanidade
                break;
            case HorrorEventType.FalseAlarmClock:
                GameEvents.TriggerFalseAlarm(selectedEvent.falseAlarmDuration);
                break;

            // Limiar 4: Colapso
            case HorrorEventType.SpawnCoveredBody:
                GameEvents.TriggerSpawnCoveredBody(selectedEvent.coveredBodyPrefab, selectedEvent.coveredBodySpawnPoint);
                break;
            case HorrorEventType.GuiltChorusBurst:
                GameEvents.TriggerDualGuiltChorus(selectedEvent.guiltChorusEvent1, selectedEvent.guiltChorusEvent2, 
                                                 selectedEvent.guiltChorusTarget1, selectedEvent.guiltChorusTarget2);
                break;
        }
    }
}