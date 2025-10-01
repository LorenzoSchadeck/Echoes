using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Echoes.Effects
{
    /// <summary>
    /// Manager global para efeitos de corrupção baseados em sanidade.
    /// Integra-se com o HorrorEventManager existente para criar efeitos coordenados.
    /// </summary>
    public class CorruptionEffectsManager : MonoBehaviour
    {
        [System.Serializable]
        public class CorruptionThreshold
        {
            [Header("🎯 Threshold Settings")]
            public string name = "New Threshold";
            [Range(0f, 1f)] public float sanityThreshold = 0.5f;
            
            [Header("🎨 Visual Effects")]
            [Range(0f, 1f)] public float corruptionIntensity = 0.5f;
            [Range(0f, 5f)] public float deformationStrength = 1f;
            [Range(0.1f, 10f)] public float deformationFrequency = 2f;
            [Range(0f, 2f)] public float normalStrength = 1f;
            
            [Header("⏱️ Timing")]
            public float transitionDuration = 2f;
            public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            
            [Header("🎵 Audio")]
            public AudioClip corruptionSound;
            [Range(0f, 1f)] public float soundVolume = 0.7f;
        }
        
        [Header("🧠 Sanity Integration")]
        [SerializeField] private bool autoDetectSanitySystem = true;
        [SerializeField] private float updateFrequency = 0.1f;
        
        [Header("🎚️ Corruption Thresholds")]
        [SerializeField] private List<CorruptionThreshold> thresholds = new List<CorruptionThreshold>
        {
            new CorruptionThreshold 
            { 
                name = "Light Anxiety", 
                sanityThreshold = 0.8f, 
                corruptionIntensity = 0.2f,
                deformationStrength = 0.5f 
            },
            new CorruptionThreshold 
            { 
                name = "Growing Unease", 
                sanityThreshold = 0.6f, 
                corruptionIntensity = 0.4f,
                deformationStrength = 1.5f 
            },
            new CorruptionThreshold 
            { 
                name = "Mental Strain", 
                sanityThreshold = 0.4f, 
                corruptionIntensity = 0.7f,
                deformationStrength = 3f 
            },
            new CorruptionThreshold 
            { 
                name = "Psychological Break", 
                sanityThreshold = 0.2f, 
                corruptionIntensity = 1f,
                deformationStrength = 5f 
            }
        };
        
        [Header("🎯 Performance")]
        [SerializeField] private int maxActiveCorruptions = 50;
        [SerializeField] private float cullingDistance = 100f;
        [SerializeField] private LayerMask corruptionLayers = -1;
        
        // Private fields
        private List<EnhancedCorruptionController> registeredControllers = new List<EnhancedCorruptionController>();
        private List<ICorruptionController> simpleControllers = new List<ICorruptionController>();
        private float currentSanityLevel = 1f;
        private int currentThresholdIndex = -1;
        private float lastUpdateTime;
        private AudioSource audioSource;
        
        // Static instance for easy access
        public static CorruptionEffectsManager Instance { get; private set; }
        
        // Events
        public System.Action<float> OnSanityChanged;
        public System.Action<int> OnThresholdChanged;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Sort thresholds by sanity level (highest to lowest)
            thresholds.Sort((a, b) => b.sanityThreshold.CompareTo(a.sanityThreshold));
            
            if (autoDetectSanitySystem)
            {
                DetectAndIntegrateWithHorrorSystem();
            }
            
            // Auto-register existing corruption controllers
            RegisterAllCorruptionControllers();
        }
        
        private void Update()
        {
            if (Time.time - lastUpdateTime >= updateFrequency)
            {
                UpdateCorruptionEffects();
                lastUpdateTime = Time.time;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeSystem()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
            
            Debug.Log("[CorruptionEffectsManager] System initialized");
        }
        
        private void DetectAndIntegrateWithHorrorSystem()
        {
            // Integração com o InsanityManager existente do projeto Echoes
            var insanityManager = FindFirstObjectByType<InsanityManager>();
            if (insanityManager != null)
            {
                // Conecta ao evento de mudança de sanidade
                InsanityManager.OnSanityChanged += UpdateSanityLevel;
                
                // Obtém o valor inicial de sanidade
                UpdateSanityLevel(insanityManager.CurrentSanity);
                
                Debug.Log("[CorruptionEffectsManager] Successfully integrated with InsanityManager");
            }
            else
            {
                Debug.LogWarning("[CorruptionEffectsManager] InsanityManager not found. Running in standalone mode.");
            }
            
            Debug.Log("[CorruptionEffectsManager] Auto-detection enabled");
        }
        
        #endregion
        
        #region Controller Management
        
        /// <summary>
        /// Register a corruption controller with the manager
        /// </summary>
        public void RegisterController(EnhancedCorruptionController controller)
        {
            if (!registeredControllers.Contains(controller))
            {
                registeredControllers.Add(controller);
                
                // Apply current corruption state to new controller
                ApplyCorruptionToController(controller, currentSanityLevel);
                
                Debug.Log($"[CorruptionEffectsManager] Registered controller: {controller.name}");
            }
        }
        
        /// <summary>
        /// Unregister a corruption controller
        /// </summary>
        public void UnregisterController(EnhancedCorruptionController controller)
        {
            if (registeredControllers.Contains(controller))
            {
                registeredControllers.Remove(controller);
                Debug.Log($"[CorruptionEffectsManager] Unregistered controller: {controller.name}");
            }
        }
        
        /// <summary>
        /// Register a simple corruption controller with the manager
        /// </summary>
        public void RegisterController(ICorruptionController controller)
        {
            if (!simpleControllers.Contains(controller))
            {
                simpleControllers.Add(controller);
                
                // Apply current corruption state to new controller
                float intensity = Mathf.Clamp01(1f - currentSanityLevel);
                controller.SetCorruptionIntensity(intensity);
                
                Debug.Log($"[CorruptionEffectsManager] Registered simple controller: {controller.name}");
            }
        }
        
        /// <summary>
        /// Unregister a simple corruption controller
        /// </summary>
        public void UnregisterController(ICorruptionController controller)
        {
            if (simpleControllers.Contains(controller))
            {
                simpleControllers.Remove(controller);
                Debug.Log($"[CorruptionEffectsManager] Unregistered simple controller: {controller.name}");
            }
        }
        
        /// <summary>
        /// Automatically find and register all corruption controllers in the scene
        /// </summary>
        private void RegisterAllCorruptionControllers()
        {
            var controllers = FindObjectsByType<EnhancedCorruptionController>(FindObjectsSortMode.None);
            foreach (var controller in controllers)
            {
                RegisterController(controller);
            }
            
            Debug.Log($"[CorruptionEffectsManager] Auto-registered {controllers.Length} controllers");
        }
        
        #endregion
        
        #region Sanity Management
        
        /// <summary>
        /// Update the current sanity level and trigger effects
        /// </summary>
        public void UpdateSanityLevel(float sanityLevel)
        {
            sanityLevel = Mathf.Clamp01(sanityLevel);
            
            if (Mathf.Abs(currentSanityLevel - sanityLevel) < 0.01f)
                return; // Avoid unnecessary updates
            
            float previousSanity = currentSanityLevel;
            currentSanityLevel = sanityLevel;
            
            // Check for threshold changes
            int newThresholdIndex = GetCurrentThresholdIndex(sanityLevel);
            if (newThresholdIndex != currentThresholdIndex)
            {
                OnThresholdChanged?.Invoke(newThresholdIndex);
                TriggerThresholdEffects(newThresholdIndex, previousSanity < sanityLevel);
                currentThresholdIndex = newThresholdIndex;
            }
            
            OnSanityChanged?.Invoke(sanityLevel);
        }
        
        /// <summary>
        /// Get the current threshold index based on sanity level
        /// </summary>
        private int GetCurrentThresholdIndex(float sanityLevel)
        {
            for (int i = 0; i < thresholds.Count; i++)
            {
                if (sanityLevel <= thresholds[i].sanityThreshold)
                {
                    return i;
                }
            }
            return -1; // No threshold active
        }
        
        #endregion
        
        #region Corruption Effects
        
        /// <summary>
        /// Update all registered corruption controllers
        /// </summary>
        private void UpdateCorruptionEffects()
        {
            // Clean up destroyed controllers
            registeredControllers.RemoveAll(controller => controller == null);
            simpleControllers.RemoveAll(controller => controller == null);
            
            // Limit active corruptions for performance
            int activeCount = 0;
            
            // Process enhanced controllers
            foreach (var controller in registeredControllers)
            {
                if (activeCount >= maxActiveCorruptions)
                    break;
                
                if (ShouldUpdateController(controller))
                {
                    ApplyCorruptionToController(controller, currentSanityLevel);
                    activeCount++;
                }
            }
            
            // Process simple controllers
            foreach (var controller in simpleControllers)
            {
                if (activeCount >= maxActiveCorruptions)
                    break;
                
                if (controller != null && controller.IsVisible())
                {
                    float intensity = Mathf.Clamp01(1f - currentSanityLevel);
                    controller.SetCorruptionIntensity(intensity);
                    activeCount++;
                }
            }
        }
        
        /// <summary>
        /// Check if a controller should be updated (distance culling, etc.)
        /// </summary>
        private bool ShouldUpdateController(EnhancedCorruptionController controller)
        {
            if (controller == null) return false;
            
            // Check distance culling
            var playerCamera = Camera.main;
            if (playerCamera != null)
            {
                float distance = Vector3.Distance(controller.transform.position, playerCamera.transform.position);
                if (distance > cullingDistance)
                    return false;
            }
            
            // Check layer mask
            int controllerLayer = 1 << controller.gameObject.layer;
            if ((corruptionLayers & controllerLayer) == 0)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Apply corruption effects to a specific controller
        /// </summary>
        private void ApplyCorruptionToController(EnhancedCorruptionController controller, float sanityLevel)
        {
            if (controller == null) return;
            
            // Get the appropriate threshold
            int thresholdIndex = GetCurrentThresholdIndex(sanityLevel);
            if (thresholdIndex >= 0 && thresholdIndex < thresholds.Count)
            {
                var threshold = thresholds[thresholdIndex];
                
                // Calculate smooth corruption intensity
                float intensity = CalculateCorruptionIntensity(sanityLevel, threshold);
                controller.SetCorruptionIntensity(intensity);
            }
            else
            {
                // No corruption
                controller.SetCorruptionIntensity(0f);
            }
        }
        
        /// <summary>
        /// Calculate smooth corruption intensity based on sanity and threshold
        /// </summary>
        private float CalculateCorruptionIntensity(float sanityLevel, CorruptionThreshold threshold)
        {
            // Use the threshold curve for smooth transitions
            float normalizedProgress = 1f - (sanityLevel / threshold.sanityThreshold);
            normalizedProgress = Mathf.Clamp01(normalizedProgress);
            
            return threshold.transitionCurve.Evaluate(normalizedProgress) * threshold.corruptionIntensity;
        }
        
        #endregion
        
        #region Threshold Effects
        
        /// <summary>
        /// Trigger effects when crossing a sanity threshold
        /// </summary>
        private void TriggerThresholdEffects(int thresholdIndex, bool isRecovering)
        {
            if (thresholdIndex < 0 || thresholdIndex >= thresholds.Count)
                return;
            
            var threshold = thresholds[thresholdIndex];
            
            // Play audio effect
            if (threshold.corruptionSound != null && audioSource != null)
            {
                audioSource.pitch = isRecovering ? 1.2f : 0.8f; // Higher pitch when recovering
                audioSource.PlayOneShot(threshold.corruptionSound, threshold.soundVolume);
            }
            
            // Trigger visual pulse effect on all controllers
            StartCoroutine(ThresholdPulseEffect(threshold, isRecovering));
            
            Debug.Log($"[CorruptionEffectsManager] Threshold triggered: {threshold.name} (Recovering: {isRecovering})");
        }
        
        /// <summary>
        /// Visual pulse effect when crossing thresholds
        /// </summary>
        private System.Collections.IEnumerator ThresholdPulseEffect(CorruptionThreshold threshold, bool isRecovering)
        {
            float duration = threshold.transitionDuration;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Apply pulse effect to all controllers
                foreach (var controller in registeredControllers)
                {
                    if (controller != null && ShouldUpdateController(controller))
                    {
                        float pulseIntensity = threshold.transitionCurve.Evaluate(progress);
                        if (isRecovering)
                            pulseIntensity = 1f - pulseIntensity;
                        
                        // Temporarily boost corruption for pulse effect
                        float baseIntensity = CalculateCorruptionIntensity(currentSanityLevel, threshold);
                        float pulsedIntensity = baseIntensity + (pulseIntensity * 0.3f);
                        
                        controller.SetCorruptionIntensity(Mathf.Clamp01(pulsedIntensity));
                    }
                }
                
                yield return null;
            }
            
            // Return to normal corruption levels
            UpdateCorruptionEffects();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Manually set sanity level (for testing or external systems)
        /// </summary>
        public void SetSanityLevel(float sanityLevel)
        {
            UpdateSanityLevel(sanityLevel);
        }
        
        /// <summary>
        /// Get current sanity level
        /// </summary>
        public float GetCurrentSanityLevel()
        {
            return currentSanityLevel;
        }
        
        /// <summary>
        /// Trigger corruption effect on all controllers
        /// </summary>
        public void TriggerGlobalCorruption(float intensity, float duration = 2f)
        {
            foreach (var controller in registeredControllers)
            {
                if (controller != null)
                {
                    controller.TriggerCorruption(intensity, duration);
                }
            }
        }
        
        /// <summary>
        /// Reset all corruption effects
        /// </summary>
        public void ResetAllCorruption()
        {
            currentSanityLevel = 1f;
            currentThresholdIndex = -1;
            
            foreach (var controller in registeredControllers)
            {
                if (controller != null)
                {
                    controller.ResetCorruption();
                }
            }
        }
        
        #endregion
        
        #region Debug and Editor Support
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnDrawGizmosSelected()
        {
            // Draw culling distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, cullingDistance);
            
            // Draw registered controllers
            Gizmos.color = Color.cyan;
            foreach (var controller in registeredControllers)
            {
                if (controller != null)
                {
                    Gizmos.DrawLine(transform.position, controller.transform.position);
                }
            }
        }
        
        /// <summary>
        /// Get debug information about the corruption system
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Sanity: {currentSanityLevel:F2} | " +
                   $"Threshold: {(currentThresholdIndex >= 0 ? thresholds[currentThresholdIndex].name : "None")} | " +
                   $"Controllers: {registeredControllers.Count} | " +
                   $"Active: {registeredControllers.Count(c => c != null && ShouldUpdateController(c))}";
        }
        
        #endregion
    }
}