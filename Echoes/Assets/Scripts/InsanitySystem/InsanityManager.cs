using System;
using System.Collections;
using UnityEngine;

public class InsanityManager : MonoBehaviour
{
    public static event Action<float> OnInsanityChanged;

    [Header("Insanity Settings")]
    [Tooltip("O nível de insanidade atual do jogador, de 0 (são) a 1 (insano).")]
    [SerializeField, Range(0f, 1f)] private float currentInsanity = 0f;

    [Tooltip("Quanto a insanidade aumenta por segundo. Ex: 0.01 significa que levará 100 segundos para ir de 0 a 1.")]
    [SerializeField] private float normalInsanityRate = 0.005f;
    [SerializeField] private float flashbackInsanityRate = 0.05f;

    [Header("Death Settings")]
    [Tooltip("Tempo em segundos que o jogador pode permanecer em insanidade máxima antes de morrer.")]
    [SerializeField] private float timeAtMaxInsanityBeforeDeath = 10f;
    private float maxInsanityTimer = 0f;
    private bool isPlayerDead = false;
    private bool isDeathSequenceActive = false;

    [Header("Remedy Settings")]
    [Tooltip("Duração em segundos para a insanidade ir a zero após usar um remédio.")]
    [SerializeField] private float remedyEffectDuration = 3.0f;
    [Tooltip("Duração em segundos que o aumento de insanidade fica pausado após o efeito do remédio.")]
    [SerializeField] private float insanityPauseDuration = 15.0f;
    private bool isInsanityPaused = false;
    private bool isInFlashback = false;

    private float previousInsanity;
    private float currentPassiveInsanityRate;
    private Coroutine remedyCoroutine;

    public float CurrentInsanity
    {
        get => currentInsanity;
        set
        {
            float newInsanity = Mathf.Clamp01(value);
            if (Mathf.Approximately(currentInsanity, newInsanity)) return;

            currentInsanity = newInsanity;
            Debug.Log($"Insanity changed to: {currentInsanity} (via code)");
            OnInsanityChanged?.Invoke(currentInsanity);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnFlashbackStarted += StartFlashbackState;
        GameEvents.OnFlashbackEnded += EndFlashbackState;
        GameEvents.OnRemedyUsed += UseRemedy;
    }

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= StartFlashbackState;
        GameEvents.OnFlashbackEnded -= EndFlashbackState;
        GameEvents.OnRemedyUsed -= UseRemedy;
    }

    private void Start()
    {
        currentPassiveInsanityRate = normalInsanityRate;
        UpdateInsanityAndDispatchEvent(0f);
    }

    private void Update()
    {
        if (isPlayerDead) return;

        if (currentInsanity < 1f && !isInsanityPaused) 
        {
            currentInsanity += currentPassiveInsanityRate * Time.deltaTime;
        }

        if (Mathf.Approximately(currentInsanity, 1f))
        {
            // Se a contagem está começando agora, dispara o evento
            if (!isDeathSequenceActive)
            {
                isDeathSequenceActive = true;
                GameEvents.TriggerDeathSequenceStarted(timeAtMaxInsanityBeforeDeath);
            }

            // Inicia ou continua o cronômetro de morte
            maxInsanityTimer += Time.deltaTime;
            Debug.Log($"Tempo em insanidade máxima: {maxInsanityTimer:F1}s / {timeAtMaxInsanityBeforeDeath}s");

            // Verifica se o tempo limite foi atingido
            if (maxInsanityTimer >= timeAtMaxInsanityBeforeDeath)
            {
                Die();
            }
        }
        else
        {
            // Se a insanidade diminuiu e a sequência estava ativa, dispara o evento de cancelamento
            if (isDeathSequenceActive)
            {
                isDeathSequenceActive = false;
                GameEvents.TriggerDeathSequenceCancelled();
            }
            maxInsanityTimer = 0f;
        }

        UpdateInsanityAndDispatchEvent(currentInsanity);
    }

    private void UpdateInsanityAndDispatchEvent(float newInsanityValue)
    {
        currentInsanity = Mathf.Clamp01(newInsanityValue);
        if (!Mathf.Approximately(previousInsanity, currentInsanity))
        {
            Debug.Log($"Insanity changed to: {currentInsanity}");
            OnInsanityChanged?.Invoke(currentInsanity);
            previousInsanity = currentInsanity;
        }
    }

    private void StartFlashbackState()
    {
        Debug.Log("InsanityManager: Flashback iniciado. Resetando insanidade e acelerando taxa.");
        isInFlashback = true; 
        currentPassiveInsanityRate = flashbackInsanityRate;
        currentInsanity = 0f;
        UpdateInsanityAndDispatchEvent(0f);
    }

    private void EndFlashbackState()
    {
        Debug.Log("InsanityManager: Flashback terminado.");
        isInFlashback = false;
        currentPassiveInsanityRate = normalInsanityRate;
        currentInsanity = 0f;
        UpdateInsanityAndDispatchEvent(0f);
    }
    
    private void UseRemedy()
    {
        Debug.Log("InsanityManager: Recebido evento de uso de remédio.");

        // Se estiver em um flashback, primeiro encerra o flashback.
        if (isInFlashback)
        {
            GameEvents.TriggerFlashbackEnded();
        }

        // Para a coroutine anterior se ela estiver rodando
        if (remedyCoroutine != null)
        {
            StopCoroutine(remedyCoroutine);
        }
        remedyCoroutine = StartCoroutine(RemedyEffectRoutine());
    }

    private IEnumerator RemedyEffectRoutine()
    {
        // Pausa o aumento de insanidade imediatamente
        isInsanityPaused = true;
        Debug.Log("Aumento de insanidade PAUSADO.");

        // Reduz a insanidade para 0 ao longo de 'remedyEffectDuration'
        float startingInsanity = currentInsanity;
        float elapsedTime = 0f;
        while (elapsedTime < remedyEffectDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / remedyEffectDuration;
            CurrentInsanity = Mathf.Lerp(startingInsanity, 0f, t); // Usa a propriedade para garantir o evento
            yield return null;
        }
        CurrentInsanity = 0f; // Garante que o valor final seja exatamente 0

        // Mantém a insanidade pausada por 'insanityPauseDuration'
        Debug.Log($"A insanidade ficará pausada por {insanityPauseDuration} segundos.");
        yield return new WaitForSeconds(insanityPauseDuration);

        // Retoma o aumento de insanidade
        isInsanityPaused = false;
        remedyCoroutine = null;
        Debug.Log("Aumento de insanidade RETOMADO.");
    }
    
    private void Die()
    {
        if (isPlayerDead) return;

        isPlayerDead = true;
        Debug.Log("JOGADOR MORREU POR INSANIDADE!");

        // Dispara o evento global de morte
        GameEvents.TriggerPlayerDied();

        // this.enabled = false; 
    }
}