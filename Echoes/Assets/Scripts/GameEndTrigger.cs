using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FMODUnity;
using System.Collections;
using FMOD.Studio;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

/// <summary>
/// Trigger que finaliza o jogo ao colidir com o jogador.
/// Trava movimento, inicia heartbeat intensificando e faz fade out para reset da cena.
/// </summary>
public class GameEndTrigger : MonoBehaviour
{
    [Header("🎵 Áudio de Heartbeat FMOD")]
    [Tooltip("Evento FMOD do batimento cardíaco tocado durante o fade")]
    [SerializeField] private EventReference heartbeatEvent;
    [Tooltip("Se verdadeiro, o áudio é 2D (não espacial)")]
    [SerializeField] private bool is2DAudio = true;
    
    [Header("🎭 Fade da Tela")]
    [Tooltip("Canvas com Image preta para fazer o fade")]
    [SerializeField] private Canvas fadeCanvas;
    [Tooltip("Image preta que será usada para escurecer a tela")]
    [SerializeField] private Image fadeImage;
    [Tooltip("Duração do fade out para preto (em segundos)")]
    [SerializeField] private float fadeDuration = 5f;
    
    [Header("📋 Painel Final")]
    [Tooltip("Canvas com o painel final")]
    [SerializeField] private Canvas endCanvas;
    [Tooltip("CanvasGroup do painel (para fade in do painel)")]
    [SerializeField] private CanvasGroup panelGroup;
    [Tooltip("Componente de texto (Text ou TextMeshProUGUI) para fade do texto")]
    [SerializeField] private Graphic textComponent;
    [Tooltip("String localizada para o texto final do jogo")]
    [SerializeField] private LocalizedString localizedEndText;
    [Tooltip("Duração do fade in do canvas/painel (em segundos)")]
    [SerializeField] private float canvasFadeInDuration = 4f;
    [Tooltip("Duração do fade in do texto (em segundos)")]
    [SerializeField] private float textFadeInDuration = 3f;
    [Tooltip("Tempo que o painel permanece visível antes de resetar (em segundos)")]
    [SerializeField] private float textDisplayDuration = 15f;
    [Tooltip("Duração do fade out do texto antes de resetar (em segundos)")]
    [SerializeField] private float textFadeOutDuration = 2f;
    [Tooltip("Duração do fade out do áudio antes de resetar (em segundos)")]
    [SerializeField] private float audioFadeOutDuration = 2f;
    
    [Header("⚙️ Configurações")]
    [Tooltip("Tag do jogador que deve ativar o trigger")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Se verdadeiro, o trigger pode ser ativado apenas uma vez")]
    [SerializeField] private bool triggerOnce = true;
    
    [Header("🐛 Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Estado interno
    private EventInstance heartbeatInstance;
    private bool isSequenceActive = false;
    private bool hasTriggered = false;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // Valida componentes necessários
        ValidateComponents();
        
        // Configura o canvas de fade
        SetupFadeCanvas();
        
        // Valida que é um trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[GameEndTrigger] {gameObject.name} precisa de um Collider para funcionar!", this);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[GameEndTrigger] {gameObject.name}: Collider não está marcado como Trigger! Corrigindo...", this);
            col.isTrigger = true;
        }
    }
    
    private void OnDestroy()
    {
        // Garante que o áudio seja parado e liberado
        StopHeartbeat();
    }
    
    #endregion
    
    #region Trigger Detection
    
    private void OnTriggerEnter(Collider other)
    {
        // Verifica se já foi ativado
        if (triggerOnce && hasTriggered) return;
        
        // Verifica se é o jogador
        if (!other.CompareTag(playerTag)) return;
        
        // Verifica se a sequência já está ativa
        if (isSequenceActive) return;
        
        // Marca como ativado
        hasTriggered = true;
        
        if (enableDebugLogs)
            Debug.Log($"[GameEndTrigger] 🎬 Trigger ativado por {other.name}! Iniciando sequência de fim de jogo...", this);
        
        // Inicia a sequência de fim de jogo
        StartCoroutine(GameEndSequence());
    }
    
    #endregion
    
    #region Game End Sequence
    
    /// <summary>
    /// Sequência completa de fim de jogo:
    /// 1. Trava movimento do jogador
    /// 2. Inicia heartbeat (som permanece durante todo o processo)
    /// 3. Fade out da tela (5s) - heartbeat continua
    /// 4. Fade in do canvas/painel (4s) - heartbeat continua
    /// 5. Fade in do texto (3s) - heartbeat continua
    /// 6. Aguarda 15s com texto visível
    /// 7. Fade out do texto (2s) - heartbeat continua
    /// 8. Fade out do áudio (2s)
    /// 9. Reseta a cena
    /// </summary>
    private IEnumerator GameEndSequence()
    {
        isSequenceActive = true;
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] 🚫 Travando movimento do jogador...");
        
        // 1. Trava o movimento do jogador
        DisablePlayerControls();
        
        // 2. Inicia o heartbeat (permanece durante todo o fluxo)
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] 💓 Iniciando heartbeat...");
        
