using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

namespace Echoes.InsanitySystem
{
    /// <summary>
    /// Controla o áudio ambiente baseado na sanidade do jogador.
    /// Gerencia sons de vento, trovão (sempre tocando em loop) e tensão (baseado na sanidade).
    /// Integrado com FMOD Studio para controle dinâmico de parâmetros.
    /// </summary>
    public class AmbientInsanityAudio : MonoBehaviour
    {
        [Header("🌪️ FMOD Events - Ambient Loops")]
        [Tooltip("Evento FMOD para o som de vento (deve ser configurado para loop)")]
        public FMODUnity.EventReference windAmbientEvent;
        
        [Tooltip("Evento FMOD para o som de trovão (deve ser configurado para loop)")]
        public FMODUnity.EventReference thunderAmbientEvent;
        
        [Header("😰 FMOD Event - Tension")]
        [Tooltip("Evento FMOD para o som de tensão (controlado pela sanidade)")]
        public FMODUnity.EventReference tensionAmbientEvent;
        
        [Header("🎚️ Sanity Control")]
        [Tooltip("Sanidade abaixo deste valor ativa o parâmetro de tensão")]
        [SerializeField, Range(0f, 1f)] private float tensionActivationThreshold = 0.7f;
        
        [Tooltip("Sanidade onde o parâmetro de tensão atinge valor máximo")]
        [SerializeField, Range(0f, 1f)] private float tensionMaxIntensityThreshold = 0.3f;

        [Tooltip("Valor mínimo do parâmetro tension quando ativado (quase inaudível)")]
        [SerializeField, Range(0f, 0.5f)] private float minTensionValue = 0.05f;

        [Tooltip("Valor máximo do parâmetro tension (0.0 a 2.0)")]
        [SerializeField, Range(0f, 2f)] private float maxTensionValue = 2.0f;

        [Tooltip("Velocidade da transição do parâmetro de tensão")]
        [SerializeField, Range(0.1f, 5f)] private float tensionTransitionSpeed = 1.5f;
        
        [Header("⚡ Remedy & Transition Settings")]
        [Tooltip("Duração da transição quando o remédio é usado")]
        [SerializeField, Range(1f, 10f)] private float remedyTransitionDuration = 3f;
        
        [Tooltip("Curva para controlar a progressão do parâmetro tension")]
        [SerializeField] private AnimationCurve tensionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("🐛 Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showDebugGUI = false;
        
        // FMOD Audio Triggers (seguindo padrão do projeto)
        private FMODAudioTrigger windAudioTrigger;
        private FMODAudioTrigger thunderAudioTrigger;
        private FMODAudioTrigger tensionAudioTrigger;
        private float targetTensionValue = 0f;
        private float currentTensionValue = 0f;
        private float preFlashbackTensionValue = 0f;
        private bool isTensionAudioPlaying = false;
        
        // State management
        private float currentSanity = 1f;
        private bool isSystemInitialized = false;
        
        // Remedy transition control
        private bool isRemedyTransitionActive = false;
        private Coroutine remedyTransitionCoroutine;
        
        // Flashback control
        private bool isInFlashback = false;
        
        private void Awake()
        {
            // Validação dos eventos FMOD
            if (windAmbientEvent.IsNull)
            {
                Debug.LogError($"[AmbientInsanityAudio] {name}: Wind Ambient Event não foi configurado!");
                enabled = false;
                return;
            }
            
            if (thunderAmbientEvent.IsNull)
            {
                Debug.LogError($"[AmbientInsanityAudio] {name}: Thunder Ambient Event não foi configurado!");
                enabled = false;
                return;
            }
            
            if (tensionAmbientEvent.IsNull)
            {
                Debug.LogError($"[AmbientInsanityAudio] {name}: Tension Ambient Event não foi configurado!");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            InitializeFMODSystem();
        }
        
        private void InitializeFMODSystem()
        {
            try
            {
                // Cria componentes FMODAudioTrigger seguindo padrão do projeto
                windAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                thunderAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                tensionAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                
                // Configura eventos FMOD
                windAudioTrigger.fmodEvent = windAmbientEvent;
                thunderAudioTrigger.fmodEvent = thunderAmbientEvent;
                tensionAudioTrigger.fmodEvent = tensionAmbientEvent;
                
                // Configura distâncias para eventos 3D (vento e trovão)
                windAudioTrigger.SetSpatialRange(5f, 50f);    // Vento: audível a distância
                thunderAudioTrigger.SetSpatialRange(10f, 100f); // Trovão: muito longe
                
                // Obtém IDs dos parâmetros para controle otimizado
                GetParameterIDs();
                
                isSystemInitialized = true;
                
                // Inicia os sons ambiente em loop
                StartAmbientSounds();
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[AmbientInsanityAudio] Sistema FMOD inicializado - Sanity inicial: {currentSanity:F2}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AmbientInsanityAudio] Erro ao inicializar FMOD: {e.Message}");
                enabled = false;
            }
        }
        
        private void GetParameterIDs()
        {
            // Não precisamos mais buscar parâmetros, controlamos volume diretamente
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] Usando controle direto de volume - não precisa de parâmetros FMOD");
            }
        }
        
