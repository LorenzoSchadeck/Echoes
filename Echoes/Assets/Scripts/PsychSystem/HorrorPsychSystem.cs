using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace Echoes.PsychSystem
{
    /// <summary>
    /// Sistema central de terror psicológico que integra sanidade, eventos e corrupção visual.
    /// Substitui o antigo CorruptionEffectsManager com arquitetura mais robusta.
    /// </summary>
    public class HorrorPsychSystem : MonoBehaviour
    {
        #region Singleton
        public static HorrorPsychSystem Instance { get; private set; }
        #endregion
        
        #region Events
        public static event Action<float> OnSanityChanged;
        public static event Action<SanityThreshold> OnThresholdEntered;
        public static event Action<SanityThreshold> OnThresholdExited;
        public static event Action<string> OnHorrorEventTriggered;
        #endregion
        
        [Header("🧠 Sanity Thresholds")]
        [Tooltip("Lista de limiares de sanidade configurados em ordem crescente")]
        [SerializeField] private List<SanityThreshold> sanityThresholds = new List<SanityThreshold>();
        
        [Header("⚙️ System Configuration")]
        [Tooltip("Frequência de atualização do sistema (em segundos)")]
        [SerializeField, Range(0.01f, 1f)] private float updateFrequency = 0.1f;
        
        [Tooltip("Máximo de objetos corrompidos processados por frame")]
        [SerializeField] private int maxCorruptedObjectsPerFrame = 50;
        
        [Tooltip("Distância máxima para aplicar efeitos de corrupção")]
        [SerializeField] private float maxCorruptionDistance = 100f;
        
        [Header("🎯 Performance")]
        [Tooltip("Ativar otimizações de performance")]
        [SerializeField] private bool enablePerformanceOptimizations = true;
        
        [Tooltip("Usar culling por distância")]
        [SerializeField] private bool useDistanceCulling = true;
        
        [Header("🔊 Audio Integration")]
        [Tooltip("AudioSource para sons de transição de limiar")]
        [SerializeField] private AudioSource thresholdAudioSource;
        
        [Header("🐛 Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool enablePerformanceLogs = false;
        
        #region Private Fields
        private float currentSanity = 1f;
        private SanityThreshold currentThreshold;
        private SanityThreshold previousThreshold;
        
        private List<PsychCorruptionController> registeredControllers = new List<PsychCorruptionController>();
        private Camera playerCamera;
        private InsanityManager insanityManager;
        private HorrorEventManager horrorEventManager;
        
        private Coroutine updateCoroutine;
        private float lastUpdateTime;
        private int processedControllersThisFrame;
        
        // Performance tracking
        private float performanceTimer;
        private int totalUpdatesThisSecond;
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeSingleton();
            ValidateConfiguration();
        }
        
        private void Start()
        {
            InitializeSystem();
            StartUpdateCoroutine();
        }
        
        private void OnDestroy()
        {
            CleanupSystem();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void ValidateConfiguration()
        {
            // Ordena thresholds por valor mínimo de sanidade
            sanityThresholds = sanityThresholds.OrderBy(t => t.minSanityValue).ToList();
            
            // Valida overlaps
            for (int i = 0; i < sanityThresholds.Count - 1; i++)
            {
                if (sanityThresholds[i].maxSanityValue > sanityThresholds[i + 1].minSanityValue)
                {
                    Debug.LogWarning($"[HorrorPsychSystem] Threshold overlap detected between '{sanityThresholds[i].name}' and '{sanityThresholds[i + 1].name}'");
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[HorrorPsychSystem] Configured {sanityThresholds.Count} sanity thresholds");
            }
        }
        
        private void InitializeSystem()
        {
            // Encontra componentes necessários
            playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            insanityManager = FindFirstObjectByType<InsanityManager>();
            horrorEventManager = FindFirstObjectByType<HorrorEventManager>();
            
            // Configura AudioSource se não especificado
            if (thresholdAudioSource == null)
            {
                thresholdAudioSource = GetComponent<AudioSource>();
                if (thresholdAudioSource == null)
                {
                    thresholdAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Conecta eventos
            if (insanityManager != null)
            {
                InsanityManager.OnSanityChanged += HandleSanityChanged;
                if (enableDebugLogs)
                {
                    Debug.Log("[HorrorPsychSystem] Successfully connected to InsanityManager");
                }
            }
            else
            {
                Debug.LogWarning("[HorrorPsychSystem] InsanityManager not found!");
            }
            
            // Auto-detecta controladores existentes
            AutoRegisterExistingControllers();
            
            // Define threshold inicial
            UpdateCurrentThreshold(currentSanity);
        }
        
        private void AutoRegisterExistingControllers()
        {
            var existingControllers = FindObjectsByType<PsychCorruptionController>(FindObjectsSortMode.None);
            foreach (var controller in existingControllers)
            {
                RegisterController(controller);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[HorrorPsychSystem] Auto-registered {existingControllers.Length} existing controllers");
            }
        }
        
        #endregion
        
        #region Sanity Management
        
        private void HandleSanityChanged(float newSanity)
        {
            float previousSanity = currentSanity;
            currentSanity = Mathf.Clamp01(newSanity);
            
            // Atualiza threshold atual
            UpdateCurrentThreshold(currentSanity);
            
            // Dispara evento
            OnSanityChanged?.Invoke(currentSanity);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[HorrorPsychSystem] Sanity changed: {previousSanity:F2} → {currentSanity:F2}");
            }
        }
        
        private void UpdateCurrentThreshold(float sanityValue)
        {
            SanityThreshold newThreshold = GetThresholdForSanity(sanityValue);
            
            if (newThreshold != currentThreshold)
            {
                // Threshold mudou
                previousThreshold = currentThreshold;
                currentThreshold = newThreshold;
                
                // Eventos de entrada/saída
                if (previousThreshold != null)
                {
                    OnThresholdExited?.Invoke(previousThreshold);
                }
                
                if (currentThreshold != null)
                {
                    OnThresholdEntered?.Invoke(currentThreshold);
                    PlayThresholdAudio(currentThreshold);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[HorrorPsychSystem] Entered threshold: {currentThreshold.name}");
                    }
                }
            }
        }
        
        private SanityThreshold GetThresholdForSanity(float sanityValue)
        {
            return sanityThresholds.FirstOrDefault(t => t.IsWithinThreshold(sanityValue));
        }
        
        private void PlayThresholdAudio(SanityThreshold threshold)
        {
            if (threshold.thresholdAudio != null && thresholdAudioSource != null)
            {
                thresholdAudioSource.clip = threshold.thresholdAudio;
                thresholdAudioSource.volume = threshold.audioVolume;
                thresholdAudioSource.Play();
            }
        }
        
        #endregion
        
        #region Controller Management
        
        public void RegisterController(PsychCorruptionController controller)
        {
            if (controller == null || registeredControllers.Contains(controller)) 
                return;
            
            registeredControllers.Add(controller);
            
            // Aplica estado atual imediatamente
            if (currentThreshold != null)
            {
                controller.ApplyCorruption(currentSanity, currentThreshold);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"[HorrorPsychSystem] Registered controller: {controller.name}");
            }
        }
        
        public void UnregisterController(PsychCorruptionController controller)
        {
            if (registeredControllers.Remove(controller) && enableDebugLogs)
            {
                Debug.Log($"[HorrorPsychSystem] Unregistered controller: {controller.name}");
            }
        }
        
        #endregion
        
        #region Update System
        
        private void StartUpdateCoroutine()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            
            updateCoroutine = StartCoroutine(UpdateControllersCoroutine());
        }
        
        private IEnumerator UpdateControllersCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(updateFrequency);
                
                UpdateControllers();
                
                if (enablePerformanceLogs)
                {
                    TrackPerformance();
                }
            }
        }
        
        private void UpdateControllers()
        {
            if (currentThreshold == null) return;
            
            // Remove controladores nulos
            registeredControllers.RemoveAll(c => c == null);
            
            processedControllersThisFrame = 0;
            
            foreach (var controller in registeredControllers)
            {
                if (processedControllersThisFrame >= maxCorruptedObjectsPerFrame)
                    break;
                
                if (ShouldUpdateController(controller))
                {
                    controller.ApplyCorruption(currentSanity, currentThreshold);
                    processedControllersThisFrame++;
                }
            }
            
            totalUpdatesThisSecond++;
        }
        
        private bool ShouldUpdateController(PsychCorruptionController controller)
        {
            if (controller == null || !controller.isActiveAndEnabled)
                return false;
            
            if (useDistanceCulling && playerCamera != null)
            {
                float distance = Vector3.Distance(controller.transform.position, playerCamera.transform.position);
                if (distance > maxCorruptionDistance)
                    return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Performance Tracking
        
        private void TrackPerformance()
        {
            performanceTimer += updateFrequency;
            
            if (performanceTimer >= 1f)
            {
                Debug.Log($"[HorrorPsychSystem] Performance: {totalUpdatesThisSecond} updates/sec | " +
                         $"Controllers: {registeredControllers.Count} | " +
                         $"Active: {processedControllersThisFrame}");
                
                performanceTimer = 0f;
                totalUpdatesThisSecond = 0;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Obtém o threshold atual
        /// </summary>
        public SanityThreshold GetCurrentThreshold()
        {
            return currentThreshold;
        }
        
        /// <summary>
        /// Obtém a sanidade atual
        /// </summary>
        public float GetCurrentSanity()
        {
            return currentSanity;
        }
        
        /// <summary>
        /// Força atualização imediata de todos os controladores
        /// </summary>
        public void ForceUpdateAllControllers()
        {
            if (currentThreshold == null) return;
            
            foreach (var controller in registeredControllers)
            {
                if (controller != null)
                {
                    controller.ApplyCorruption(currentSanity, currentThreshold);
                }
            }
        }
        
        /// <summary>
        /// Obtém estatísticas do sistema
        /// </summary>
        public string GetSystemStats()
        {
            return $"Controllers: {registeredControllers.Count} | " +
                   $"Current Threshold: {currentThreshold?.name ?? "None"} | " +
                   $"Sanity: {currentSanity:F2}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void CleanupSystem()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
            
            if (insanityManager != null)
            {
                InsanityManager.OnSanityChanged -= HandleSanityChanged;
            }
            
            registeredControllers.Clear();
        }
        
        #endregion
        
        #region Editor Support
        
        [ContextMenu("Force Update All Controllers")]
        private void EditorForceUpdateAllControllers()
        {
            ForceUpdateAllControllers();
        }
        
        [ContextMenu("Log System Stats")]
        private void EditorLogSystemStats()
        {
            Debug.Log($"[HorrorPsychSystem] {GetSystemStats()}");
        }
        
        #endregion
    }
}