        StartHeartbeat();
        
        // 3. Fade out da tela (5s) - heartbeat continua
        if (enableDebugLogs)
            Debug.Log($"[GameEndTrigger] 🌑 Iniciando fade out ({fadeDuration}s)...");
        
        yield return StartCoroutine(FadeOutWithHeartbeat());
        
        // 4. Fade in do canvas/painel (4s) - heartbeat continua
        if (endCanvas != null && panelGroup != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[GameEndTrigger] 🎨 Iniciando fade in do canvas/painel ({canvasFadeInDuration}s)...");
            
            yield return StartCoroutine(FadeInCanvas());
            
            // 5. Fade in do texto (3s) - heartbeat continua
            if (textComponent != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[GameEndTrigger] 📝 Iniciando fade in do texto ({textFadeInDuration}s)...");
                
                yield return StartCoroutine(FadeInText());
                
                // 6. Aguarda 15s com texto completamente visível
                if (enableDebugLogs)
                    Debug.Log($"[GameEndTrigger] ⏳ Texto visível por {textDisplayDuration}s...");
                
                yield return new WaitForSeconds(textDisplayDuration);
                
                // 7. Fade out do texto (2s) - heartbeat continua
                if (enableDebugLogs)
                    Debug.Log($"[GameEndTrigger] 🌑 Iniciando fade out do texto ({textFadeOutDuration}s)...");
                
                yield return StartCoroutine(FadeOutText());
            }
            else
            {
                Debug.LogWarning("[GameEndTrigger] textComponent não configurado! Pulando fade do texto...", this);
            }
        }
        else
        {
            Debug.LogWarning("[GameEndTrigger] endCanvas ou panelGroup não configurado! Pulando exibição do painel...", this);
        }
        
        // 8. Fade out do áudio (2s)
        if (enableDebugLogs)
            Debug.Log($"[GameEndTrigger] 🔉 Iniciando fade out do áudio ({audioFadeOutDuration}s)...");
        
        yield return StartCoroutine(FadeOutAudio());
        
