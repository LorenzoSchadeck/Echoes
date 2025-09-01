using System;
using System.Collections;
using UnityEngine;

public class InsanityManager : MonoBehaviour
{
    public static event Action<float> OnVisualInsanityChanged;
    public static event Action<float> OnLatentInsanityChanged;

    [Header("Insanity Settings (Visual)")]
    [SerializeField, Range(0f, 1f)] private float visualInsanity = 0f;
    [SerializeField] private float flashbackVisualInsanityRate = 0.05f;

    [Header("Insanity Settings (Latent)")]
    [SerializeField, Range(0f, 1f)] private float latentInsanity = 0f;
    [SerializeField] private float latentInsanityPassiveRate = 0.005f;

    [Header("Death Settings")]
    [SerializeField] private float timeAtMaxInsanityBeforeDeath = 10f;
    private float maxInsanityTimer = 0f;
    private bool isPlayerDead = false;
    private bool isDeathSequenceActive = false;

    [Header("Remedy Settings")]
    [Tooltip("Duração em segundos para a insanidade ir a zero após usar um remédio.")]
    [SerializeField] private float remedyTransitionDuration = 3.0f;
    [Tooltip("Duração em segundos que o aumento de insanidade fica pausado após o efeito do remédio.")]
    [SerializeField] private float insanityPauseDuration = 15.0f;
    
    private float previousVisualInsanity;
    private float previousLatentInsanity;
    private bool isInFlashback = false;
    private Coroutine remedyCoroutine;
    private bool isInsanityPaused = false;

    // --- Propriedades Públicas ---
    public float VisualInsanity { get => visualInsanity; set => visualInsanity = Mathf.Clamp01(value); }
    public float LatentInsanity { get => latentInsanity; set => latentInsanity = Mathf.Clamp01(value); }

    private void OnEnable()
    {
        GameEvents.OnFlashbackStarted += StartFlashbackState;
        GameEvents.OnFlashbackEnded += EndFlashbackState;
        GameEvents.OnRemedyUsed += UseRemedy;
        GameEvents.OnTriggerVisualFlash += OnTriggerVisualFlash;
    }

    private void OnDisable()
    {
        GameEvents.OnFlashbackStarted -= StartFlashbackState;
        GameEvents.OnFlashbackEnded -= EndFlashbackState;
        GameEvents.OnRemedyUsed -= UseRemedy;
        GameEvents.OnTriggerVisualFlash -= OnTriggerVisualFlash;
    }

    private void Start()
    {
        previousVisualInsanity = visualInsanity;
        previousLatentInsanity = latentInsanity;
        UpdateInsanityAndDispatchEvent(visualInsanity, latentInsanity);
    }

    private void Update()
    {
        if (isPlayerDead) return;

        if (!isInsanityPaused)
        {
            if (latentInsanity < 1f) latentInsanity += latentInsanityPassiveRate * Time.deltaTime;
            if (isInFlashback && visualInsanity < 1f) visualInsanity += flashbackVisualInsanityRate * Time.deltaTime;
        }

        if (Mathf.Approximately(visualInsanity, 1f))
        {
            if (!isDeathSequenceActive)
            {
                isDeathSequenceActive = true;
                GameEvents.TriggerDeathSequenceStarted(timeAtMaxInsanityBeforeDeath);
            }
            maxInsanityTimer += Time.deltaTime;
            if (maxInsanityTimer >= timeAtMaxInsanityBeforeDeath) Die();
        }
        else
        {
            if (isDeathSequenceActive)
            {
                isDeathSequenceActive = false;
                GameEvents.TriggerDeathSequenceCancelled();
            }
            maxInsanityTimer = 0f;
        }

        UpdateInsanityAndDispatchEvent(visualInsanity, latentInsanity);
    }

    private void UpdateInsanityAndDispatchEvent(float newVisual, float newLatent)
    {
        newVisual = Mathf.Clamp01(newVisual);
        newLatent = Mathf.Clamp01(newLatent);

        if (!Mathf.Approximately(previousVisualInsanity, newVisual))
        {
            Debug.Log($"Visual Insanity changed to: {newVisual}");
            OnVisualInsanityChanged?.Invoke(newVisual);
            previousVisualInsanity = newVisual;
        }

        if (!Mathf.Approximately(previousLatentInsanity, newLatent))
        {
            Debug.Log($"Latent Insanity changed to: {newLatent}");
            OnLatentInsanityChanged?.Invoke(newLatent);
            previousLatentInsanity = newLatent;
        }
        
        visualInsanity = newVisual;
        latentInsanity = newLatent;
    }

    private void Die()
    {
        if (isPlayerDead) return;
        isPlayerDead = true;
        Debug.Log("JOGADOR MORREU POR INSANIDADE!");
        GameEvents.TriggerPlayerDied();
        this.enabled = false;
    }

    private void StartFlashbackState()
    {
        isInFlashback = true;
        VisualInsanity = 0f;
    }

    private void EndFlashbackState()
    {
        isInFlashback = false;
        VisualInsanity = 0f;
    }

    private void UseRemedy()
    {
        if (isInFlashback) GameEvents.TriggerFlashbackEnded();
        if (remedyCoroutine != null) StopCoroutine(remedyCoroutine);
        remedyCoroutine = StartCoroutine(RemedyEffectRoutine());
    }

    private IEnumerator RemedyEffectRoutine()
    {
        isInsanityPaused = true;
        Debug.Log("Aumento de insanidade PAUSADO.");

        float startingVisualInsanity = visualInsanity;
        float elapsedTime = 0f;
        
        while (elapsedTime < remedyTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / remedyTransitionDuration;
            VisualInsanity = Mathf.Lerp(startingVisualInsanity, 0f, t);
            yield return null;
        }
        VisualInsanity = 0f;

        Debug.Log($"A insanidade ficará pausada por {insanityPauseDuration} segundos.");
        yield return new WaitForSeconds(insanityPauseDuration);

        isInsanityPaused = false;
        remedyCoroutine = null;
        Debug.Log("Aumento de insanidade RETOMADO.");
    }
    
    private Coroutine visualFlashRoutine;
    
    private void OnTriggerVisualFlash(float peakInsanity, float duration)
    {
        if (visualFlashRoutine != null) StopCoroutine(visualFlashRoutine);
        visualFlashRoutine = StartCoroutine(VisualFlashRoutine(peakInsanity, duration));
    }

    private IEnumerator VisualFlashRoutine(float peakInsanity, float duration)
    {
        float startInsanity = visualInsanity;
        float halfDuration = duration / 2f;
        
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            VisualInsanity = Mathf.Lerp(startInsanity, peakInsanity, t);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            VisualInsanity = Mathf.Lerp(peakInsanity, startInsanity, t);
            yield return null;
        }
        VisualInsanity = startInsanity;
        visualFlashRoutine = null;
    }
}