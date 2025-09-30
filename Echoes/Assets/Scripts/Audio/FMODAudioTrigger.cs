using FMODUnity;
using FMOD.Studio;
using UnityEngine;

/// <summary>
/// Componente modular para tocar eventos FMOD 3D em qualquer posição do mundo.
/// Permite one-shot, loop, spatialization e controle de parâmetros.
/// </summary>
public class FMODAudioTrigger : MonoBehaviour
{
    [Tooltip("Evento FMOD a ser disparado.")]
    public EventReference fmodEvent;

    [Tooltip("Toca o evento automaticamente ao iniciar?")]
    public bool playOnStart = false;

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
        
        // Inicia o evento
        instance.start();
        
        isPlaying = true;
        
        // Limpa os parâmetros após aplicar
        pendingParameters.Clear();
        
        // Libera automaticamente após reprodução
        instance.release();
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
    /// Para o áudio se estiver tocando.
    /// </summary>
    public void Stop(bool immediate = false)
    {
        if (instance.isValid() && isPlaying)
        {
            instance.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            isPlaying = false;
        }
    }

    private void OnDestroy()
    {
        Stop(true);
    }
}