        private void StartAmbientSounds()
        {
            // Inicia apenas vento e trovão em posição atual (eventos 3D em loop)
            windAudioTrigger.PlayAtPosition(transform.position);
            thunderAudioTrigger.PlayAtPosition(transform.position);
            
            // NÃO inicia tensão aqui - só deve tocar quando sanidade < 70%
            
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] Audio triggers iniciados (vento 3D, trovão 3D) - tensão aguardando threshold");
            }
            
            // Inicializa valores da tensão como 0
            currentTensionValue = 0f;
            targetTensionValue = 0f;
        }

        
        private void OnEnable()
        {
            InsanityManager.OnSanityChanged += HandleSanityChange;
            GameEvents.OnRemedyUsed += HandleRemedyUsed;
            GameEvents.OnDeathSequenceCancelled += HandleRemedyUsed;
            GameEvents.OnFlashbackStarted += HandleFlashbackStarted;
            GameEvents.OnFlashbackEnded += HandleFlashbackEnded;
        }
        
        private void OnDisable()
        {
            InsanityManager.OnSanityChanged -= HandleSanityChange;
            GameEvents.OnRemedyUsed -= HandleRemedyUsed;
            GameEvents.OnDeathSequenceCancelled -= HandleRemedyUsed;
            GameEvents.OnFlashbackStarted -= HandleFlashbackStarted;
            GameEvents.OnFlashbackEnded -= HandleFlashbackEnded;
        }
        
        private void Update()
        {
            if (!isSystemInitialized || isRemedyTransitionActive || isInFlashback) return;
            
            // Atualiza tensão se necessário
            if (!Mathf.Approximately(currentTensionValue, targetTensionValue))
            {
                currentTensionValue = Mathf.MoveTowards(currentTensionValue, targetTensionValue, tensionTransitionSpeed * Time.deltaTime);
                SetTensionParameter(currentTensionValue);
            }
            
            // Atualiza posição 3D dos eventos espaciais (vento e trovão)
            UpdateSpatialAudio();
        }
        
        private void UpdateSpatialAudio()
        {
            // FMODAudioTrigger já gerencia posicionamento 3D automaticamente
            // Não é necessário atualização manual
        }
        
        private void HandleSanityChange(float newSanity)
        {
            currentSanity = newSanity;
            CalculateTensionValue();
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Sanity: {newSanity:F2} | Target Tension: {targetTensionValue:F2}");
            }
        }
        
        private void CalculateTensionValue()
        {
            // Sanidade >= 70% (0.7): Tensão = 0 (silêncio total)
            if (currentSanity >= tensionActivationThreshold)
            {
                targetTensionValue = 0f;
                
                // Para o áudio de tensão se estiver tocando
                if (isTensionAudioPlaying)
                {
                    tensionAudioTrigger?.Stop(false);
                    isTensionAudioPlaying = false;
                    if (enableDebugLogs)
                    {
                        Debug.Log("[AmbientInsanityAudio] Stopping tension audio - sanity above threshold");
                    }
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[AmbientInsanityAudio] Tension OFF - Sanity above threshold ({currentSanity:F2} >= {tensionActivationThreshold:F2})");
                }
            }
            // Sanidade entre 30% e 70%: Transição suave de minTensionValue para maxTensionValue
            else if (currentSanity >= tensionMaxIntensityThreshold)
            {
                // Inicia o áudio de tensão se não estiver tocando
                if (!isTensionAudioPlaying && tensionAudioTrigger != null)
                {
                    tensionAudioTrigger.PlayAtPosition(transform.position);
                    isTensionAudioPlaying = true;
                    if (enableDebugLogs)
                    {
                        Debug.Log("[AmbientInsanityAudio] Starting tension audio - sanity below threshold");
                    }
                }
                
                // Inverte a lógica: quanto menor a sanidade, maior a tensão
                float intensityFactor = 1f - Mathf.InverseLerp(tensionMaxIntensityThreshold, tensionActivationThreshold, currentSanity);
                intensityFactor = tensionCurve.Evaluate(intensityFactor);
                
                // Mapeia de minTensionValue para maxTensionValue (começa bem baixo e escala)
                targetTensionValue = Mathf.Lerp(minTensionValue, maxTensionValue, intensityFactor);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[AmbientInsanityAudio] Tension scaling - Sanity: {currentSanity:F2}, Factor: {intensityFactor:F2}, Target: {targetTensionValue:F2} (Range: {minTensionValue:F2}-{maxTensionValue:F2})");
                }
            }
            // Sanidade <= 30%: Tensão no máximo
            else
            {
                // Inicia o áudio de tensão se não estiver tocando
                if (!isTensionAudioPlaying && tensionAudioTrigger != null)
                {
                    tensionAudioTrigger.PlayAtPosition(transform.position);
                    isTensionAudioPlaying = true;
                    if (enableDebugLogs)
                    {
                        Debug.Log("[AmbientInsanityAudio] Starting tension audio - maximum intensity");
                    }
                }
                
                targetTensionValue = maxTensionValue;
                if (enableDebugLogs)
                {
                    Debug.Log($"[AmbientInsanityAudio] Tension at MAXIMUM - Sanity below minimum ({currentSanity:F2} <= {tensionMaxIntensityThreshold:F2})");
                }
            }
        }
        
        private void HandleRemedyUsed()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] Remedy used - starting smooth audio transition");
            }
            
            // Para qualquer transição anterior
            if (remedyTransitionCoroutine != null)
            {
                StopCoroutine(remedyTransitionCoroutine);
            }
            
            // Inicia transição suave para estado calmo
            remedyTransitionCoroutine = StartCoroutine(RemedyTransitionRoutine());
        }
        
        private void HandleFlashbackStarted()
        {
            isInFlashback = true;
            preFlashbackTensionValue = currentTensionValue;
            targetTensionValue = 0f;
            
            // Para o áudio de tensão durante flashback
            if (isTensionAudioPlaying)
            {
                tensionAudioTrigger?.Stop(false);
                isTensionAudioPlaying = false;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] Flashback started - tension reduced");
            }
        }
        
        private void HandleFlashbackEnded()
        {
            isInFlashback = false;
            
            // Restaura tensão baseada na sanidade atual
            CalculateTensionValue();
            
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] Flashback ended - tension restored");
            }
        }
        
        private IEnumerator RemedyTransitionRoutine()
        {
            isRemedyTransitionActive = true;
            float startTension = currentTensionValue;
            float targetClean = 0f;
            float elapsedTime = 0f;
            while (elapsedTime < remedyTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / remedyTransitionDuration;
                t = Mathf.SmoothStep(0f, 1f, t);
                currentTensionValue = Mathf.Lerp(startTension, targetClean, t);
                SetTensionParameter(currentTensionValue);
                yield return null;
            }
            currentTensionValue = 0f;
            targetTensionValue = 0f;
            SetTensionParameter(0f);
            isRemedyTransitionActive = false;
            remedyTransitionCoroutine = null;
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] Remedy transition completed - tension eliminated");
            }
        }
        
        private void SetTensionParameter(float value)
        {
            if (!isSystemInitialized || tensionAudioTrigger == null) 
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[AmbientInsanityAudio] Tentativa de definir tension antes da inicialização!");
                return;
            }
            
            // Clamp value to valid range
            value = Mathf.Clamp(value, 0f, maxTensionValue);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] SetTensionParameter: {value:F2}");
            }
            
            // Controla parâmetro via FMODAudioTrigger (assumindo que "tension" existe no evento)
            tensionAudioTrigger.SetParameterRealTime("tension", value);
        }
        
        /// <summary>
        /// Aplica um efeito de tensão temporário (para eventos específicos)
        /// </summary>
        /// <param name="tensionValue">Volume da tensão temporária (0.0 a 1.0)</param>
        /// <param name="duration">Duração do efeito em segundos</param>
        public void ApplyTemporaryTension(float tensionValue, float duration)
        {
            StartCoroutine(TemporaryTensionRoutine(tensionValue, duration));
        }
        private IEnumerator TemporaryTensionRoutine(float tensionValue, float duration)
        {
            if (!isSystemInitialized) yield break;
            float savedValue = currentTensionValue;
            SetTensionParameter(Mathf.Clamp(tensionValue, 0f, maxTensionValue));
            yield return new WaitForSeconds(duration);
            SetTensionParameter(savedValue);
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Temporary tension completed (Value: {tensionValue:F2}, Duration: {duration:F2}s)");
            }
        }
        
        /// <summary>
        /// Para todos os sons ambiente (para cutscenes ou situações especiais)
        /// </summary>
        public void StopAllAmbientSounds()
        {
            if (!isSystemInitialized) return;
            
            windAudioTrigger?.Stop(false);
            thunderAudioTrigger?.Stop(false);
            tensionAudioTrigger?.Stop(false);
            isTensionAudioPlaying = false;
            
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] All ambient sounds stopped");
            }
        }
        
        /// <summary>
        /// Reinicia todos os sons ambiente
        /// </summary>
        public void RestartAllAmbientSounds()
        {
            if (!isSystemInitialized) return;
            
            StartAmbientSounds();
            CalculateTensionValue();
            
            if (enableDebugLogs)
            {
                Debug.Log("[AmbientInsanityAudio] All ambient sounds restarted");
            }
        }
        
        private void OnDestroy()
        {
            if (remedyTransitionCoroutine != null)
            {
                StopCoroutine(remedyTransitionCoroutine);
            }
            
            // Cleanup FMOD instances - FMODAudioTrigger faz cleanup automaticamente
            if (isSystemInitialized)
            {
                windAudioTrigger?.Stop(true);
                thunderAudioTrigger?.Stop(true);
                tensionAudioTrigger?.Stop(true);
                isTensionAudioPlaying = false;
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 540, 350, 250));
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label("🌪️ Ambient Audio Debug");
            GUILayout.Label($"Current Sanity: {currentSanity:F2}");
            GUILayout.Label($"Tension Activation: {tensionActivationThreshold:F2}");
            GUILayout.Label($"Tension Max Intensity: {tensionMaxIntensityThreshold:F2}");
            GUILayout.Label($"Current Tension: {currentTensionValue:F2}");
            GUILayout.Label($"Target Tension: {targetTensionValue:F2}");
            GUILayout.Label($"Tension Range: {minTensionValue:F2} - {maxTensionValue:F2}");
            
            // Status visual
            if (isRemedyTransitionActive)
            {
                GUILayout.Label("💊 REMEDY TRANSITION", GUI.skin.box);
            }
            else if (isInFlashback)
            {
                GUILayout.Label("📖 FLASHBACK MODE", GUI.skin.box);
            }
            else if (currentSanity < tensionMaxIntensityThreshold)
            {
                GUILayout.Label("🔴 MAXIMUM TENSION", GUI.skin.box);
            }
            else if (currentSanity < tensionActivationThreshold)
            {
                GUILayout.Label("😰 TENSION ACTIVE", GUI.skin.box);
            }
            else
            {
                GUILayout.Label("🌤️ CALM AMBIENT", GUI.skin.box);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Retorna informações de status do sistema para debug
        /// </summary>
        public string GetAudioStats()
        {
            string status = "Calm";
            if (currentSanity < tensionMaxIntensityThreshold)
                status = "Maximum Tension";
            else if (currentSanity < tensionActivationThreshold)
                status = "Tension Active";
                
        return $"Sanity: {currentSanity:F2} | Audio: {status} | Tension: {currentTensionValue:F2}";
        }
    }
}