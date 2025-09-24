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

    private void Start()
    {
        if (playOnStart)
            PlayAtPosition(transform.position);
    }

    public void PlayAtPosition(Vector3 position)
    {
        if (fmodEvent.IsNull) return;
        instance = RuntimeManager.CreateInstance(fmodEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        instance.start();
        instance.release(); // Para one-shot
        isPlaying = true;
    }

    public void PlayAttached(GameObject target)
    {
        if (fmodEvent.IsNull) return;
        instance = RuntimeManager.CreateInstance(fmodEvent);
        RuntimeManager.AttachInstanceToGameObject(instance, target.transform, target.GetComponent<Rigidbody>());
        instance.start();
        instance.release();
        isPlaying = true;
    }

    public void Stop()
    {
        if (isPlaying)
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
            isPlaying = false;
        }
    }

    public void SetParameter(string name, float value)
    {
        if (isPlaying)
            instance.setParameterByName(name, value);
    }
}
