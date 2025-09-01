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
    [Tooltip("Intervalo em segundos entre cada 'rolagem de dados' para um evento.")]
    [SerializeField] private float checkInterval = 10.0f;
    [Tooltip("Chance base de um evento ocorrer, mesmo com insanidade zero. Aumenta a imprevisibilidade.")]
    [Range(0f, 1f)]
    [SerializeField] private float baseChance = 0.05f; // 5% de chance base

    private float currentLatentInsanity = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        InsanityManager.OnLatentInsanityChanged += HandleLatentInsanityChanged;
    }

    private void OnDisable()
    {
        InsanityManager.OnLatentInsanityChanged -= HandleLatentInsanityChanged;
    }

    private void Start()
    {
        StartCoroutine(HorrorCheckRoutine());
    }

    private void HandleLatentInsanityChanged(float newInsanityValue)
    {
        currentLatentInsanity = newInsanityValue;
    }

    private IEnumerator HorrorCheckRoutine()
    {
        // Loop infinito que roda em segundo plano
        while (true)
        {
            // Espera pelo intervalo definido
            yield return new WaitForSeconds(checkInterval);

            // Tenta disparar um evento
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        // A chance aumenta linearmente com a insanidade latente
        float chance = baseChance + currentLatentInsanity * (1f - baseChance);
        Debug.Log($"Tentando disparar evento de horror. Insanidade Latente: {currentLatentInsanity:P0}. Chance: {chance:P0}.");

        // Rola os dados
        if (Random.value < chance)
        {
            TriggerRandomEvent();
        }
    }

    private void TriggerRandomEvent()
    {
        // 1. Filtra a biblioteca para pegar apenas os eventos que podem acontecer AGORA
        List<HorrorEvent> possibleEvents = eventLibrary
            .Where(evt => currentLatentInsanity >= evt.minLatentInsanity)
            .ToList();
        
        if (possibleEvents.Count == 0)
        {
            Debug.Log("Rolagem de dados bem-sucedida, mas nenhum evento disponível para o nível de insanidade atual.");
            return;
        }

        // 2. Sorteia um evento da lista de possibilidades
        HorrorEvent selectedEvent = possibleEvents[Random.Range(0, possibleEvents.Count)];
        
        Debug.Log($"<color=red>DISPARANDO EVENTO DE HORROR: {selectedEvent.eventName}</color>");

        // 3. Dispara o evento correspondente
        switch (selectedEvent.type)
        {
            case HorrorEventType.VisualFlash:
                GameEvents.TriggerVisualFlash(selectedEvent.visualFlashPeak, selectedEvent.visualFlashDuration);
                break;
            
            case HorrorEventType.FalseAlarmClock:
                GameEvents.TriggerFalseAlarm(selectedEvent.falseAlarmDuration);
                break;
        }
    }
}