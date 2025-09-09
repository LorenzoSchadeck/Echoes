using UnityEngine;

public class WhisperSource : MonoBehaviour
{
    private void Start()
    {
        // Se registra com o manager para que ele saiba que esta fonte existe.
        if (DirectionalAudioPuzzleManager.Instance != null)
        {
            DirectionalAudioPuzzleManager.Instance.RegisterWhisperSource(this);
        }
        else
        {
            Debug.LogError($"Não foi possível encontrar o DirectionalAudioPuzzleManager na cena.", this);
        }
    }

    public void PlayWhisper(Whisper whisper)
    {
        // Loga o texto do sussurro diretamente no console.
        Debug.Log($"[Sussurro de {gameObject.name}]: \"{whisper.whisperText}\"");
    }
}