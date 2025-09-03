using System;
using System.Collections;
using UnityEngine;

public class InsanityManager : MonoBehaviour
{
    // Envia a sanidade atual (1.0 = são, 0.0 = colapso)
    public static event Action<float> OnSanityChanged;

    [Header("Sanity Core Settings")]
    [Tooltip("A sanidade atual do jogador. 1.0 = são, 0.0 = colapso.")]
    [SerializeField, Range(0f, 1f)]
    private float currentSanity = 1.0f;

    [Tooltip("Taxa de perda de sanidade por segundo em estado normal.")]
    [SerializeField]
    private float normalSanityDrainRate = 0.01f; // Equivalente a 1% de sanidade perdida a cada segundo

    [Tooltip("Taxa de perda de sanidade por segundo durante um flashback.")]
    [SerializeField]
    private float flashbackSanityDrainRate = 0.05f;

    [Header("Death Settings")]
    [SerializeField]
    private float timeAtZeroSanityBeforeDeath = 10f;
    private float zeroSanityTimer = 0f;

    [Header("Remedy Settings")]
    [Tooltip("Duração da pausa na perda de sanidade após usar um remédio.")]
    [SerializeField]
    private float sanityDrainPauseDuration = 15.0f;

    // --- Estado Interno ---
    private float previousSanity;
    private bool isInFlashback = false;
    private bool isSanityDrainPaused = false;
    private bool isDeathSequenceActive = false;
    private bool isPlayerDead = false;
    private Coroutine remedyCoroutine;
    private float currentSanityDrainRate;

    // Propriedade pública para manipulação externa
    public float CurrentSanity { get => currentSanity; set => currentSanity = Mathf.Clamp01(value); }

    private void OnEnable()
    {
        GameEvents.OnFlashbackStarted += StartFlashbackState;
        GameEvents.OnFlashbackEnded += EndFlashbackState;
        GameEvents.OnRemedyUsed += UseRemedy;
        GameEvents.OnSanityLost += LoseSanity;
    }

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= StartFlashbackState;
        GameEvents.OnFlashbackEnded -= EndFlashbackState;
        GameEvents.OnRemedyUsed -= UseRemedy;
        GameEvents.OnSanityLost -= LoseSanity;
    }

    private void Start()
    {
        currentSanityDrainRate = normalSanityDrainRate;
        UpdateSanityAndDispatchEvent(currentSanity);
    }

    private void Update()
    {
        if (isPlayerDead) return;

        // Perda Passiva de Sanidade
        if (!isSanityDrainPaused && currentSanity > 0f)
        {
            currentSanity -= currentSanityDrainRate * Time.deltaTime;
        }

        // Lógica de Morte (baseada em Sanidade Zero)
        if (Mathf.Approximately(currentSanity, 0f))
        {
            if (!isDeathSequenceActive)
            {
                isDeathSequenceActive = true;
                GameEvents.TriggerDeathSequenceStarted(timeAtZeroSanityBeforeDeath);
            }
            zeroSanityTimer += Time.deltaTime;
            if (zeroSanityTimer >= timeAtZeroSanityBeforeDeath) Die();
        }
        else
        {
            if (isDeathSequenceActive)
            {
                isDeathSequenceActive = false;
                GameEvents.TriggerDeathSequenceCancelled();
            }
            zeroSanityTimer = 0f;
        }

        // Disparo Centralizado de Eventos
        UpdateSanityAndDispatchEvent(currentSanity);
    }

    private void UpdateSanityAndDispatchEvent(float newSanity)
    {
        currentSanity = Mathf.Clamp01(newSanity);

        if (!Mathf.Approximately(previousSanity, currentSanity))
        {
            OnSanityChanged?.Invoke(currentSanity);
            previousSanity = currentSanity;
        }
    }

    private void Die()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;
        Debug.Log("JOGADOR MORREU POR INSANIDADE!");
        GameEvents.TriggerPlayerDied();
        this.enabled = false;
    }

    private void LoseSanity(float amount)
    {
        if (isPlayerDead || isSanityDrainPaused) return;

        Debug.Log($"Perdendo {amount:P0} de sanidade por uma ação.");
        CurrentSanity -= amount;
    }

    private void StartFlashbackState()
    {
        isInFlashback = true;
        currentSanityDrainRate = flashbackSanityDrainRate;
        CurrentSanity = 1.0f; // Reseta a sanidade para o início do flashback
        UpdateSanityAndDispatchEvent(1.0f); // Força a atualização
    }

    private void EndFlashbackState()
    {
        isInFlashback = false;
        currentSanityDrainRate = normalSanityDrainRate;
        CurrentSanity = 1.0f; // Reseta a sanidade ao voltar para o mundo normal
        UpdateSanityAndDispatchEvent(1.0f); // Força a atualização
    }

    private void UseRemedy()
    {
        if (remedyCoroutine != null) StopCoroutine(remedyCoroutine);
        
        // Se estiver em um flashback, o remédio força a saída
        if (isInFlashback)
        {
            GameEvents.TriggerFlashbackEnded();
        }
        
        remedyCoroutine = StartCoroutine(RemedyEffectRoutine());
    }

    private IEnumerator RemedyEffectRoutine()
    {
        // FASE 1: Ações Imediatas
        isSanityDrainPaused = true;
        CurrentSanity = 1.0f; // O remédio restaura a sanidade para 100% INSTANTANEAMENTE
        UpdateSanityAndDispatchEvent(1.0f); // Garante que todos os sistemas saibam disso

        // FASE 2: Espera pela duração da pausa
        yield return new WaitForSeconds(sanityDrainPauseDuration);

        // FASE 3: Retoma a perda de sanidade
        isSanityDrainPaused = false;
        remedyCoroutine = null;
    }
}