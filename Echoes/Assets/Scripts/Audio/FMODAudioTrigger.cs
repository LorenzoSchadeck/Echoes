using FMODUnity;
using FMOD.Studio;
using UnityEngine;

/// <summary>
/// Componente modular para tocar eventos FMOD 3D em qualquer posição do mundo.
/// Permite one-shot, loop, spatialization e controle de parâmetros.
/// </summary>
public class FMODAudioTrigger : MonoBehaviour
{
    // Range padrão para áudio espacial (mesmo que o rádio e eventos de horror)
    private const float STANDARD_AUDIO_MAX_RANGE = 70f;
    
    [Tooltip("Evento FMOD a ser disparado.")]
    public EventReference fmodEvent;

    [Tooltip("Toca o evento automaticamente ao iniciar?")]
    public bool playOnStart = false;

    [Header("Configurações Espaciais 3D")]
    [Tooltip("Distância mínima onde o volume é máximo.")]
    [SerializeField] private float minDistance = 1f;
    
    [Tooltip("Distância máxima onde o áudio ainda é audível (padronizada igual ao rádio).")]
    [SerializeField] private float maxDistance = STANDARD_AUDIO_MAX_RANGE;
    
    [Tooltip("Usa configurações customizadas de distância. Se false, usa as do FMOD Studio.")]
    [SerializeField] private bool useCustomDistances = true;

    private EventInstance instance;
    private bool isPlaying = false;
    
    // Armazena parâmetros que devem ser aplicados antes de tocar
    private System.Collections.Generic.Dictionary<string, float> pendingParameters = new System.Collections.Generic.Dictionary<string, float>();

    private void Start()
    {
        if (playOnStart)
            PlayAtPosition(transform.position);
    }

    public void PlayAtPosition(Vector3 position)
    {
        if (fmodEvent.IsNull) return;
        
        instance = RuntimeManager.CreateInstance(fmodEvent);
        
        if (!instance.isValid()) return;
        
        // Aplica parâmetros pendentes antes de iniciar
        foreach (var param in pendingParameters)
        {
            instance.setParameterByName(param.Key, param.Value);
        }
        
        // Define a posição 3D
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        
        // Aplica configurações customizadas de distância se habilitado
        if (useCustomDistances)
        {
            instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, minDistance);
            instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxDistance);
        }
        
        // Inicia o evento
        instance.start();
        
        isPlaying = true;
        
        // Limpa os parâmetros após aplicar
        pendingParameters.Clear();
        
        // NÃO libera automaticamente - permite mudanças de parâmetro durante reprodução
        // instance.release(); // Será liberado no Stop() ou OnDestroy()
    }

    /// <summary>
    /// Define um parâmetro que será aplicado na próxima reprodução.
    /// </summary>
    public void SetParameter(string parameterName, float value)
    {
        if (string.IsNullOrEmpty(parameterName)) return;
        
        pendingParameters[parameterName] = value;
    }

    /// <summary>
    /// Define um parâmetro imediatamente na instância atual (se estiver tocando).
    /// Útil para mudanças em tempo real durante a reprodução.
    /// </summary>
    public void SetParameterRealTime(string parameterName, float value)
    {
        if (string.IsNullOrEmpty(parameterName)) return;
        
        if (instance.isValid() && isPlaying)
        {
            instance.setParameterByName(parameterName, value);
        }
        else
        {
            // Se não está tocando, armazena como pendente
            pendingParameters[parameterName] = value;
        }
    }

    /// <summary>
    /// Configura as distâncias de atenuação 3D para este áudio.
    /// </summary>
    public void SetSpatialRange(float minDist, float maxDist)
    {
        minDistance = minDist;
        maxDistance = maxDist;
        useCustomDistances = true;
        
        // Se já está tocando, aplica imediatamente
        if (instance.isValid() && isPlaying)
        {
            instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MINIMUM_DISTANCE, minDistance);
            instance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, maxDistance);
        }
    }

    /// <summary>
    /// Para o áudio se estiver tocando.
    /// </summary>
    public void Stop(bool immediate = false)
    {
        if (instance.isValid() && isPlaying)
        {
            instance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release(); // Libera a instância ao parar
            isPlaying = false;
        }
    }

    private void OnDestroy()
    {
        Stop(true);
    }
}