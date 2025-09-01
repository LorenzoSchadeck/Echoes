using UnityEngine;
using System.Collections;

public class AlarmClockController : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("O Transform do Quad que representa a barra de sanidade.")]
    [SerializeField] private Transform sanityBar;

    [Header("Visual Feedback")]
    [Tooltip("O Renderer da barra de sanidade, para podermos mudar sua cor.")]
    [SerializeField] private Renderer sanityBarRenderer;
    [SerializeField] private Color saneColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Alarm Components")]
    [SerializeField] private Light alarmLight;
    [SerializeField] private AudioSource alarmAudio;
    [Tooltip("Intervalo em segundos para a luz piscar (ex: 0.5 = meio segundo acesa, meio segundo apagada).")]
    [SerializeField] private float blinkInterval = 0.5f;

    private Coroutine activeAlarmEventRoutine;
    private Coroutine activeBlinkingLightRoutine;

    private void OnEnable()
    {
        InsanityManager.OnVisualInsanityChanged += UpdateSanityBar;
        GameEvents.OnDeathSequenceStarted += PlayRealAlarm;
        GameEvents.OnDeathSequenceCancelled += StopAllAlarms;
        GameEvents.OnFalseAlarmTriggered += PlayFalseAlarm;
    }

    private void OnDisable()
    {
        InsanityManager.OnVisualInsanityChanged -= UpdateSanityBar;
        GameEvents.OnDeathSequenceStarted -= PlayRealAlarm;
        GameEvents.OnDeathSequenceCancelled -= StopAllAlarms;
        GameEvents.OnFalseAlarmTriggered -= PlayFalseAlarm;
    }

    /// <summary>
    /// Atualiza a escala e a cor da barra de sanidade com base na insanidade visual do jogador.
    /// </summary>
    private void UpdateSanityBar(float visualInsanity)
    {
        if (sanityBar == null || sanityBarRenderer == null) return;

        // A barra de sanidade é o inverso da insanidade. 1.0 insanidade = 0.0 de barra.
        float barScale = 1f - visualInsanity;
        sanityBar.localScale = new Vector3(barScale, 1f, 1f);

        // Usa um MaterialPropertyBlock para mudar a cor de forma otimizada,
        // sem criar novas instâncias de material.
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        sanityBarRenderer.GetPropertyBlock(propBlock);

        Color barColor = saneColor;
        if (barScale <= 0.25f) // Barra em 25% ou menos (insanidade >= 75%)
        {
            barColor = dangerColor;
        }
        else if (barScale <= 0.5f) // Barra entre 25% e 50% (insanidade entre 50% e 75%)
        {
            barColor = warningColor;
        }
        
        propBlock.SetColor("_Color", barColor);
        sanityBarRenderer.SetPropertyBlock(propBlock);
    }

    /// <summary>
    /// Toca o alarme real e contínuo quando a sequência de morte começa.
    /// </summary>
    private void PlayRealAlarm(float ignoredDuration)
    {
        StopAllAlarms(); // Garante que qualquer alarme anterior (como um falso) seja interrompido
        Debug.Log("Alarme REAL disparado! Zona de Perigo!");
        
        // Inicia o efeito de piscar contínuo
        activeBlinkingLightRoutine = StartCoroutine(BlinkingLightRoutine());
        
        if (alarmAudio != null) 
        { 
            alarmAudio.loop = true; 
            alarmAudio.Play(); 
        }
    }

    private void PlayFalseAlarm(float duration)
    {
        // Não toca um alarme falso se o real já estiver tocando
        if (alarmAudio != null && alarmAudio.isPlaying && alarmAudio.loop) return; 

        StopAllAlarms();
        activeAlarmEventRoutine = StartCoroutine(FalseAlarmRoutine(duration));
    }

    private IEnumerator FalseAlarmRoutine(float duration)
    {
        Debug.Log("Alarme FALSO disparado!");
        
        // Inicia o efeito de piscar
        activeBlinkingLightRoutine = StartCoroutine(BlinkingLightRoutine());
        
        if (alarmAudio != null) 
        { 
            alarmAudio.loop = false; 
            alarmAudio.Play(); 
        }

        // Espera a duração do alarme falso
        yield return new WaitForSeconds(duration);

        // Para tudo
        StopAllAlarms();
        Debug.Log("Alarme FALSO terminou.");
    }

    /// <summary>
    /// O "interruptor" central para parar todos os efeitos de alarme.
    /// </summary>
    private void StopAllAlarms()
    {
        Debug.Log("Parando todos os alarmes.");
        
        // Para a coroutine do evento (se houver uma)
        if (activeAlarmEventRoutine != null)
        {
            StopCoroutine(activeAlarmEventRoutine);
            activeAlarmEventRoutine = null;
        }

        // Para a coroutine da luz piscando (se houver uma)
        if (activeBlinkingLightRoutine != null)
        {
            StopCoroutine(activeBlinkingLightRoutine);
            activeBlinkingLightRoutine = null;
        }

        // Garante que a luz e o som terminem no estado "desligado"
        if (alarmLight != null) alarmLight.enabled = false;
        if (alarmAudio != null) alarmAudio.Stop();
    }
    
    /// <summary>
    /// Coroutine que executa o loop de piscar da luz indefinidamente.
    /// </summary>
    private IEnumerator BlinkingLightRoutine()
    {
        // Garante que a luz esteja desligada no início para um ciclo consistente
        if (alarmLight != null) alarmLight.enabled = false;
        
        // Loop infinito que só é interrompido quando a coroutine é parada externamente
        while (true)
        {
            if (alarmLight != null) alarmLight.enabled = !alarmLight.enabled; // Inverte o estado da luz
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    // A função StopAlarm do OnDisable precisa ser ajustada para a nova nomenclatura
    private void OnApplicationQuit()
    {
        // Garante que a luz não fique acesa no editor ao parar o jogo
        if(alarmLight != null) alarmLight.enabled = false;
    }
}