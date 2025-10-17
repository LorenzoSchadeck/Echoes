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
        public EventReference windAmbientEvent;
        
        [Tooltip("Evento FMOD para o som de trovão (deve ser configurado para loop)")]
        public EventReference thunderAmbientEvent;
        
        [Header("😰 FMOD Event - Tension")]
        [Tooltip("Evento FMOD para o som de tensão (controlado pela sanidade)")]
        public EventReference tensionAmbientEvent;
        
        [Header("💓 FMOD Event - Heartbeat")]
        [Tooltip("Evento FMOD para o som de batimento cardíaco (toca junto com tensão)")]
        public EventReference heartbeatAmbientEvent;
        
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
        
        [Tooltip("Volume base do batimento cardíaco quando ativo")]
        [SerializeField, Range(0f, 1f)] private float heartbeatBaseVolume = 0.6f;
        
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
        
        [Header("💓 Heartbeat Control")]
        [Tooltip("Velocidade máxima do batimento cardíaco (pitch) quando sanidade está no mínimo")]
        [SerializeField, Range(1.0f, 3.0f)] private float maxHeartbeatSpeed = 2.0f;
        
        [Header("⚡ Remedy & Transition Settings")]
        [Tooltip("Duração da transição quando o remédio é usado")]
        [SerializeField, Range(1f, 10f)] private float remedyTransitionDuration = 3f;
        
        [Tooltip("Curva para controlar a progressão do parâmetro tension")]
        [SerializeField] private AnimationCurve tensionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        // FMOD Audio Triggers (seguindo padrão do projeto)
        private FMODAudioTrigger windAudioTrigger;
        private FMODAudioTrigger thunderAudioTrigger;
        private FMODAudioTrigger tensionAudioTrigger;
        private FMODAudioTrigger heartbeatAudioTrigger;
        
        // Direct FMOD EventInstance control for better volume management
        private EventInstance windEventInstance;
        private EventInstance thunderEventInstance;
        private EventInstance tensionEventInstance;
        private EventInstance heartbeatEventInstance;
        
        private float targetTensionValue = 0f;
        private float currentTensionValue = 0f;
        private float preFlashbackTensionValue = 0f;
        private bool isTensionAudioPlaying = false;
        private bool isHeartbeatAudioPlaying = false;
        
        // Volume control variables
        private float currentWindVolume = 0f;
        private float currentThunderVolume = 0f;
        private float currentTensionVolume = 0f;
        private float currentHeartbeatVolume = 0f;
        
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
            
            if (heartbeatAmbientEvent.IsNull)
            {
                Debug.LogError($"[AmbientInsanityAudio] {name}: Heartbeat Ambient Event não foi configurado!");
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
               
                windEventInstance = RuntimeManager.CreateInstance(windAmbientEvent);
                thunderEventInstance = RuntimeManager.CreateInstance(thunderAmbientEvent);
                tensionEventInstance = RuntimeManager.CreateInstance(tensionAmbientEvent);
                heartbeatEventInstance = RuntimeManager.CreateInstance(heartbeatAmbientEvent);

                var transform3D = RuntimeUtils.To3DAttributes(transform);
                windEventInstance.set3DAttributes(transform3D);
                thunderEventInstance.set3DAttributes(transform3D);
                tensionEventInstance.set3DAttributes(transform3D);
                heartbeatEventInstance.set3DAttributes(transform3D);
                
                windAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                thunderAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                tensionAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                heartbeatAudioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
                
                windAudioTrigger.fmodEvent = windAmbientEvent;
                thunderAudioTrigger.fmodEvent = thunderAmbientEvent;
                tensionAudioTrigger.fmodEvent = tensionAmbientEvent;
                heartbeatAudioTrigger.fmodEvent = heartbeatAmbientEvent;
                
                windAudioTrigger.SetSpatialRange(windMinDistance, windMaxDistance);
                thunderAudioTrigger.SetSpatialRange(thunderMinDistance, thunderMaxDistance);
                
                isSystemInitialized = true;
                
                // Inicia os sons ambiente em loop
                StartAmbientSounds();
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
            try
            {
                // Verifica se os EventInstances foram criados
                if (windEventInstance.isValid())
                {
                    var result = windEventInstance.start();
                }
                
                if (thunderEventInstance.isValid())
                {
                    var result = thunderEventInstance.start();
                }
                
                // Aplica volumes iniciais
                currentWindVolume = windVolume;
                currentThunderVolume = thunderVolume;
                
                // Aplica volumes diretamente nos EventInstances
                if (windEventInstance.isValid())
                {
                    windEventInstance.setVolume(currentWindVolume);
                }
                
                if (thunderEventInstance.isValid())
                {
                    thunderEventInstance.setVolume(currentThunderVolume);
                }

                // Verifica estado de reprodução após 1 segundo
                StartCoroutine(CheckPlaybackState());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AmbientInsanityAudio] Erro ao iniciar sons ambiente: {e.Message}");
            }
            
            // Inicializa valores da tensão e batimento cardíaco como 0
            currentTensionValue = 0f;
            targetTensionValue = 0f;
            currentTensionVolume = 0f;
            currentHeartbeatVolume = 0f;
        }
        
        private System.Collections.IEnumerator CheckPlaybackState()
        {
            yield return new WaitForSeconds(1f);
            
            windEventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE windState);
            thunderEventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE thunderState);
            
            // Verifica volumes atuais
            windEventInstance.getVolume(out float windVol);
            thunderEventInstance.getVolume(out float thunderVol);
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
            // CORREÇÃO: Remove bloqueio de flashback para permitir áudio de tensão durante flashback
            if (!isSystemInitialized || isRemedyTransitionActive) return;
            
            // Atualiza tensão se necessário
            if (!Mathf.Approximately(currentTensionValue, targetTensionValue))
            {
                currentTensionValue = Mathf.MoveTowards(currentTensionValue, targetTensionValue, tensionTransitionSpeed * Time.deltaTime);
                SetTensionParameter(currentTensionValue);
            }
        }
        
        private void HandleSanityChange(float newSanity)
        {
            currentSanity = newSanity;
            
            // Debug para verificar se está funcionando durante flashback
            if (isInFlashback)
            {
                Debug.Log($"[AmbientAudio] Mudança de sanidade durante flashback: {newSanity:F2} - Recalculando tensão e heartbeat");
            }
            
            CalculateTensionValue();
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
                }
                
                // Para o batimento cardíaco se estiver tocando
                if (isHeartbeatAudioPlaying)
                {
                    heartbeatEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    isHeartbeatAudioPlaying = false;
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
                    if (isInFlashback) Debug.Log("[AmbientAudio] Iniciando áudio de tensão durante flashback");
                }
                
                // Inicia o batimento cardíaco se não estiver tocando
                if (!isHeartbeatAudioPlaying)
                {
                    heartbeatEventInstance.start();
                    isHeartbeatAudioPlaying = true;
                    if (isInFlashback) Debug.Log("[AmbientAudio] Iniciando heartbeat durante flashback");
                }
                
                // Inverte a lógica: quanto menor a sanidade, maior a tensão
                float intensityFactor = 1f - Mathf.InverseLerp(tensionMaxIntensityThreshold, tensionActivationThreshold, currentSanity);
                intensityFactor = tensionCurve.Evaluate(intensityFactor);
                
                // Mapeia de minTensionValue para maxTensionValue (começa bem baixo e escala)
                targetTensionValue = Mathf.Lerp(minTensionValue, maxTensionValue, intensityFactor);
            }
            // Sanidade <= 30%: Tensão no máximo
            else
            {
                // Inicia o áudio de tensão se não estiver tocando
                if (!isTensionAudioPlaying)
                {
                    tensionEventInstance.start();
                    isTensionAudioPlaying = true;
                }
                
                // Inicia o batimento cardíaco se não estiver tocando
                if (!isHeartbeatAudioPlaying)
                {
                    heartbeatEventInstance.start();
                    isHeartbeatAudioPlaying = true;
                }
                
                targetTensionValue = maxTensionValue;
            }
            
            // Atualiza o batimento cardíaco sempre que há mudança na tensão
            UpdateHeartbeatAudio();
        }
        
        private void HandleRemedyUsed()
        {
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
            
            // CORREÇÃO: Não para os áudios durante flashback - permite que funcionem baseados na sanidade
            // Remove o código que parava tensão e heartbeat, deixando-os responder à sanidade atual
            
            // Recalcula tensão baseada na sanidade atual para iniciar flashback corretamente
            CalculateTensionValue();
        }
        
        private void HandleFlashbackEnded()
        {
            isInFlashback = false;
            
            // Restaura tensão baseada na sanidade atual
            CalculateTensionValue();
            
            // CORREÇÃO: Reinicia áudio de tensão e heartbeat se necessário baseado na sanidade atual
            InsanityManager insanityManager = FindFirstObjectByType<InsanityManager>();
            float currentSanity = insanityManager != null ? insanityManager.CurrentSanity : 1.0f;
            HandleSanityChange(currentSanity);
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
        }
        
        private void SetTensionParameter(float value)
        {
            if (!isSystemInitialized) return;
            
            // Converte o valor de tensão (0-2) para volume (0-1)
            // Multiplica pelo volume base configurável
            float normalizedValue = Mathf.Clamp01(value / maxTensionValue);
            currentTensionVolume = normalizedValue * tensionBaseVolume;
            
            // Controla volume diretamente no EventInstance FMOD
            tensionEventInstance.setVolume(currentTensionVolume);
        }
        
        private void UpdateHeartbeatAudio()
        {
            if (!isSystemInitialized) return;
            
            // Se o batimento cardíaco não está tocando, não faz nada
            if (!isHeartbeatAudioPlaying) return;
            
            // Calcula volume baseado na tensão atual (sincronizado)
            float normalizedTension = Mathf.Clamp01(currentTensionValue / maxTensionValue);
            currentHeartbeatVolume = normalizedTension * heartbeatBaseVolume;
            
            // Calcula velocidade baseada na sanidade (não na tensão)
            // Quando sanidade é baixa (perto do threshold mínimo), velocidade é alta
            float sanityFactor = 1f - Mathf.InverseLerp(tensionMaxIntensityThreshold, tensionActivationThreshold, currentSanity);
            sanityFactor = Mathf.Clamp01(sanityFactor);
            
            // Mapeia de 1.0 (velocidade normal) para maxHeartbeatSpeed
            float currentHeartbeatSpeed = Mathf.Lerp(1.0f, maxHeartbeatSpeed, sanityFactor);
            
            // Aplica volume e velocidade
            heartbeatEventInstance.setVolume(currentHeartbeatVolume);
            heartbeatEventInstance.setPitch(currentHeartbeatSpeed);
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
            heartbeatEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            isTensionAudioPlaying = false;
            isHeartbeatAudioPlaying = false;
        }
        
        /// <summary>
        /// Reinicia todos os sons ambiente
        /// </summary>
        public void RestartAllAmbientSounds()
        {
            if (!isSystemInitialized) return;
            
            StartAmbientSounds();
            CalculateTensionValue();
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
        }
        
        /// <summary>
        /// Atualiza o volume base do batimento cardíaco
        /// </summary>
        /// <param name="baseVolume">Novo volume base (0.0 a 1.0)</param>
        public void SetHeartbeatBaseVolume(float baseVolume)
        {
            heartbeatBaseVolume = Mathf.Clamp01(baseVolume);
            
            // Recalcula o volume atual do batimento cardíaco
            if (isHeartbeatAudioPlaying)
            {
                UpdateHeartbeatAudio();
            }
        }
        
        /// <summary>
        /// Atualiza a velocidade máxima do batimento cardíaco
        /// </summary>
        /// <param name="maxSpeed">Nova velocidade máxima (1.0 a 3.0)</param>
        public void SetMaxHeartbeatSpeed(float maxSpeed)
        {
            maxHeartbeatSpeed = Mathf.Clamp(maxSpeed, 1.0f, 3.0f);
            
            // Recalcula a velocidade atual do batimento cardíaco
            if (isHeartbeatAudioPlaying)
            {
                UpdateHeartbeatAudio();
            }
        }
        
        /// <summary>
        /// Atualiza todos os volumes de uma vez
        /// </summary>
        /// <param name="windVol">Volume do vento (0.0 a 1.0)</param>
        /// <param name="thunderVol">Volume do trovão (0.0 a 1.0)</param>
        /// <param name="tensionBaseVol">Volume base da tensão (0.0 a 1.0)</param>
        /// <param name="heartbeatBaseVol">Volume base do batimento cardíaco (0.0 a 1.0)</param>
        public void SetAllVolumes(float windVol, float thunderVol, float tensionBaseVol, float heartbeatBaseVol)
        {
            SetWindVolume(windVol);
            SetThunderVolume(thunderVol);
            SetTensionBaseVolume(tensionBaseVol);
            SetHeartbeatBaseVolume(heartbeatBaseVol);
        }
        
        /// <summary>
        /// Atualiza todos os volumes de uma vez (versão compatível com código anterior)
        /// </summary>
        /// <param name="windVol">Volume do vento (0.0 a 1.0)</param>
        /// <param name="thunderVol">Volume do trovão (0.0 a 1.0)</param>
        /// <param name="tensionBaseVol">Volume base da tensão (0.0 a 1.0)</param>
        public void SetAllVolumes(float windVol, float thunderVol, float tensionBaseVol)
        {
            SetAllVolumes(windVol, thunderVol, tensionBaseVol, heartbeatBaseVolume);
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
                heartbeatEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                
                windEventInstance.release();
                thunderEventInstance.release();
                tensionEventInstance.release();
                heartbeatEventInstance.release();
                
                isTensionAudioPlaying = false;
                isHeartbeatAudioPlaying = false;
            }
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
        
        public float WindMinDistance => windMinDistance;
        public float WindMaxDistance => windMaxDistance;
        public float ThunderMinDistance => thunderMinDistance;
        public float ThunderMaxDistance => thunderMaxDistance;

        public string GetSpatialRangeStats()
        {
            return $"Wind: {windMinDistance:F1}-{windMaxDistance:F1}m | Thunder: {thunderMinDistance:F1}-{thunderMaxDistance:F1}m";
        }
        
        #endregion
        
        #region Public Properties for Volume Controls    
        public float CurrentWindVolume => currentWindVolume;
        public float CurrentThunderVolume => currentThunderVolume;
        public float CurrentTensionVolume => currentTensionVolume;
        public float TensionBaseVolume => tensionBaseVolume;
        public float CurrentHeartbeatVolume => currentHeartbeatVolume;
        public float HeartbeatBaseVolume => heartbeatBaseVolume;
        public float MaxHeartbeatSpeed => maxHeartbeatSpeed;
        public bool IsHeartbeatPlaying => isHeartbeatAudioPlaying;
        
        public string GetVolumeStats()
        {
            return $"Wind: {currentWindVolume:F2} | Thunder: {currentThunderVolume:F2} | Tension: {currentTensionVolume:F2} (Base: {tensionBaseVolume:F2}) | Heartbeat: {currentHeartbeatVolume:F2} (Base: {heartbeatBaseVolume:F2})";
        }
    
        public string GetHeartbeatStats()
        {
            if (!isHeartbeatAudioPlaying)
                return "Heartbeat: Inactive";
                
            float sanityFactor = 1f - Mathf.InverseLerp(tensionMaxIntensityThreshold, tensionActivationThreshold, currentSanity);
            float currentSpeed = Mathf.Lerp(1.0f, maxHeartbeatSpeed, sanityFactor);
            
            return $"Heartbeat: Volume {currentHeartbeatVolume:F2} | Speed {currentSpeed:F2}x | Factor {sanityFactor:F2}";
        }
        
        #endregion
    }
}