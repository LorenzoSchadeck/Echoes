using UnityEngine;

namespace Echoes.Effects
{
    /// <summary>
    /// Versão SIMPLIFICADA do controlador de corrupção.
    /// Use quando as texturas de corrupção estão definidas diretamente no Material.
    /// Controla apenas intensidade e parâmetros, sem gerenciamento de texturas.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class SimpleCorruptionController : MonoBehaviour, ICorruptionController
    {
        [Header("⚙️ Corruption Parameters")]
        [SerializeField, Range(0f, 1f)] private float corruptionInfluence = 0f;
        [SerializeField, Range(0f, 2f)] private float corruptionNormalStrength = 1f;
        
        [Header("🔧 Mesh Deformation")]
        [Tooltip("Enable vertex deformation effects. Disable for objects that should only have visual corruption without geometry changes.")]
        [SerializeField] private bool enableMeshDeformation = true;
        [SerializeField, Range(0f, 5f)] private float deformStrength = 0f;
        [SerializeField, Range(0.1f, 10f)] private float deformFrequency = 1f;
        
        [Header("🧠 Sanity Integration")]
        [SerializeField] private bool useAutomaticSanityControl = true;
        [SerializeField] private AnimationCurve corruptionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve deformationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("🎯 Performance")]
        [SerializeField] private bool enableLODOptimization = true;
        [SerializeField] private float maxDistance = 50f;
        
        // Cached components
        private Renderer cachedRenderer;
        private Material materialInstance;
        private Camera playerCamera;
        
        // Shader property IDs (cached for performance)
        private static readonly int CorruptionInfluenceId = Shader.PropertyToID("_CorruptionInfluence");
        private static readonly int CorruptionNormalStrengthId = Shader.PropertyToID("_CorruptionNormalStrength");
        private static readonly int DeformStrengthId = Shader.PropertyToID("_DeformStrength");
        private static readonly int DeformFrequencyId = Shader.PropertyToID("_DeformFrequency");
        private static readonly int InsanityLevelId = Shader.PropertyToID("_InsanityLevel");
        
        // Current state
        private float currentInsanityLevel = 0f;
        private bool isVisible = true;
        private bool isInitialized = false;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeCorruption();
            RegisterWithManager();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            if (useAutomaticSanityControl)
            {
                UpdateFromSanitySystem();
            }
            
            if (enableLODOptimization)
            {
                UpdateLOD();
            }
            
