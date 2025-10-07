using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FMODUnity;
using System.Collections;
using FMOD.Studio;

/// <summary>
/// Gerencia a sequência completa de morte do jogador no projeto Echoes.
/// Controla desabilitação de movimento, áudio de morte, fade da tela e reset da cena.
/// </summary>
public class DeathManager : MonoBehaviour
{
    [Header("🎵 Áudio de Morte FMOD")]
    [Tooltip("Evento FMOD que será tocado quando o jogador morrer")]
    public EventReference deathAudioEvent;
    
    [Header("🎭 Fade da Tela")]
    [Tooltip("Canvas com Image preta para fazer o fade")]
    [SerializeField] private Canvas fadeCanvas;
    [Tooltip("Image preta que será usada para escurecer a tela")]
    [SerializeField] private Image fadeImage;
    [Tooltip("Duração do fade in/out para preto (em segundos)")]
    [SerializeField] private float fadeDuration = 2f;
    
    [Header("🐛 Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Componentes e estado
    private FMODAudioTrigger audioTrigger;
    private EventInstance deathAudioInstance;
    private bool isDeathSequenceActive = false;
    private PlayerMovement playerMovement;
    private PlayerInteractor playerInteractor;

    #region Unity Lifecycle

    private void Awake()
    {
        // Configura o audio trigger
        audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
        
        // Encontra as referências necessárias
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        
        if (playerMovement == null && enableDebugLogs)
            Debug.LogWarning("[DeathManager] PlayerMovement não encontrado na cena!", this);
            
        if (playerInteractor == null && enableDebugLogs)
            Debug.LogWarning("[DeathManager] PlayerInteractor não encontrado na cena!", this);
        
        // Configura o fade canvas para começar invisível
        SetupFadeCanvas();
    }

    private void Start()
    {
        // Verifica se deve fazer fade out após reset da cena
        if (PlayerPrefs.GetInt("DoFadeOutAfterReset", 0) == 1)
        {
            PlayerPrefs.DeleteKey("DoFadeOutAfterReset");
            StartFadeOutAfterReset();
        }
    }

    private void OnEnable()
    {
        // Escuta o evento de morte do jogador
        GameEvents.OnPlayerDied += HandlePlayerDeath;
        
        if (enableDebugLogs)
            Debug.Log("[DeathManager] Registrado para escutar eventos de morte do jogador");
    }

    private void OnDisable()
    {
        // Remove a escuta do evento
        GameEvents.OnPlayerDied -= HandlePlayerDeath;
        
        // Para qualquer áudio de morte que esteja tocando
        if (deathAudioInstance.isValid())
        {
            deathAudioInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            deathAudioInstance.release();
        }
    }

    #endregion

    #region Death Sequence

    /// <summary>
    /// Inicia a sequência completa de morte do jogador
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (isDeathSequenceActive)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DeathManager] Sequência de morte já está ativa!");
            return;
        }

        isDeathSequenceActive = true;
        
        if (enableDebugLogs)
            Debug.Log("[DeathManager] 💀 INICIANDO SEQUÊNCIA DE MORTE");

