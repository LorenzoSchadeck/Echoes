using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Componente para adicionar feedback sonoro aos botões do menu
/// Automaticamente detecta eventos de hover e click
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("FMOD Audio Events")]
    public string hoverEvent = "event:/UI/Button_Hover";
    public string clickEvent = "event:/UI/Button_Click";
    
    [Header("Audio Settings")]
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private float hoverCooldown = 0.1f; // Evita spam de hover sounds
    
    private Button button;
    private float lastHoverTime;
    
    #region Unity Lifecycle
    
    void Awake()
    {
        button = GetComponent<Button>();
        
        // Adiciona listener para click sound
        if (button != null && playClickSound)
        {
            button.onClick.AddListener(PlayClickSound);
        }
    }
    
    void OnDestroy()
    {
        // Remove listener para evitar memory leaks
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
    
    #endregion
    
    #region IPointerEventHandler Implementation
    
    /// <summary>
    /// Chamado quando o cursor entra na área do botão
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHoverSound && button != null && button.interactable)
        {
            // Verifica cooldown para evitar spam
            if (Time.time - lastHoverTime >= hoverCooldown)
            {
                PlayHoverSound();
                lastHoverTime = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Chamado quando o cursor sai da área do botão
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Pode ser usado para stop de eventos contínuos se necessário
    }
    
    #endregion
    
    #region Audio Methods
    
    /// <summary>
    /// Reproduz som de hover do botão
    /// </summary>
    private void PlayHoverSound()
    {
        if (!string.IsNullOrEmpty(hoverEvent))
        {
            try
            {
                FMODUnity.RuntimeManager.PlayOneShot(hoverEvent, transform.position);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Erro ao reproduzir hover sound: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Reproduz som de clique do botão
    /// </summary>
    private void PlayClickSound()
    {
        if (!string.IsNullOrEmpty(clickEvent))
        {
            try
            {
                FMODUnity.RuntimeManager.PlayOneShot(clickEvent, transform.position);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Erro ao reproduzir click sound: {e.Message}");
            }
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Força reprodução do som de hover (para uso externo)
    /// </summary>
    public void ForcePlayHoverSound()
    {
        PlayHoverSound();
    }
    
    /// <summary>
    /// Força reprodução do som de clique (para uso externo)
    /// </summary>
    public void ForcePlayClickSound()
    {
        PlayClickSound();
    }
    
    /// <summary>
    /// Habilita/desabilita sons de hover
    /// </summary>
    public void SetHoverSoundEnabled(bool enabled)
    {
        playHoverSound = enabled;
    }
    
    /// <summary>
    /// Habilita/desabilita sons de click
    /// </summary>
    public void SetClickSoundEnabled(bool enabled)
    {
        playClickSound = enabled;
    }
    
    #endregion
}