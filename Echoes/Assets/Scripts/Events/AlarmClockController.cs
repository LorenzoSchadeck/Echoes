using UnityEngine;
using System.Collections;

public class AlarmClockController : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Os Renderers das três barras de sanidade, em ordem (da esquerda para a direita).")]
    [SerializeField] private Renderer[] sanityBars;

    [Header("Visual Feedback")]
    [SerializeField] private Color saneColor = new Color(0.5f, 1f, 0.5f); 
    [SerializeField] private Color warningColor = new Color(1f, 1f, 0.5f); 
    [SerializeField] private Color dangerColor = new Color(1f, 0.5f, 0.5f); 

    [Header("Alarm Components")]
    [SerializeField] private Light alarmLight;
    [SerializeField] private FMODUnity.EventReference alarmEvent;
    [SerializeField] private float blinkInterval = 0.5f;

    private FMODAudioTrigger audioTrigger;

    private Coroutine blinkingLightRoutine;
    private MaterialPropertyBlock propBlock; // Otimização: reutilizar o mesmo property block

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
    }

    private void OnEnable()
    {
        InsanityManager.OnSanityChanged += UpdateSanityDisplay;
        GameEvents.OnDeathSequenceStarted += StartAlarm;
        GameEvents.OnDeathSequenceCancelled += StopAlarm;
        GameEvents.OnFalseAlarmTriggered += PlayFalseAlarm;
        GameEvents.OnFlashbackEnded += StopAlarm;
    }

    private void OnDisable()
    {
        InsanityManager.OnSanityChanged -= UpdateSanityDisplay;
        GameEvents.OnDeathSequenceStarted -= StartAlarm;
        GameEvents.OnDeathSequenceCancelled -= StopAlarm;
        GameEvents.OnFalseAlarmTriggered -= PlayFalseAlarm;
        GameEvents.OnFlashbackEnded -= StopAlarm;
    }

    /// <summary>
    /// Atualiza a opacidade e a cor das barras de sanidade com base na Sanidade atual (1.0 = são, 0.0 = colapso).
    /// </summary>
    private void UpdateSanityDisplay(float currentSanity)
    {
        if (sanityBars == null || sanityBars.Length == 0) return;

        int barCount = sanityBars.Length;

        // Define a cor base para TODAS as barras
        Color currentColor = saneColor;
        // Limiar de perigo: quando menos de 40% da sanidade resta (ex: 2 de 5 barras)
        if (currentSanity <= 0.4f) 
        {
            currentColor = dangerColor;
        }
        // Limiar de aviso: quando menos de 80% da sanidade resta (ex: 4 de 5 barras)
        else if (currentSanity <= 0.8f) 
        {
            currentColor = warningColor;
        }

        // Calcula a opacidade de cada barra individualmente
        for (int i = 0; i < barCount; i++)
        {
            if (sanityBars[i] == null) continue;

            // Cada barra representa um segmento de sanidade.
            // Barra 0 (a primeira) representa o segmento de sanidade de 1.0 a 0.8.
            // Barra 1 (a segunda) representa o segmento de 0.8 a 0.6, e assim por diante.
            float segmentSize = 1f / barCount;
            float upperThreshold = 1f - (i * segmentSize);       // O topo do segmento desta barra
            float lowerThreshold = 1f - ((i + 1) * segmentSize); // A base do segmento desta barra

            // Mathf.InverseLerp nos diz "quão longe" a sanidade atual está dentro do segmento desta barra.
            // O resultado (fadeProgress) será 0 se a sanidade estiver no topo do segmento (barra cheia),
            // e 1 se a sanidade estiver na base do segmento (barra vazia).
            float fadeProgress = Mathf.InverseLerp(upperThreshold, lowerThreshold, currentSanity);

            // A opacidade (alpha) é o inverso do progresso do fade.
            float finalAlpha = 1f - fadeProgress;

            // Aplica a cor e a opacidade
            Color finalColor = new (currentColor.r, currentColor.g, currentColor.b, finalAlpha);
            
            sanityBars[i].GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", finalColor);
            sanityBars[i].SetPropertyBlock(propBlock);
        }
    }

    /// <summary>
    /// Toca o alarme real e contínuo quando a sequência de morte começa.
    /// </summary>
    private void StartAlarm(float ignoredDuration = 0f)
    {
        // Se o alarme já estiver tocando, não faz nada
        if (blinkingLightRoutine != null) return;
        Debug.Log("<color=orange>ALARM STARTED</color>");
        if (alarmLight != null)
        {
            blinkingLightRoutine = StartCoroutine(BlinkingLightRoutine());
        }
        if (!alarmEvent.IsNull)
        {
            audioTrigger.fmodEvent = alarmEvent;
            audioTrigger.PlayAtPosition(transform.position);
        }
    }

    // Função única para PARAR o alarme
    public void StopAlarm()
    {
        Debug.Log("<color=cyan>ALARM STOPPED</color>");
        if (blinkingLightRoutine != null)
        {
            StopCoroutine(blinkingLightRoutine);
            blinkingLightRoutine = null;
        }
        if (alarmLight != null) alarmLight.enabled = false;
        audioTrigger.Stop();
    }
    
    // Alarme falso agora chama as funções principais
    private void PlayFalseAlarm(float duration)
    {
        StartCoroutine(FalseAlarmRoutine(duration));
    }

    private IEnumerator FalseAlarmRoutine(float duration)
    {
        StartAlarm(); // Usa a função de início padrão
        // FMOD: evento de alarme deve ser one-shot ou controlado via lógica do evento
        yield return new WaitForSeconds(duration);
        StopAlarm(); // Usa a função de parada padrão
    }

    private IEnumerator BlinkingLightRoutine()
    {
        if (alarmLight == null) yield break;
        alarmLight.enabled = false;
        
        while (true)
        {
            alarmLight.enabled = !alarmLight.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}