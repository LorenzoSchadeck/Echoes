using UnityEngine;

namespace Echoes.Effects
{
    /// <summary>
    /// Controlador avançado para efeitos de corrupção com mapas PBR completos.
    /// Integra-se com o sistema de sanidade para criar efeitos visuais progressivos.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class EnhancedCorruptionController : MonoBehaviour, ICorruptionController
    {
        [Header("🎭 Dynamic Corruption Override (Optional)")]
        [Tooltip("Leave empty to use textures from Material. Only fill if you need dynamic texture swapping.")]
        [SerializeField] private Texture2D corruptionBaseMap;
        [SerializeField] private Texture2D corruptionNormalMap;
        [SerializeField] private Texture2D corruptionMetallicMap;
        [SerializeField] private Texture2D corruptionOcclusionMap;
        [SerializeField] private Texture2D corruptionHeightMap;
        
        [Header("🔄 Advanced Features")]
        [SerializeField] private bool enableDynamicTextureSwapping = false;
        
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
        private static readonly int CorruptionMapId = Shader.PropertyToID("_CorruptionMap");
        private static readonly int CorruptionNormalMapId = Shader.PropertyToID("_CorruptionNormalMap");
        private static readonly int CorruptionMetallicMapId = Shader.PropertyToID("_CorruptionMetallicMap");
        private static readonly int CorruptionOcclusionMapId = Shader.PropertyToID("_CorruptionOcclusionMap");
        private static readonly int CorruptionHeightMapId = Shader.PropertyToID("_CorruptionHeightMap");
        private static readonly int CorruptionInfluenceId = Shader.PropertyToID("_CorruptionInfluence");
        private static readonly int CorruptionNormalStrengthId = Shader.PropertyToID("_CorruptionNormalStrength");
        private static readonly int DeformStrengthId = Shader.PropertyToID("_DeformStrength");
        private static readonly int DeformFrequencyId = Shader.PropertyToID("_DeformFrequency");
        private static readonly int InsanityLevelId = Shader.PropertyToID("_InsanityLevel");
        
        // Current state
        private float currentInsanityLevel = 0f;
        private float distanceToPlayer;
        private bool isInLODRange = true;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeMaterial();
            playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            
            // Force initial deformation state based on configuration
            if (!enableMeshDeformation)
            {
                deformStrength = 0f;
                if (materialInstance != null)
                {
                    materialInstance.SetFloat(DeformStrengthId, 0f);
                    Debug.Log($"[EnhancedCorruptionController] Mesh deformation DISABLED for {gameObject.name}");
                }
            }
        }
        
        private void Update()
        {
            if (enableLODOptimization)
            {
                UpdateLOD();
            }
            
            if (useAutomaticSanityControl)
            {
                UpdateFromSanitySystem();
            }
            
            if (isInLODRange)
            {
                UpdateCorruptionEffects();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer == null)
            {
                Debug.LogError($"[EnhancedCorruptionController] Renderer not found on {gameObject.name}!", this);
                enabled = false;
            }
        }
        
        private void InitializeMaterial()
        {
            if (cachedRenderer.material != null)
            {
                // Create material instance to avoid affecting other objects
                materialInstance = new Material(cachedRenderer.material);
                cachedRenderer.material = materialInstance;
                
                // Set initial corruption textures
                SetCorruptionTextures();
                
                Debug.Log($"[EnhancedCorruptionController] Initialized on {gameObject.name}");
            }
            else
            {
                Debug.LogError($"[EnhancedCorruptionController] No material found on renderer for {gameObject.name}!", this);
                enabled = false;
            }
        }
        
        #endregion
        
        #region Corruption Control
        
        /// <summary>
        /// Sets corruption textures to the material (only if dynamic swapping is enabled)
        /// In most cases, textures should be set directly in the Material instead.
        /// </summary>
        private void SetCorruptionTextures()
        {
            if (materialInstance == null || !enableDynamicTextureSwapping) return;
            
            // Only override material textures if dynamic swapping is enabled
            if (corruptionBaseMap != null)
                materialInstance.SetTexture(CorruptionMapId, corruptionBaseMap);
                
            if (corruptionNormalMap != null)
                materialInstance.SetTexture(CorruptionNormalMapId, corruptionNormalMap);
                
            if (corruptionMetallicMap != null)
                materialInstance.SetTexture(CorruptionMetallicMapId, corruptionMetallicMap);
                
            if (corruptionOcclusionMap != null)
                materialInstance.SetTexture(CorruptionOcclusionMapId, corruptionOcclusionMap);
                
            if (corruptionHeightMap != null)
                materialInstance.SetTexture(CorruptionHeightMapId, corruptionHeightMap);
        }
        
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
        
        #region LOD and Performance
        
        /// <summary>
        /// Updates Level of Detail based on distance to player
        /// </summary>
        private void UpdateLOD()
        {
            if (playerCamera == null) return;
            
            distanceToPlayer = Vector3.Distance(transform.position, playerCamera.transform.position);
            bool wasInRange = isInLODRange;
            isInLODRange = distanceToPlayer <= maxDistance;
            
            // Disable/enable renderer based on distance
            if (wasInRange != isInLODRange)
            {
                cachedRenderer.enabled = isInLODRange;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Manually trigger corruption effect
        /// </summary>
        /// <param name="intensity">Corruption intensity (0-1)</param>
        /// <param name="duration">Duration of the effect</param>
        public void TriggerCorruption(float intensity, float duration = 2f)
        {
            StartCoroutine(CorruptionSequence(intensity, duration));
        }
        
        /// <summary>
        /// Corruption sequence coroutine
        /// </summary>
        private System.Collections.IEnumerator CorruptionSequence(float targetIntensity, float duration)
        {
            float startIntensity = corruptionInfluence;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, progress);
                
                SetCorruptionIntensity(currentIntensity);
                
                yield return null;
            }
            
            SetCorruptionIntensity(targetIntensity);
        }
        
        /// <summary>
        /// Reset corruption to default state
        /// </summary>
        public void ResetCorruption()
        {
            corruptionInfluence = 0f;
            corruptionNormalStrength = 1f;
            deformStrength = 0f;
            currentInsanityLevel = 0f;
            
            UpdateCorruptionEffects();
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // Clean up material instance to prevent memory leaks
            if (materialInstance != null)
            {
                DestroyImmediate(materialInstance);
            }
        }
        
        #endregion
        
        #region ICorruptionController Implementation
        
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
            return isInLODRange && cachedRenderer != null && cachedRenderer.enabled;
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
        
        #region Editor Support
        
        private void OnValidate()
        {
            // Update effects in real-time during editor changes
            if (Application.isPlaying && materialInstance != null)
            {
                SetCorruptionTextures();
                UpdateCorruptionEffects();
            }
        }
        
        #endregion
    }
}