using UnityEngine;

namespace Echoes.Examples
{
    /// <summary>
    /// Exemplo de integração do sistema de corrupção aprimorado com o HorrorEventManager existente.
    /// Este script demonstra como conectar os novos efeitos visuais com o sistema de sanidade.
    /// </summary>
    public class CorruptionIntegrationExample : MonoBehaviour
    {
        [Header("🔗 System Integration")]
        [SerializeField] private bool enableIntegration = true;
        
        [Header("🧪 Testing Controls")]
        [SerializeField] private bool useTestingMode = false;
        [SerializeField, Range(0f, 1f)] private float testSanityLevel = 1f;
        [SerializeField] private KeyCode decreaseSanityKey = KeyCode.Q;
        [SerializeField] private KeyCode increaseSanityKey = KeyCode.E;
        [SerializeField] private KeyCode corruptionPulseKey = KeyCode.R;
        
        [Header("📊 Debug Display")]
        [SerializeField] private bool showDebugGUI = true;
        [SerializeField] private bool showDebugLog = false;
        
        // Private fields
        private float lastSanityValue = 1f;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (enableIntegration)
            {
                InitializeIntegration();
            }
            
            Debug.Log("[CorruptionIntegration] Example integration started");
        }
        
        private void Update()
        {
            if (useTestingMode)
            {
                HandleTestingInput();
            }
            
            if (enableIntegration)
            {
                UpdateIntegration();
            }
        }
        
        #endregion
        
        #region Integration Setup
        