            UpdateCorruptionEffects();
        }
        
        private void OnDestroy()
        {
            UnregisterFromManager();
            CleanupMaterial();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer == null)
            {
                Debug.LogError($"[SimpleCorruptionController] No Renderer found on {gameObject.name}");
                enabled = false;
                return;
            }
            
            // Create material instance
            materialInstance = new Material(cachedRenderer.sharedMaterial);
            cachedRenderer.material = materialInstance;
            
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindFirstObjectByType<Camera>();
        }
        
        private void InitializeCorruption()
        {
            if (materialInstance == null) return;
            
            // Force initial deformation state based on configuration
            if (!enableMeshDeformation)
            {
                deformStrength = 0f;
                materialInstance.SetFloat(DeformStrengthId, 0f);
                Debug.Log($"[SimpleCorruptionController] Mesh deformation DISABLED for {gameObject.name}");
            }
            
            // Set initial parameters
            UpdateCorruptionEffects();
            
            isInitialized = true;
            Debug.Log($"[SimpleCorruptionController] Initialized on {gameObject.name}");
        }
        
        private void RegisterWithManager()
        {
            var manager = CorruptionEffectsManager.Instance;
            if (manager != null)
            {
                manager.RegisterController((ICorruptionController)this);
            }
        }
        
        private void UnregisterFromManager()
        {
            var manager = CorruptionEffectsManager.Instance;
            if (manager != null)
            {
                manager.UnregisterController((ICorruptionController)this);
            }
        }
        
        private void CleanupMaterial()
        {
            if (materialInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(materialInstance);
                else
                    DestroyImmediate(materialInstance);
                    
                materialInstance = null;
            }
        }
        
        #endregion
        
        #region Corruption Control
        
        /// <summary>
        /// Updates corruption effects based on current parameters
        /// </summary>
        private void UpdateCorruptionEffects()
        {
            if (materialInstance == null) return;
            
            // Apply corruption influence (visual effects)
            materialInstance.SetFloat(CorruptionInfluenceId, corruptionInfluence);
            materialInstance.SetFloat(CorruptionNormalStrengthId, corruptionNormalStrength);
            
            // Apply deformation (only if enabled) - FORCE ZERO if disabled
            float finalDeformStrength = enableMeshDeformation ? deformStrength : 0f;
            materialInstance.SetFloat(DeformStrengthId, finalDeformStrength);
            materialInstance.SetFloat(DeformFrequencyId, deformFrequency);
            
            // Set insanity level for shader calculations
            materialInstance.SetFloat(InsanityLevelId, currentInsanityLevel);
            
            // Debug log when deformation is disabled but strength > 0
            if (!enableMeshDeformation && deformStrength > 0f)
            {
                Debug.Log($"[{gameObject.name}] Mesh deformation DISABLED - forcing DeformStrength to 0 (was {deformStrength})");
            }
        }
        
        /// <summary>
        /// Sets corruption intensity directly (0-1 range)
        /// </summary>
        public void SetCorruptionIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            
            // Use curves for more interesting progression
            corruptionInfluence = corruptionCurve.Evaluate(intensity);
            
            // Only apply deformation if enabled - ALWAYS FORCE ZERO when disabled
            if (enableMeshDeformation)
            {
                deformStrength = deformationCurve.Evaluate(intensity) * 5f; // Max deform strength
            }
            else
            {
                deformStrength = 0f; // FORCE no deformation
                
                // Debug check
                if (intensity > 0f)
                {
                    Debug.Log($"[{gameObject.name}] Mesh deformation DISABLED - DeformStrength forced to 0 despite intensity {intensity:F2}");
                }
            }
            
            // Progressive normal strength (more chaotic as corruption increases)
            corruptionNormalStrength = Mathf.Lerp(0.5f, 2f, intensity);
            
            // Force immediate update to ensure shader gets the zero value
            if (!enableMeshDeformation && materialInstance != null)
            {
                materialInstance.SetFloat(DeformStrengthId, 0f);
            }
        }
        
        #endregion
        
        #region Sanity System Integration
        
        /// <summary>
        /// Updates corruption based on the game's sanity system
        /// </summary>
        private void UpdateFromSanitySystem()
        {
            // Integração com o InsanityManager existente do projeto Echoes
            var insanityManager = FindFirstObjectByType<InsanityManager>();
            if (insanityManager != null)
            {
                // Converte sanidade (1.0 = são) para insanidade (0.0 = são)
                currentInsanityLevel = 1.0f - insanityManager.CurrentSanity;
                SetCorruptionIntensity(currentInsanityLevel);
            }
            else
            {
                // Fallback: simulate with sine wave for testing when InsanityManager not found
                if (Application.isPlaying)
                {
                    currentInsanityLevel = Mathf.Sin(Time.time * 0.1f) * 0.5f + 0.5f;
                    SetCorruptionIntensity(currentInsanityLevel);
                }
            }
        }
        
        #endregion
        
        #region Performance Optimization
        
        /// <summary>
        /// Updates LOD based on distance to player
        /// </summary>
        private void UpdateLOD()
        {
            if (playerCamera == null) return;
            
            float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
            bool shouldBeVisible = distance <= maxDistance;
            
            if (isVisible != shouldBeVisible)
            {
                isVisible = shouldBeVisible;
                cachedRenderer.enabled = isVisible;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Forces update of corruption effects
        /// </summary>
        public void ForceUpdate()
        {
            UpdateCorruptionEffects();
        }
        
        /// <summary>
        /// Gets current corruption intensity
        /// </summary>
        public float GetCorruptionIntensity()
        {
            return corruptionInfluence;
        }
        
        /// <summary>
        /// Checks if object is within render distance
        /// </summary>
        public bool IsVisible()
        {
            return isVisible;
        }
        
        /// <summary>
        /// Forces reconfiguration of mesh deformation setting
        /// Call this if you change enableMeshDeformation at runtime
        /// </summary>
        public void ReconfigureMeshDeformation()
        {
            if (!enableMeshDeformation)
            {
                deformStrength = 0f;
                if (materialInstance != null)
                {
                    materialInstance.SetFloat(DeformStrengthId, 0f);
                }
                Debug.Log($"[{gameObject.name}] Mesh deformation DISABLED via ReconfigureMeshDeformation()");
            }
            
            // Force update
            UpdateCorruptionEffects();
        }
        
        /// <summary>
        /// Gets current mesh deformation enabled state
        /// </summary>
        public bool IsMeshDeformationEnabled()
        {
            return enableMeshDeformation;
        }
        
        #endregion
    }
}