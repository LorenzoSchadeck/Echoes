using UnityEngine;
using System.Collections;

namespace Echoes.PsychSystem
{
    /// <summary>
    /// Controlador unificado de corrupção psicológica que substitui os antigos controladores.
    /// Integra perfis de corrupção configuráveis com sistema de sanidade.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PsychCorruptionController : MonoBehaviour
    {
        [Header("🎭 Corruption Profile")]
        [Tooltip("Perfil que define como este objeto reage à corrupção")]
        [SerializeField] private CorruptionProfile corruptionProfile = new CorruptionProfile();
        
        [Header("🎨 Visual Configuration")]
        [Tooltip("Material alternativo para preview de corrupção máxima (opcional)")]
        [SerializeField] private Material previewCorruptionMaterial;
        
        [Header("⚡ Performance")]
        [Tooltip("Usar LOD baseado em distância")]
        [SerializeField] private bool useLOD = true;
        [SerializeField] private float lodDistance = 50f;
        
        [Header("🐛 Debug")]
        [SerializeField] private bool enableDebugVisualization = false;
        [SerializeField] private bool logCorruptionChanges = false;
        
        #region Private Fields
        
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
        private float currentTextureCorruption = 0f;
        private float targetTextureCorruption = 0f;
        private float currentMeshDeformation = 0f;
        private float targetMeshDeformation = 0f;
        private float currentNormalStrength = 1f;
        private float targetNormalStrength = 1f;
        private float currentDeformFrequency = 1f;
        
        // Transition state
        private Coroutine transitionCoroutine;
        private bool isInLODRange = true;
        private bool isInitialized = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeCorruption();
            RegisterWithSystem();
        }
        
        private void Update()
        {
            if (useLOD)
            {
                UpdateLOD();
            }
        }
        
        private void OnDestroy()
        {
            UnregisterFromSystem();
            CleanupMaterial();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer == null)
            {
                Debug.LogError($"[PsychCorruptionController] No Renderer found on {gameObject.name}");
                enabled = false;
                return;
            }
            
            // Create material instance to avoid affecting shared materials
            if (cachedRenderer.sharedMaterial != null)
            {
                materialInstance = new Material(cachedRenderer.sharedMaterial);
                cachedRenderer.material = materialInstance;
            }
            else
            {
                Debug.LogError($"[PsychCorruptionController] No material found on {gameObject.name}");
                enabled = false;
                return;
            }
            
            playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        }
        
        private void InitializeCorruption()
        {
            if (materialInstance == null) return;
            
            // Set initial shader values
            ApplyShaderValues(0f, 0f, 1f, 1f);
            
            isInitialized = true;
            
            if (logCorruptionChanges)
            {
                Debug.Log($"[PsychCorruptionController] Initialized on {gameObject.name}");
            }
        }
        
        private void RegisterWithSystem()
        {
            var psychSystem = HorrorPsychSystem.Instance;
            if (psychSystem != null)
            {
                psychSystem.RegisterController(this);
            }
            else if (logCorruptionChanges)
            {
                Debug.LogWarning($"[PsychCorruptionController] HorrorPsychSystem not found for {gameObject.name}");
            }
        }
        
        private void UnregisterFromSystem()
        {
            var psychSystem = HorrorPsychSystem.Instance;
            if (psychSystem != null)
            {
                psychSystem.UnregisterController(this);
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
        
        #region Corruption Application
        
        /// <summary>
        /// Aplica corrupção baseada na sanidade e threshold atual
        /// </summary>
        public void ApplyCorruption(float sanityValue, SanityThreshold threshold)
        {
            if (!isInitialized || threshold == null || !corruptionProfile.ShouldApplyCorruption(sanityValue))
            {
                // Reset corruption if conditions are not met
                SetTargetValues(0f, 0f, 1f, threshold?.deformationFrequency ?? 1f);
                return;
            }
            
            // Calculate corruption values using profile
            float textureCorruption = corruptionProfile.CalculateTextureCorruption(sanityValue, threshold.textureCorruptionIntensity);
            float meshDeformation = corruptionProfile.CalculateMeshDeformation(sanityValue, threshold.meshDeformationStrength);
            float normalStrength = Mathf.Lerp(1f, threshold.normalStrength, 1f - sanityValue);
            float deformFrequency = corruptionProfile.GetDeformationFrequency(threshold.deformationFrequency);
            
            SetTargetValues(textureCorruption, meshDeformation, normalStrength, deformFrequency);
            
            if (logCorruptionChanges)
            {
                Debug.Log($"[{gameObject.name}] Corruption applied - Texture: {textureCorruption:F2}, Mesh: {meshDeformation:F2}");
            }
        }
        
        private void SetTargetValues(float textureCorruption, float meshDeformation, float normalStrength, float deformFrequency)
        {
            // Apply profile restrictions
            targetTextureCorruption = corruptionProfile.allowTextureCorruption ? textureCorruption : 0f;
            targetMeshDeformation = corruptionProfile.allowMeshDeformation ? meshDeformation : 0f;
            targetNormalStrength = normalStrength;
            currentDeformFrequency = deformFrequency;
            
            // Start smooth transition
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            
            transitionCoroutine = StartCoroutine(SmoothTransition());
        }
        
        private IEnumerator SmoothTransition()
        {
            float transitionSpeed = corruptionProfile.responseSpeed;
            float smoothing = corruptionProfile.smoothing;
            
            while (!Mathf.Approximately(currentTextureCorruption, targetTextureCorruption) ||
                   !Mathf.Approximately(currentMeshDeformation, targetMeshDeformation) ||
                   !Mathf.Approximately(currentNormalStrength, targetNormalStrength))
            {
                float deltaTime = Time.deltaTime * transitionSpeed;
                
                // Smooth interpolation
                currentTextureCorruption = Mathf.Lerp(currentTextureCorruption, targetTextureCorruption, deltaTime);
                currentMeshDeformation = Mathf.Lerp(currentMeshDeformation, targetMeshDeformation, deltaTime);
                currentNormalStrength = Mathf.Lerp(currentNormalStrength, targetNormalStrength, deltaTime);
                
                // Apply smoothing
                if (smoothing > 0f)
                {
                    currentTextureCorruption = Mathf.SmoothStep(currentTextureCorruption, targetTextureCorruption, smoothing);
                    currentMeshDeformation = Mathf.SmoothStep(currentMeshDeformation, targetMeshDeformation, smoothing);
                    currentNormalStrength = Mathf.SmoothStep(currentNormalStrength, targetNormalStrength, smoothing);
                }
                
                ApplyShaderValues(currentTextureCorruption, currentMeshDeformation, currentNormalStrength, currentDeformFrequency);
                
                yield return null;
            }
            
            // Ensure final values are exact
            ApplyShaderValues(targetTextureCorruption, targetMeshDeformation, targetNormalStrength, currentDeformFrequency);
            transitionCoroutine = null;
        }
        
        private void ApplyShaderValues(float textureCorruption, float meshDeformation, float normalStrength, float deformFrequency)
        {
            if (materialInstance == null) return;
            
            materialInstance.SetFloat(CorruptionInfluenceId, textureCorruption);
            materialInstance.SetFloat(DeformStrengthId, meshDeformation);
            materialInstance.SetFloat(CorruptionNormalStrengthId, normalStrength);
            materialInstance.SetFloat(DeformFrequencyId, deformFrequency);
            
            // Set current sanity for shader calculations
            var psychSystem = HorrorPsychSystem.Instance;
            if (psychSystem != null)
            {
                materialInstance.SetFloat(InsanityLevelId, 1f - psychSystem.GetCurrentSanity());
            }
        }
        
        #endregion
        
        #region LOD System
        
        private void UpdateLOD()
        {
            if (playerCamera == null) return;
            
            float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
            bool shouldBeInRange = distance <= lodDistance;
            
            if (isInLODRange != shouldBeInRange)
            {
                isInLODRange = shouldBeInRange;
                cachedRenderer.enabled = isInLODRange;
                
                if (logCorruptionChanges)
                {
                    Debug.Log($"[{gameObject.name}] LOD changed - In range: {isInLODRange}");
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Força aplicação imediata da corrupção
        /// </summary>
        public void ForceApplyCorruption()
        {
            var psychSystem = HorrorPsychSystem.Instance;
            if (psychSystem != null)
            {
                var threshold = psychSystem.GetCurrentThreshold();
                if (threshold != null)
                {
                    ApplyCorruption(psychSystem.GetCurrentSanity(), threshold);
                }
            }
        }
        
        /// <summary>
        /// Reseta toda corrupção
        /// </summary>
        public void ResetCorruption()
        {
            SetTargetValues(0f, 0f, 1f, 1f);
        }
        
        /// <summary>
        /// Preview de corrupção máxima (para teste)
        /// </summary>
        public void PreviewMaxCorruption()
        {
            SetTargetValues(1f, 5f, 3f, 5f);
        }
        
        /// <summary>
        /// Obtém o perfil de corrupção atual
        /// </summary>
        public CorruptionProfile GetCorruptionProfile()
        {
            return corruptionProfile;
        }
        
        /// <summary>
        /// Define um novo perfil de corrupção
        /// </summary>
        public void SetCorruptionProfile(CorruptionProfile newProfile)
        {
            corruptionProfile = newProfile;
            ForceApplyCorruption();
        }
        
        /// <summary>
        /// Verifica se o objeto está no range de LOD
        /// </summary>
        public bool IsInLODRange()
        {
            return isInLODRange;
        }
        
        /// <summary>
        /// Obtém valores atuais de corrupção para debug
        /// </summary>
        public string GetCorruptionDebugInfo()
        {
            return $"Texture: {currentTextureCorruption:F2} | " +
                   $"Mesh: {currentMeshDeformation:F2} | " +
                   $"Normal: {currentNormalStrength:F2} | " +
                   $"LOD: {isInLODRange}";
        }
        
        #endregion
        
        #region Debug Visualization
        
        private void OnDrawGizmosSelected()
        {
            if (!enableDebugVisualization) return;
            
            // Draw LOD range
            if (useLOD)
            {
                Gizmos.color = isInLODRange ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position, lodDistance);
            }
            
            // Draw corruption level as colored sphere
            float corruptionLevel = (currentTextureCorruption + currentMeshDeformation) * 0.5f;
            Gizmos.color = Color.Lerp(Color.blue, Color.red, corruptionLevel);
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.3f);
        }
        
        #endregion
        
        #region Editor Support
        
        [ContextMenu("Force Apply Corruption")]
        private void EditorForceApplyCorruption()
        {
            ForceApplyCorruption();
        }
        
        [ContextMenu("Reset Corruption")]
        private void EditorResetCorruption()
        {
            ResetCorruption();
        }
        
        [ContextMenu("Preview Max Corruption")]
        private void EditorPreviewMaxCorruption()
        {
            PreviewMaxCorruption();
        }
        
        [ContextMenu("Log Debug Info")]
        private void EditorLogDebugInfo()
        {
            Debug.Log($"[{gameObject.name}] {GetCorruptionDebugInfo()}");
        }
        
        #endregion
    }
}