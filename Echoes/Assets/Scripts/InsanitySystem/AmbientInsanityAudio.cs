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
        
        [Header("📡 Audio Spatial Settings")]
        [Tooltip("Distância mínima onde o som de vento começa a diminuir")]
        [SerializeField, Range(1f, 100f)] private float windMinDistance = 5f;
        
        [Tooltip("Distância máxima onde o som de vento não é mais audível")]
        [SerializeField, Range(10f, 200f)] private float windMaxDistance = 50f;
        
        [Tooltip("Distância mínima onde o som de trovão começa a diminuir")]
        [SerializeField, Range(5f, 150f)] private float thunderMinDistance = 10f;
        
        [Tooltip("Distância máxima onde o som de trovão não é mais audível")]
        [SerializeField, Range(20f, 300f)] private float thunderMaxDistance = 100f;
        
        [Header("🔊 Volume Controls")]
        [Tooltip("Volume do som de vento (0.0 = silêncio, 1.0 = volume máximo)")]
        [SerializeField, Range(0f, 1f)] private float windVolume = 1.0f;
        
        [Tooltip("Volume do som de trovão (0.0 = silêncio, 1.0 = volume máximo)")]
        [SerializeField, Range(0f, 1f)] private float thunderVolume = 1.0f;
        
        [Tooltip("Volume base do som de tensão quando ativo (multiplicado pelo fator de intensidade)")]
        [SerializeField, Range(0f, 1f)] private float tensionBaseVolume = 0.8f;
        
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
        
        // Direct FMOD EventInstance control for better volume management
        private FMOD.Studio.EventInstance windEventInstance;
        private FMOD.Studio.EventInstance thunderEventInstance;
        private FMOD.Studio.EventInstance tensionEventInstance;
        
        private float targetTensionValue = 0f;
        private float currentTensionValue = 0f;
        private float preFlashbackTensionValue = 0f;
        private bool isTensionAudioPlaying = false;
        
        // Volume control variables
        private float currentWindVolume = 0f;
        private float currentThunderVolume = 0f;
        private float currentTensionVolume = 0f;
        
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
                Debug.Log("[AmbientInsanityAudio] Iniciando inicialização do sistema FMOD...");
                
                // Verifica se os eventos estão configurados
                Debug.Log($"[AmbientInsanityAudio] Wind Event: {windAmbientEvent}");
                Debug.Log($"[AmbientInsanityAudio] Thunder Event: {thunderAmbientEvent}");
                Debug.Log($"[AmbientInsanityAudio] Tension Event: {tensionAmbientEvent}");
                
                // Cria EventInstances diretos para melhor controle
                Debug.Log("[AmbientInsanityAudio] Criando EventInstances...");
                windEventInstance = FMODUnity.RuntimeManager.CreateInstance(windAmbientEvent);
                thunderEventInstance = FMODUnity.RuntimeManager.CreateInstance(thunderAmbientEvent);
                tensionEventInstance = FMODUnity.RuntimeManager.CreateInstance(tensionAmbientEvent);
                
                // Verifica se foram criados com sucesso
                Debug.Log($"[AmbientInsanityAudio] Wind EventInstance válido: {windEventInstance.isValid()}");
                Debug.Log($"[AmbientInsanityAudio] Thunder EventInstance válido: {thunderEventInstance.isValid()}");
                Debug.Log($"[AmbientInsanityAudio] Tension EventInstance válido: {tensionEventInstance.isValid()}");
                
                // Configura posição 3D
                Debug.Log("[AmbientInsanityAudio] Configurando posições 3D...");
                var transform3D = FMODUnity.RuntimeUtils.To3DAttributes(transform);
                windEventInstance.set3DAttributes(transform3D);
                thunderEventInstance.set3DAttributes(transform3D);
                tensionEventInstance.set3DAttributes(transform3D);
                
                // TAMBÉM cria componentes FMODAudioTrigger para compatibilidade
                Debug.Log("[AmbientInsanityAudio] Criando FMODAudioTriggers de compatibilidade...");
                windAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                thunderAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                tensionAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                
                windAudioTrigger.fmodEvent = windAmbientEvent;
                thunderAudioTrigger.fmodEvent = thunderAmbientEvent;
                tensionAudioTrigger.fmodEvent = tensionAmbientEvent;
                
                windAudioTrigger.SetSpatialRange(windMinDistance, windMaxDistance);
                thunderAudioTrigger.SetSpatialRange(thunderMinDistance, thunderMaxDistance);
                
                isSystemInitialized = true;
                Debug.Log("[AmbientInsanityAudio] Sistema inicializado com sucesso!");
                
                // Inicia os sons ambiente em loop
                StartAmbientSounds();
                
                Debug.Log($"[AmbientInsanityAudio] Sistema FMOD inicializado com EventInstances diretos - Sanity inicial: {currentSanity:F2}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AmbientInsanityAudio] Erro ao inicializar FMOD: {e.Message}");
                Debug.LogError($"[AmbientInsanityAudio] Stack trace: {e.StackTrace}");
                enabled = false;
            }
        }

        
        private void StartAmbientSounds()
        {
            Debug.Log("[AmbientInsanityAudio] StartAmbientSounds() chamado");
            
            try
            {
                // Verifica se os EventInstances foram criados
                if (windEventInstance.isValid())
                {
                    Debug.Log("[AmbientInsanityAudio] Wind EventInstance válido - iniciando...");
                    var result = windEventInstance.start();
                    Debug.Log($"[AmbientInsanityAudio] Wind start result: {result}");
                }
                else
                {
                    Debug.LogError("[AmbientInsanityAudio] Wind EventInstance INVÁLIDO!");
                }
                
                if (thunderEventInstance.isValid())
                {
                    Debug.Log("[AmbientInsanityAudio] Thunder EventInstance válido - iniciando...");
                    var result = thunderEventInstance.start();
                    Debug.Log($"[AmbientInsanityAudio] Thunder start result: {result}");
                }
                else
                {
                    Debug.LogError("[AmbientInsanityAudio] Thunder EventInstance INVÁLIDO!");
                }
                
                // Aplica volumes iniciais
                currentWindVolume = windVolume;
                currentThunderVolume = thunderVolume;
                
                // Aplica volumes diretamente nos EventInstances
                if (windEventInstance.isValid())
                {
                    windEventInstance.setVolume(currentWindVolume);
                    Debug.Log($"[AmbientInsanityAudio] Wind volume set to: {currentWindVolume:F2}");
                }
                
                if (thunderEventInstance.isValid())
                {
                    thunderEventInstance.setVolume(currentThunderVolume);
                    Debug.Log($"[AmbientInsanityAudio] Thunder volume set to: {currentThunderVolume:F2}");
                }
                
                Debug.Log($"[AmbientInsanityAudio] EventInstances iniciados - Wind: {currentWindVolume:F2}, Thunder: {currentThunderVolume:F2}");
                
                // Verifica estado de reprodução após 1 segundo
                StartCoroutine(CheckPlaybackState());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AmbientInsanityAudio] Erro ao iniciar sons ambiente: {e.Message}");
            }
            
            // Inicializa valores da tensão como 0
            currentTensionValue = 0f;
            targetTensionValue = 0f;
            currentTensionVolume = 0f;
        }
        
        private System.Collections.IEnumerator CheckPlaybackState()
        {
            yield return new WaitForSeconds(1f);
            
            windEventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE windState);
            thunderEventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE thunderState);
            
            Debug.Log($"[AmbientInsanityAudio] Playback State Check - Wind: {windState}, Thunder: {thunderState}");
            
            // Verifica volumes atuais
            windEventInstance.getVolume(out float windVol);
            thunderEventInstance.getVolume(out float thunderVol);
            
            Debug.Log($"[AmbientInsanityAudio] Current Volumes - Wind: {windVol:F2}, Thunder: {thunderVol:F2}");
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
                    tensionEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
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
                if (!isTensionAudioPlaying)
                {
                    tensionEventInstance.start();
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
                if (!isTensionAudioPlaying)
                {
                    tensionEventInstance.start();
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
                tensionEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
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
            if (!isSystemInitialized) 
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[AmbientInsanityAudio] Tentativa de definir tension antes da inicialização!");
                return;
            }
            
            // Converte o valor de tensão (0-2) para volume (0-1)
            // Multiplica pelo volume base configurável
            float normalizedValue = Mathf.Clamp01(value / maxTensionValue);
            currentTensionVolume = normalizedValue * tensionBaseVolume;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] SetTensionVolume: {currentTensionVolume:F2} (from tension: {value:F2})");
            }
            
            // Controla volume diretamente no EventInstance FMOD
            tensionEventInstance.setVolume(currentTensionVolume);
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
            
            windEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            thunderEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            tensionEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
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
        
        /// <summary>
        /// Atualiza as ranges espaciais dos audio triggers em runtime
        /// </summary>
        /// <param name="newWindMin">Nova distância mínima do vento</param>
        /// <param name="newWindMax">Nova distância máxima do vento</param>
        /// <param name="newThunderMin">Nova distância mínima do trovão</param>
        /// <param name="newThunderMax">Nova distância máxima do trovão</param>
        public void UpdateSpatialRanges(float newWindMin, float newWindMax, float newThunderMin, float newThunderMax)
        {
            if (!isSystemInitialized) return;
            
            windMinDistance = Mathf.Clamp(newWindMin, 1f, 100f);
            windMaxDistance = Mathf.Clamp(newWindMax, 10f, 200f);
            thunderMinDistance = Mathf.Clamp(newThunderMin, 5f, 150f);
            thunderMaxDistance = Mathf.Clamp(newThunderMax, 20f, 300f);
            
            // Aplica as novas ranges
            windAudioTrigger?.SetSpatialRange(windMinDistance, windMaxDistance);
            thunderAudioTrigger?.SetSpatialRange(thunderMinDistance, thunderMaxDistance);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Spatial ranges updated - Wind: {windMinDistance:F1}-{windMaxDistance:F1}m, Thunder: {thunderMinDistance:F1}-{thunderMaxDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Atualiza apenas a range espacial do vento
        /// </summary>
        public void UpdateWindSpatialRange(float minDistance, float maxDistance)
        {
            if (!isSystemInitialized) return;
            
            windMinDistance = Mathf.Clamp(minDistance, 1f, 100f);
            windMaxDistance = Mathf.Clamp(maxDistance, 10f, 200f);
            windAudioTrigger?.SetSpatialRange(windMinDistance, windMaxDistance);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Wind spatial range updated: {windMinDistance:F1}-{windMaxDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Atualiza apenas a range espacial do trovão
        /// </summary>
        public void UpdateThunderSpatialRange(float minDistance, float maxDistance)
        {
            if (!isSystemInitialized) return;
            
            thunderMinDistance = Mathf.Clamp(minDistance, 5f, 150f);
            thunderMaxDistance = Mathf.Clamp(maxDistance, 20f, 300f);
            thunderAudioTrigger?.SetSpatialRange(thunderMinDistance, thunderMaxDistance);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Thunder spatial range updated: {thunderMinDistance:F1}-{thunderMaxDistance:F1}m");
            }
        }
        
        /// <summary>
        /// Atualiza o volume do som de vento
        /// </summary>
        /// <param name="volume">Novo volume (0.0 a 1.0)</param>
        public void SetWindVolume(float volume)
        {
            if (!isSystemInitialized) return;
            
            windVolume = Mathf.Clamp01(volume);
            currentWindVolume = windVolume;
            windEventInstance.setVolume(currentWindVolume);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Wind volume updated: {currentWindVolume:F2}");
            }
        }
        
        /// <summary>
        /// Atualiza o volume do som de trovão
        /// </summary>
        /// <param name="volume">Novo volume (0.0 a 1.0)</param>
        public void SetThunderVolume(float volume)
        {
            if (!isSystemInitialized) return;
            
            thunderVolume = Mathf.Clamp01(volume);
            currentThunderVolume = thunderVolume;
            thunderEventInstance.setVolume(currentThunderVolume);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Thunder volume updated: {currentThunderVolume:F2}");
            }
        }
        
        /// <summary>
        /// Atualiza o volume base do som de tensão
        /// </summary>
        /// <param name="baseVolume">Novo volume base (0.0 a 1.0)</param>
        public void SetTensionBaseVolume(float baseVolume)
        {
            tensionBaseVolume = Mathf.Clamp01(baseVolume);
            
            // Recalcula o volume atual da tensão
            if (isTensionAudioPlaying)
            {
                SetTensionParameter(currentTensionValue);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] Tension base volume updated: {tensionBaseVolume:F2}");
            }
        }
        
        /// <summary>
        /// Atualiza todos os volumes de uma vez
        /// </summary>
        /// <param name="windVol">Volume do vento (0.0 a 1.0)</param>
        /// <param name="thunderVol">Volume do trovão (0.0 a 1.0)</param>
        /// <param name="tensionBaseVol">Volume base da tensão (0.0 a 1.0)</param>
        public void SetAllVolumes(float windVol, float thunderVol, float tensionBaseVol)
        {
            SetWindVolume(windVol);
            SetThunderVolume(thunderVol);
            SetTensionBaseVolume(tensionBaseVol);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[AmbientInsanityAudio] All volumes updated - Wind: {windVolume:F2}, Thunder: {thunderVolume:F2}, Tension Base: {tensionBaseVolume:F2}");
            }
        }
        
        private void OnDestroy()
        {
            if (remedyTransitionCoroutine != null)
            {
                StopCoroutine(remedyTransitionCoroutine);
            }
            
            // Cleanup FMOD EventInstances
            if (isSystemInitialized)
            {
                windEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                thunderEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                tensionEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                
                windEventInstance.release();
                thunderEventInstance.release();
                tensionEventInstance.release();
                
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
            GUILayout.Space(5);
            GUILayout.Label($"Wind Range: {windMinDistance:F1}m - {windMaxDistance:F1}m");
            GUILayout.Label($"Thunder Range: {thunderMinDistance:F1}m - {thunderMaxDistance:F1}m");
            GUILayout.Space(5);
            GUILayout.Label($"Wind Volume: {currentWindVolume:F2}");
            GUILayout.Label($"Thunder Volume: {currentThunderVolume:F2}");
            GUILayout.Label($"Tension Volume: {currentTensionVolume:F2} (Base: {tensionBaseVolume:F2})");
            
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
        
        #region Public Properties for Spatial Ranges
        
        /// <summary>
        /// Distância mínima atual do som de vento
        /// </summary>
        public float WindMinDistance => windMinDistance;
        
        /// <summary>
        /// Distância máxima atual do som de vento
        /// </summary>
        public float WindMaxDistance => windMaxDistance;
        
        /// <summary>
        /// Distância mínima atual do som de trovão
        /// </summary>
        public float ThunderMinDistance => thunderMinDistance;
        
        /// <summary>
        /// Distância máxima atual do som de trovão
        /// </summary>
        public float ThunderMaxDistance => thunderMaxDistance;
        
        /// <summary>
        /// Retorna informações completas das ranges espaciais
        /// </summary>
        public string GetSpatialRangeStats()
        {
            return $"Wind: {windMinDistance:F1}-{windMaxDistance:F1}m | Thunder: {thunderMinDistance:F1}-{thunderMaxDistance:F1}m";
        }
        
        #endregion
        
        #region Public Properties for Volume Controls
        
        /// <summary>
        /// Volume atual do som de vento
        /// </summary>
        public float CurrentWindVolume => currentWindVolume;
        
        /// <summary>
        /// Volume atual do som de trovão
        /// </summary>
        public float CurrentThunderVolume => currentThunderVolume;
        
        /// <summary>
        /// Volume atual do som de tensão
        /// </summary>
        public float CurrentTensionVolume => currentTensionVolume;
        
        /// <summary>
        /// Volume base configurado para tensão
        /// </summary>
        public float TensionBaseVolume => tensionBaseVolume;
        
        /// <summary>
        /// Retorna informações completas dos volumes atuais
        /// </summary>
        public string GetVolumeStats()
        {
            return $"Wind: {currentWindVolume:F2} | Thunder: {currentThunderVolume:F2} | Tension: {currentTensionVolume:F2} (Base: {tensionBaseVolume:F2})";
        }
        
        #endregion
    }
}