        // 9. Para o heartbeat e reseta a cena
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] 🔇 Parando heartbeat...");
        
        StopHeartbeat();
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] 🔄 Resetando cena...");
        
        ResetScene();
    }
    
    /// <summary>
    /// Fade out da tela com heartbeat tocando
    /// </summary>
    private IEnumerator FadeOutWithHeartbeat()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[GameEndTrigger] fadeImage não está configurado! Pulando fade...", this);
            yield return new WaitForSeconds(fadeDuration);
            yield break;
        }
        
        float elapsed = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(0f, 0f, 0f, 1f); // Preto opaco
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / fadeDuration;
            
            // Interpola o fade
            fadeImage.color = Color.Lerp(startColor, targetColor, normalizedTime);
            
            yield return null;
        }
        
        // Garante que chegou ao final
        fadeImage.color = targetColor;
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] ✅ Fade out completo!");
    }
    
    /// <summary>
    /// Fade in do canvas/painel
    /// </summary>
    private IEnumerator FadeInCanvas()
    {
        if (endCanvas == null || panelGroup == null)
        {
            Debug.LogError("[GameEndTrigger] endCanvas ou panelGroup não está configurado!", this);
            yield break;
        }
        
        // Garante que o canvas está ativo mas invisível
        endCanvas.gameObject.SetActive(true);
        panelGroup.alpha = 0f;
        
        float elapsed = 0f;
        
        while (elapsed < canvasFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / canvasFadeInDuration;
            
            // Interpola o fade in do canvas/painel
            panelGroup.alpha = Mathf.Lerp(0f, 1f, normalizedTime);
            
            yield return null;
        }
        
        // Garante que chegou ao final
        panelGroup.alpha = 1f;
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] ✅ Canvas/Painel visível!");
    }
    
    /// <summary>
    /// Fade in do texto via alpha do componente Graphic (Text/TextMeshProUGUI)
    /// </summary>
    private IEnumerator FadeInText()
    {
        if (textComponent == null)
        {
            Debug.LogError("[GameEndTrigger] textComponent não está configurado!", this);
            yield break;
        }
        
        // Atualiza o texto com a localização antes do fade
        yield return StartCoroutine(UpdateLocalizedText());
        
        // Garante que o texto começa invisível
        Color textColor = textComponent.color;
        textColor.a = 0f;
        textComponent.color = textColor;
        
        float elapsed = 0f;
        
        while (elapsed < textFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / textFadeInDuration;
            
            // Interpola o fade in do texto
            textColor = textComponent.color;
            textColor.a = Mathf.Lerp(0f, 1f, normalizedTime);
            textComponent.color = textColor;
            
            yield return null;
        }
        
        // Garante que chegou ao final
        textColor = textComponent.color;
        textColor.a = 1f;
        textComponent.color = textColor;
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] ✅ Texto visível!");
    }
    
    /// <summary>
    /// Fade out do texto via alpha do componente Graphic
    /// </summary>
    private IEnumerator FadeOutText()
    {
        if (textComponent == null)
        {
            Debug.LogError("[GameEndTrigger] textComponent não está configurado!", this);
            yield break;
        }
        
        float elapsed = 0f;
        Color textColor = textComponent.color;
        float startAlpha = textColor.a;
        
        while (elapsed < textFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / textFadeOutDuration;
            
            // Interpola o fade out do texto
            textColor = textComponent.color;
            textColor.a = Mathf.Lerp(startAlpha, 0f, normalizedTime);
            textComponent.color = textColor;
            
            yield return null;
        }
        
        // Garante que chegou ao final
        textColor = textComponent.color;
        textColor.a = 0f;
        textComponent.color = textColor;
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] ✅ Texto invisível!");
    }
    
    /// <summary>
    /// Fade out do áudio via volume do FMOD
    /// </summary>
    private IEnumerator FadeOutAudio()
    {
        if (!heartbeatInstance.isValid())
        {
            yield break;
        }
        
        float elapsed = 0f;
        float startVolume = 1f;
        
        // Obtém o volume inicial
        heartbeatInstance.getVolume(out startVolume);
        
        while (elapsed < audioFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / audioFadeOutDuration;
            
            // Interpola o fade out do volume
            float targetVolume = Mathf.Lerp(startVolume, 0f, normalizedTime);
            heartbeatInstance.setVolume(targetVolume);
            
            yield return null;
        }
        
        // Garante que chegou ao final
        heartbeatInstance.setVolume(0f);
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] ✅ Áudio em fade out completo!");
    }
    
    /// <summary>
    /// Atualiza o texto com a string localizada
    /// </summary>
    private IEnumerator UpdateLocalizedText()
    {
        if (localizedEndText == null || localizedEndText.IsEmpty)
        {
            Debug.LogWarning("[GameEndTrigger] localizedEndText não está configurado! Texto não será atualizado.", this);
            yield break;
        }
        
        // Usa o sistema nativo do Unity Localization
        var operation = localizedEndText.GetLocalizedStringAsync();
        
        // Aguarda a operação completar
        while (!operation.IsDone)
        {
            yield return null;
        }
        
        // Atualiza o texto baseado no tipo de componente
        if (operation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            string localizedText = operation.Result;
            
            if (textComponent is TextMeshProUGUI tmpComponent)
            {
                tmpComponent.text = localizedText;
            }
            else if (textComponent is Text uguiComponent)
            {
                uguiComponent.text = localizedText;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[GameEndTrigger] Texto localizado atualizado: {localizedText}");
        }
        else
        {
            Debug.LogError($"[GameEndTrigger] Falha ao carregar texto localizado! Status: {operation.Status}", this);
            
            // Fallback para a chave da tabela
            string fallbackText = localizedEndText.TableEntryReference.ToString();
            
            if (textComponent is TextMeshProUGUI tmpComponent)
            {
                tmpComponent.text = fallbackText;
            }
            else if (textComponent is Text uguiComponent)
            {
                uguiComponent.text = fallbackText;
            }
        }
    }
    
    #endregion
    
    #region Player Control
    
    /// <summary>
    /// Desabilita os controles do jogador
    /// </summary>
    private void DisablePlayerControls()
    {
        // Trava movimento via flag estática
        PlayerMovement.canMove = false;
        
        // Desabilita componentes de movimento e interação se encontrados
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            
            if (enableDebugLogs)
                Debug.Log("[GameEndTrigger] PlayerMovement desabilitado");
        }
        
        PlayerInteractor playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        if (playerInteractor != null)
        {
            playerInteractor.enabled = false;
            
            if (enableDebugLogs)
                Debug.Log("[GameEndTrigger] PlayerInteractor desabilitado");
        }
        
        // Trava o Rigidbody se existir
        Rigidbody playerRb = FindFirstObjectByType<PlayerMovement>()?.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
            
            if (enableDebugLogs)
                Debug.Log("[GameEndTrigger] Rigidbody do jogador travado");
        }
    }
    
    #endregion
    
    #region FMOD Heartbeat Control
    
    /// <summary>
    /// Inicia o áudio de heartbeat
    /// </summary>
    private void StartHeartbeat()
    {
        if (heartbeatEvent.IsNull)
        {
            Debug.LogError("[GameEndTrigger] heartbeatEvent não está configurado!", this);
            return;
        }
        
        // Cria a instância do evento
        heartbeatInstance = RuntimeManager.CreateInstance(heartbeatEvent);
        
        if (!heartbeatInstance.isValid())
        {
            Debug.LogError("[GameEndTrigger] Falha ao criar instância do heartbeat!", this);
            return;
        }
        
        // Configura como 2D se necessário
        if (is2DAudio)
        {
            heartbeatInstance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, 0f);
            heartbeatInstance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, 0f);
        }
        else
        {
            // Define posição 3D
            heartbeatInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
        }
        
        // Inicia reprodução
        heartbeatInstance.start();
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] 💓 Heartbeat iniciado!");
    }
    
    /// <summary>
    /// Para e libera o áudio de heartbeat
    /// </summary>
    private void StopHeartbeat()
    {
        if (heartbeatInstance.isValid())
        {
            heartbeatInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            heartbeatInstance.release();
            
            if (enableDebugLogs)
                Debug.Log("[GameEndTrigger] Heartbeat parado e liberado");
        }
    }
    
    #endregion
    
    #region Scene Management
    
    /// <summary>
    /// Reseta a cena atual
    /// </summary>
    private void ResetScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        if (enableDebugLogs)
            Debug.Log($"[GameEndTrigger] Parando todos os sons FMOD antes de resetar...");
        
        // Para todos os eventos FMOD ativos antes de resetar a cena
        StopAllFMODSounds();
        
        if (enableDebugLogs)
            Debug.Log($"[GameEndTrigger] Recarregando cena: {currentSceneName}");
        
        SceneManager.LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Para todos os sons FMOD ativos, incluindo sons de rádio
    /// </summary>
    private void StopAllFMODSounds()
    {
        // Para o heartbeat se ainda estiver tocando
        StopHeartbeat();
        
        // Para o RadioController se existir
        RadioController radioController = FindFirstObjectByType<RadioController>();
        if (radioController != null)
        {
            // Para todas as transmissões do rádio
            radioController.enabled = false;
            
            if (enableDebugLogs)
                Debug.Log("[GameEndTrigger] RadioController desabilitado");
        }
        
        // Para todos os buses do FMOD (isso garante que TUDO pare)
        FMOD.Studio.Bus masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");
        if (masterBus.isValid())
        {
            masterBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
            
            if (enableDebugLogs)
                Debug.Log("[GameEndTrigger] Todos os eventos FMOD parados via master bus");
        }
    }
    
    #endregion
    
    #region Setup & Validation
    
    /// <summary>
    /// Valida os componentes necessários
    /// </summary>
    private void ValidateComponents()
    {
        if (heartbeatEvent.IsNull)
        {
            Debug.LogError("[GameEndTrigger] heartbeatEvent não foi configurado!", this);
        }
        
        if (fadeCanvas == null)
        {
            Debug.LogWarning("[GameEndTrigger] fadeCanvas não foi configurado! Tentando encontrar...", this);
            fadeCanvas = FindCanvasWithFadeImage();
        }
        
        if (fadeImage == null && fadeCanvas != null)
        {
            Debug.LogWarning("[GameEndTrigger] fadeImage não foi configurado! Tentando encontrar no canvas...", this);
            fadeImage = fadeCanvas.GetComponentInChildren<Image>();
        }
        
        if (fadeImage == null)
        {
            Debug.LogError("[GameEndTrigger] fadeImage não encontrado! O fade não funcionará!", this);
        }
        
        if (fadeDuration <= 0f)
        {
            Debug.LogWarning("[GameEndTrigger] fadeDuration deve ser maior que zero! Usando valor padrão de 5s", this);
            fadeDuration = 5f;
        }
        
        if (endCanvas == null)
        {
            Debug.LogWarning("[GameEndTrigger] endCanvas não foi configurado! O canvas final não será exibido!", this);
        }
        
        if (panelGroup == null)
        {
            Debug.LogWarning("[GameEndTrigger] panelGroup não foi configurado! O painel não será exibido!", this);
        }
        
        if (textComponent == null)
        {
            Debug.LogWarning("[GameEndTrigger] textComponent não foi configurado! O texto não será exibido!", this);
        }
        
        if (localizedEndText == null || localizedEndText.IsEmpty)
        {
            Debug.LogWarning("[GameEndTrigger] localizedEndText não foi configurado! O texto não terá localização!", this);
        }
        
        if (canvasFadeInDuration <= 0f)
        {
            Debug.LogWarning("[GameEndTrigger] canvasFadeInDuration deve ser maior que zero! Usando valor padrão de 4s", this);
            canvasFadeInDuration = 4f;
        }
        
        if (textFadeInDuration <= 0f)
        {
            Debug.LogWarning("[GameEndTrigger] textFadeInDuration deve ser maior que zero! Usando valor padrão de 3s", this);
            textFadeInDuration = 3f;
        }
        
        if (textDisplayDuration <= 0f)
        {
            Debug.LogWarning("[GameEndTrigger] textDisplayDuration deve ser maior que zero! Usando valor padrão de 15s", this);
            textDisplayDuration = 15f;
        }
        
        if (textFadeOutDuration <= 0f)
        {
            Debug.LogWarning("[GameEndTrigger] textFadeOutDuration deve ser maior que zero! Usando valor padrão de 2s", this);
            textFadeOutDuration = 2f;
        }
        
        if (audioFadeOutDuration <= 0f)
        {
            Debug.LogWarning("[GameEndTrigger] audioFadeOutDuration deve ser maior que zero! Usando valor padrão de 2s", this);
            audioFadeOutDuration = 2f;
        }
    }
    
    /// <summary>
    /// Configura o canvas de fade para começar invisível
    /// </summary>
    private void SetupFadeCanvas()
    {
        if (fadeCanvas == null || fadeImage == null) return;
        
        // Garante que o canvas está ativo
        if (!fadeCanvas.gameObject.activeInHierarchy)
            fadeCanvas.gameObject.SetActive(true);
        
        // Começa totalmente transparente
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;
        
        // Coloca o canvas na frente de tudo
        fadeCanvas.sortingOrder = 9999;
        
        if (enableDebugLogs)
            Debug.Log("[GameEndTrigger] Canvas de fade configurado (alpha=0)");
        
        // Configura o canvas final
        if (endCanvas != null)
        {
            endCanvas.gameObject.SetActive(false);
            
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
            }
            
            if (textComponent != null)
            {
                Color textColor = textComponent.color;
                textColor.a = 0f;
                textComponent.color = textColor;
            }
            
            if (enableDebugLogs)
                Debug.Log("[GameEndTrigger] Canvas final configurado (desativado e invisível)");
        }
    }
    
    /// <summary>
    /// Tenta encontrar um canvas com imagem de fade na cena
    /// </summary>
    private Canvas FindCanvasWithFadeImage()
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in allCanvases)
        {
            Image img = canvas.GetComponentInChildren<Image>();
            if (img != null && img.color == Color.black)
            {
                fadeImage = img;
                return canvas;
            }
        }
        
        return null;
    }
    
    #endregion
    
    #region Editor Utilities
    
    #if UNITY_EDITOR
    
    private void OnDrawGizmos()
    {
        // Desenha o trigger em vermelho
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
        
        // Label
        float totalDuration = fadeDuration + canvasFadeInDuration + textFadeInDuration + textDisplayDuration + textFadeOutDuration + audioFadeOutDuration;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"🏁 GAME END TRIGGER\nTotal: {totalDuration}s\n(Fade {fadeDuration}s → Panel {canvasFadeInDuration}s → Text In {textFadeInDuration}s + Hold {textDisplayDuration}s + Out {textFadeOutDuration}s + Audio {audioFadeOutDuration}s)");
    }
    
    private void OnDrawGizmosSelected()
    {
        // Desenha linha apontando para o canvas de fade
        if (fadeCanvas != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, fadeCanvas.transform.position);
            
            UnityEditor.Handles.Label(fadeCanvas.transform.position + Vector3.up * 0.5f, 
                $"Fade Canvas ({fadeDuration}s)");
        }
        
        // Desenha linha apontando para o canvas final
        if (endCanvas != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, endCanvas.transform.position);
            
            UnityEditor.Handles.Label(endCanvas.transform.position + Vector3.up * 0.5f, 
                $"End Canvas\n(Panel: {canvasFadeInDuration}s | Text In: {textFadeInDuration}s | Hold: {textDisplayDuration}s | Text Out: {textFadeOutDuration}s | Audio: {audioFadeOutDuration}s)");
        }
    }
    
    /// <summary>
    /// Testa a sequência de fim de jogo no editor
    /// </summary>
    [ContextMenu("Test Game End Sequence")]
    private void TestGameEndSequence()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GameEndTrigger] Teste só funciona em runtime!");
            return;
        }
        
        if (isSequenceActive)
        {
            Debug.LogWarning("[GameEndTrigger] Sequência já está ativa!");
            return;
        }
        
        Debug.Log("[GameEndTrigger] 🧪 Testando sequência de fim de jogo...");
        StartCoroutine(GameEndSequence());
    }
    
    #endif
    
    #endregion
}