        /// <summary>
        /// Initialize integration with existing horror systems
        /// </summary>
        private void InitializeIntegration()
        {
            // Example: Try to find and connect with existing HorrorEventManager
            /*
            var horrorEventManager = FindFirstObjectByType<HorrorEventManager>();
            if (horrorEventManager != null)
            {
                // Subscribe to existing sanity events
                horrorEventManager.OnSanityChanged += OnSanityChanged;
                horrorEventManager.OnHorrorEvent += OnHorrorEvent;
                
                if (showDebugLog)
                    Debug.Log("[CorruptionIntegration] Successfully connected to HorrorEventManager");
            }
            else
            {
                Debug.LogWarning("[CorruptionIntegration] HorrorEventManager not found. Using standalone mode.");
            }
            */
            
            // Ensure CorruptionEffectsManager exists
            if (Effects.CorruptionEffectsManager.Instance == null)
            {
                var managerPrefab = new GameObject("CorruptionEffectsManager");
                managerPrefab.AddComponent<Effects.CorruptionEffectsManager>();
                
                if (showDebugLog)
                    Debug.Log("[CorruptionIntegration] Created CorruptionEffectsManager instance");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle sanity level changes from the horror system
        /// </summary>
        /// <param name="newSanityLevel">New sanity level (0-1)</param>
        private void OnSanityChanged(float newSanityLevel)
        {
            if (Effects.CorruptionEffectsManager.Instance != null)
            {
                Effects.CorruptionEffectsManager.Instance.SetSanityLevel(newSanityLevel);
                
                if (showDebugLog)
                    Debug.Log($"[CorruptionIntegration] Sanity updated: {newSanityLevel:F2}");
            }
        }
        
        /// <summary>
        /// Handle specific horror events
        /// </summary>
        /// <param name="eventType">Type of horror event</param>
        /// <param name="intensity">Event intensity</param>
        private void OnHorrorEvent(string eventType, float intensity)
        {
            // Example: Trigger specific corruption effects based on horror events
            switch (eventType.ToLower())
            {
                case "jumpscare":
                    TriggerJumpscareCorruption(intensity);
                    break;
                    
                case "paranormal":
                    TriggerParanormalCorruption(intensity);
                    break;
                    
                case "psychological":
                    TriggerPsychologicalCorruption(intensity);
                    break;
                    
                default:
                    TriggerGenericCorruption(intensity);
                    break;
            }
        }
        
        #endregion
        
        #region Corruption Effects
        
        /// <summary>
        /// Trigger corruption effect for jumpscare events
        /// </summary>
        private void TriggerJumpscareCorruption(float intensity)
        {
            if (Effects.CorruptionEffectsManager.Instance != null)
            {
                // Quick, intense pulse
                Effects.CorruptionEffectsManager.Instance.TriggerGlobalCorruption(intensity, 0.5f);
            }
        }
        
        /// <summary>
        /// Trigger corruption effect for paranormal events
        /// </summary>
        private void TriggerParanormalCorruption(float intensity)
        {
            if (Effects.CorruptionEffectsManager.Instance != null)
            {
                // Slower, more eerie effect
                Effects.CorruptionEffectsManager.Instance.TriggerGlobalCorruption(intensity * 0.7f, 3f);
            }
        }
        
        /// <summary>
        /// Trigger corruption effect for psychological events
        /// </summary>
        private void TriggerPsychologicalCorruption(float intensity)
        {
            if (Effects.CorruptionEffectsManager.Instance != null)
            {
                // Gradual, building effect
                Effects.CorruptionEffectsManager.Instance.TriggerGlobalCorruption(intensity * 0.8f, 5f);
            }
        }
        
        /// <summary>
        /// Generic corruption trigger
        /// </summary>
        private void TriggerGenericCorruption(float intensity)
        {
            if (Effects.CorruptionEffectsManager.Instance != null)
            {
                Effects.CorruptionEffectsManager.Instance.TriggerGlobalCorruption(intensity, 2f);
            }
        }
        
        #endregion
        
        #region Testing Mode
        
        /// <summary>
        /// Handle testing input for development
        /// </summary>
        private void HandleTestingInput()
        {
            // Sanity control
            if (Input.GetKey(decreaseSanityKey))
            {
                testSanityLevel = Mathf.Max(0f, testSanityLevel - Time.deltaTime * 0.5f);
                OnSanityChanged(testSanityLevel);
            }
            
            if (Input.GetKey(increaseSanityKey))
            {
                testSanityLevel = Mathf.Min(1f, testSanityLevel + Time.deltaTime * 0.5f);
                OnSanityChanged(testSanityLevel);
            }
            
            // Corruption pulse
            if (Input.GetKeyDown(corruptionPulseKey))
            {
                TriggerGenericCorruption(1f);
            }
        }
        
        #endregion
        
        #region Integration Updates
        
        /// <summary>
        /// Update integration systems
        /// </summary>
        private void UpdateIntegration()
        {
            // Example: Poll sanity from existing system if events aren't available
            /*
            var horrorEventManager = FindFirstObjectByType<HorrorEventManager>();
            if (horrorEventManager != null)
            {
                float currentSanity = horrorEventManager.GetCurrentSanityLevel();
                if (Mathf.Abs(currentSanity - lastSanityValue) > 0.01f)
                {
                    OnSanityChanged(currentSanity);
                    lastSanityValue = currentSanity;
                }
            }
            */
        }
        
        #endregion
        
        #region Debug GUI
        
        private void OnGUI()
        {
            if (!showDebugGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label("🎭 Enhanced Corruption System - Debug");
            GUILayout.Space(10);
            
            // System status
            if (Effects.CorruptionEffectsManager.Instance != null)
            {
                GUILayout.Label("📊 System Status:");
                GUILayout.Label(Effects.CorruptionEffectsManager.Instance.GetDebugInfo());
                GUILayout.Space(10);
            }
            
            // Testing controls
            if (useTestingMode)
            {
                GUILayout.Label("🧪 Testing Controls:");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Sanity: {testSanityLevel:F2}");
                testSanityLevel = GUILayout.HorizontalSlider(testSanityLevel, 0f, 1f);
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button("Trigger Corruption Pulse"))
                {
                    TriggerGenericCorruption(1f);
                }
                
                if (GUILayout.Button("Reset All Corruption"))
                {
                    if (Effects.CorruptionEffectsManager.Instance != null)
                    {
                        Effects.CorruptionEffectsManager.Instance.ResetAllCorruption();
                    }
                }
                
                GUILayout.Space(10);
                GUILayout.Label($"Controls: {decreaseSanityKey} / {increaseSanityKey} (Sanity), {corruptionPulseKey} (Pulse)");
            }
            
            // Integration status
            GUILayout.Label("🔗 Integration Status:");
            GUILayout.Label($"Integration Enabled: {enableIntegration}");
            GUILayout.Label($"Testing Mode: {useTestingMode}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Manually trigger corruption from external systems
        /// </summary>
        public void TriggerCorruptionEffect(string effectType, float intensity, float duration = 2f)
        {
            switch (effectType.ToLower())
            {
                case "jumpscare":
                    TriggerJumpscareCorruption(intensity);
                    break;
                    
                case "paranormal":
                    TriggerParanormalCorruption(intensity);
                    break;
                    
                case "psychological":
                    TriggerPsychologicalCorruption(intensity);
                    break;
                    
                default:
                    if (Effects.CorruptionEffectsManager.Instance != null)
                    {
                        Effects.CorruptionEffectsManager.Instance.TriggerGlobalCorruption(intensity, duration);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Enable or disable the integration system
        /// </summary>
        public void SetIntegrationEnabled(bool enabled)
        {
            enableIntegration = enabled;
            
            if (enabled)
            {
                InitializeIntegration();
            }
        }
        
        #endregion
    }
}

#if UNITY_EDITOR
namespace Echoes.Examples.Editor
{
    using UnityEditor;
    
    [CustomEditor(typeof(CorruptionIntegrationExample))]
    public class CorruptionIntegrationExampleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(20);
            GUILayout.Label("🎭 Quick Actions", EditorStyles.boldLabel);
            
            var example = (CorruptionIntegrationExample)target;
            
            if (GUILayout.Button("🧪 Test Corruption Pulse"))
            {
                example.TriggerCorruptionEffect("generic", 1f, 2f);
            }
            
            if (GUILayout.Button("👻 Test Paranormal Effect"))
            {
                example.TriggerCorruptionEffect("paranormal", 0.8f, 3f);
            }
            
            if (GUILayout.Button("😱 Test Jumpscare Effect"))
            {
                example.TriggerCorruptionEffect("jumpscare", 1f, 0.5f);
            }
            
            if (Application.isPlaying && Effects.CorruptionEffectsManager.Instance != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("📊 Runtime Info:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(Effects.CorruptionEffectsManager.Instance.GetDebugInfo(), MessageType.Info);
            }
        }
    }
}
#endif