        // Inicia a corrotina da sequência de morte
        StartCoroutine(DeathSequenceCoroutine());
    }

    /// <summary>
    /// Corrotina principal que executa toda a sequência de morte
    /// </summary>
    private IEnumerator DeathSequenceCoroutine()
    {
        // 1. Desabilita movimento e controle da câmera
        DisablePlayerControls();
        
        // 2. Aguarda um pouco antes do fade
        yield return new WaitForSeconds(0.5f);
        
        // 3. Faz fade para preto primeiro
        yield return StartCoroutine(FadeToBlackCoroutine());
        
        if (enableDebugLogs)
            Debug.Log("[DeathManager] Fade concluído, iniciando áudio de morte");
        
        // 4. Toca o áudio de morte APÓS o fade
        float audioDuration = PlayDeathAudio();
        
        if (enableDebugLogs)
            Debug.Log($"[DeathManager] Áudio de morte tocando por {audioDuration:F2} segundos");

        // 5. Aguarda o áudio terminar completamente
        if (audioDuration > 0)
        {
            yield return new WaitForSeconds(audioDuration);
        }
        else
        {
            yield return new WaitForSeconds(3f); // Fallback se não conseguir determinar duração
        }
        
        // 6. Aguarda 1 segundo após o áudio terminar
        yield return new WaitForSeconds(1f);
        
        // 7. Marca que deve fazer fade out após reset e reseta a cena
        PlayerPrefs.SetInt("DoFadeOutAfterReset", 1);
        ResetScene();
    }

    /// <summary>
    /// Desabilita todos os controles do jogador
    /// </summary>
    private void DisablePlayerControls()
    {
        if (enableDebugLogs)
            Debug.Log("[DeathManager] 🚫 Desabilitando controles do jogador");

        // Desabilita movimento via flag estática
        PlayerMovement.canMove = false;
        
        // Desabilita interações via PlayerInteractor
        if (playerInteractor != null)
        {
            playerInteractor.SetInspectionMode(true); // Isso desabilita controles da câmera
        }
        
        // Para qualquer input que possa estar ativo
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    /// <summary>
    /// Toca o áudio de morte usando FMOD e retorna a duração estimada
    /// </summary>
    private float PlayDeathAudio()
    {
        if (deathAudioEvent.IsNull)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DeathManager] Evento de áudio de morte não configurado!");
            return 0f;
        }

        try
        {
            // Cria a instância do evento FMOD
            deathAudioInstance = RuntimeManager.CreateInstance(deathAudioEvent);
            
            if (!deathAudioInstance.isValid())
            {
                if (enableDebugLogs)
                    Debug.LogError("[DeathManager] Falha ao criar instância do evento FMOD!");
                return 0f;
            }

            // Toca o áudio
            deathAudioInstance.start();
            
            if (enableDebugLogs)
                Debug.Log("[DeathManager] 🎵 Áudio de morte iniciado");

            // Tenta obter a duração do evento
            FMOD.RESULT result = deathAudioInstance.getDescription(out FMOD.Studio.EventDescription eventDescription);
            
            if (result != FMOD.RESULT.OK)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[DeathManager] Não foi possível obter descrição do evento FMOD: {result}");
                return 3f; // Fallback duration
            }

            result = eventDescription.getLength(out int length);
            
            if (result != FMOD.RESULT.OK)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[DeathManager] Não foi possível obter duração do evento FMOD: {result}");
                return 3f; // Fallback duration
            }

            // Converte de milissegundos para segundos
            float duration = length / 1000f;
            
            if (enableDebugLogs)
                Debug.Log($"[DeathManager] Duração do áudio: {duration:F2}s");
                
            return duration;
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
                Debug.LogError($"[DeathManager] Erro ao tocar áudio de morte: {ex.Message}");
            return 0f;
        }
    }

    #endregion

    #region Screen Fade

    /// <summary>
    /// Configura o canvas de fade - transparente no início normal, preto se vem de reset
    /// </summary>
    private void SetupFadeCanvas()
    {
        if (fadeCanvas == null || fadeImage == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DeathManager] Canvas ou Image de fade não configurados. Fade será desabilitado.");
            return;
        }

        // SEMPRE garante que o canvas está ativo
        fadeCanvas.gameObject.SetActive(true);
        
        // Se vem de um reset, a imagem deve começar preta
        // Se é início normal, a imagem deve começar transparente
        bool comingFromReset = PlayerPrefs.GetInt("DoFadeOutAfterReset", 0) == 1;
        
        // Garante que a cor é preta com alpha apropriado
        Color fadeColor = new Color(0f, 0f, 0f, comingFromReset ? 1f : 0f);
        fadeImage.color = fadeColor;
        
        if (enableDebugLogs)
            Debug.Log($"[DeathManager] Canvas de fade configurado (alpha: {fadeColor.a})");
    }

    /// <summary>
    /// Corrotina que escurece a tela gradualmente
    /// </summary>
    private IEnumerator FadeToBlackCoroutine()
    {
        if (fadeCanvas == null || fadeImage == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DeathManager] Não é possível fazer fade - componentes não configurados");
            yield break;
        }

        if (enableDebugLogs)
            Debug.Log("[DeathManager] 🌑 Iniciando fade para preto");

        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f); // Alpha = 1 (opaco)

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            // Usa uma curva suave para o fade
            t = Mathf.SmoothStep(0f, 1f, t);
            
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }

        // Garante que terminou completamente preto
        fadeImage.color = targetColor;
        
        // Para todos os áudios do jogo quando o fade terminar
        StopAllGameAudio();
        
        if (enableDebugLogs)
            Debug.Log("[DeathManager] ✅ Fade para preto concluído");
    }

    /// <summary>
    /// Corrotina que clareia a tela gradualmente (fade out após reset)
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        if (fadeCanvas == null || fadeImage == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DeathManager] Não é possível fazer fade out - componentes não configurados");
            yield break;
        }

        if (enableDebugLogs)
            Debug.Log("[DeathManager] 🌅 Iniciando fade out (preto → transparente)");

        // Garante que a tela está preta
        Color startColor = new Color(0f, 0f, 0f, 1f); // Preto opaco
        Color targetColor = new Color(0f, 0f, 0f, 0f); // Preto transparente
        fadeImage.color = startColor;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            // Usa uma curva suave para o fade
            t = Mathf.SmoothStep(0f, 1f, t);
            
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }

        // Garante que terminou completamente transparente
        fadeImage.color = targetColor;
        
        // MANTÉM o canvas ativo mas invisível para futuras mortes
        // fadeCanvas.gameObject.SetActive(false); // REMOVIDO - causava problemas em mortes subsequentes
        
        if (enableDebugLogs)
            Debug.Log("[DeathManager] ✅ Fade out concluído - sistema de morte resetado");
        
        // Reseta o estado para permitir nova morte
        isDeathSequenceActive = false;
    }

    #endregion

    #region Audio Management

    /// <summary>
    /// Para todos os áudios do jogo exceto o de morte
    /// </summary>
    private void StopAllGameAudio()
    {
        if (!RuntimeManager.IsInitialized)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DeathManager] FMOD não inicializado - não é possível parar áudios");
            return;
        }

        try
        {
            // Para todos os eventos FMOD ativos
            FMOD.RESULT result = RuntimeManager.StudioSystem.getBus("bus:/", out FMOD.Studio.Bus masterBus);
            if (result == FMOD.RESULT.OK)
            {
                masterBus.stopAllEvents(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                
                if (enableDebugLogs)
                    Debug.Log("[DeathManager] 🔇 Todos os áudios do jogo foram parados");
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning($"[DeathManager] Falha ao obter master bus: {result}");
            }
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
                Debug.LogError($"[DeathManager] Erro ao parar áudios: {ex.Message}");
        }
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// Reseta a cena atual
    /// </summary>
    private void ResetScene()
    {
        if (enableDebugLogs)
            Debug.Log("[DeathManager] 🔄 Resetando a cena...");

        try
        {
            // Para todos os eventos FMOD antes de resetar
            if (RuntimeManager.IsInitialized)
            {
                FMOD.RESULT result = RuntimeManager.StudioSystem.setParameterByName("Sanity", 1f);
                if (result != FMOD.RESULT.OK && enableDebugLogs)
                {
                    Debug.LogWarning($"[DeathManager] Falha ao resetar parâmetro Sanity: {result}");
                }
            }
            
            // Recarrega a cena atual
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
            
            if (enableDebugLogs)
                Debug.Log($"[DeathManager] ✅ Cena '{currentSceneName}' resetada com sucesso");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DeathManager] Erro ao resetar a cena: {ex.Message}");
        }
    }

    #endregion

    #region Post-Reset Fade Out

    /// <summary>
    /// Inicia o fade out após a cena ter sido resetada
    /// </summary>
    private void StartFadeOutAfterReset()
    {
        if (enableDebugLogs)
            Debug.Log("[DeathManager] 🔄 Cena resetada - iniciando fade out");

        // Garante que o canvas está ativo e a tela está preta
        if (fadeCanvas != null && fadeImage != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeImage.color = new Color(0f, 0f, 0f, 1f); // Preto opaco
            
            // Inicia o fade out
            StartCoroutine(FadeOutCoroutine());
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("[DeathManager] Não foi possível fazer fade out - componentes não encontrados");
        }
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// Força o início da sequência de morte (para testes)
    /// </summary>
    [ContextMenu("Teste: Forçar Morte")]
    public void ForceDeathSequence()
    {
        if (enableDebugLogs)
            Debug.Log("[DeathManager] 🧪 Forçando sequência de morte para teste");
            
        HandlePlayerDeath();
    }

    /// <summary>
    /// Para a sequência de morte se estiver ativa (para emergências)
    /// </summary>
    public void StopDeathSequence()
    {
        if (!isDeathSequenceActive) return;

        if (enableDebugLogs)
            Debug.Log("[DeathManager] ⏹️ Parando sequência de morte");

        StopAllCoroutines();
        
        if (deathAudioInstance.isValid())
        {
            deathAudioInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            deathAudioInstance.release();
        }

        isDeathSequenceActive = false;
    }

    #endregion

    #region Debug

    private void OnValidate()
    {
        // Valida configurações no editor
        if (fadeDuration <= 0f)
        {
            fadeDuration = 2f;
            Debug.LogWarning("[DeathManager] fadeDuration deve ser maior que zero. Resetado para 2 segundos.");
        }
    }

    #endregion
}