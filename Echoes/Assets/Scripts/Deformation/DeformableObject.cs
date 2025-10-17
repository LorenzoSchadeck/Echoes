using UnityEngine;

namespace Echoes.Deformation
{
    /// <summary>
    /// Componente que marca um objeto como deformável e define suas propriedades.
    /// Deve ser anexado a GameObjects que possuem Renderer com material compatível.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class DeformableObject : MonoBehaviour
    {
        [Header("🎯 Deformation Configuration")]
        [SerializeField] private DeformableObjectConfig configuration = new DeformableObjectConfig();
        
        [Header(" Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Componentes cacheados
        private Renderer cachedRenderer;
        private Transform cachedTransform;
        
        // Estado
        private bool isRegistered = false;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Cache componentes
            cachedRenderer = GetComponent<Renderer>();
            cachedTransform = transform;
        }
        
        private void Start()
        {
            InitializeObject();
        }
        
        private void OnEnable()
        {
            RegisterWithManager();
        }
        
        private void OnDisable()
        {
            UnregisterWithManager();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (showDebugInfo)
            {
                // Indica tipo de deformação
                Gizmos.color = configuration.allowMeshDeformation ? Color.red : Color.gray;
                Gizmos.DrawWireCube(transform.position + Vector3.up, Vector3.one * 0.5f);
                
                Gizmos.color = configuration.allowTextureDeformation ? Color.blue : Color.gray;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeObject()
        {
            // Valida configuração
            ValidateConfiguration();
            
            // Registra com o manager
            RegisterWithManager();
        }
        
        private void ValidateConfiguration()
        {
            if (cachedRenderer == null)
            {
                Debug.LogError($"[DeformableObject] {name}: Renderer component not found!");
                enabled = false;
                return;
            }
            
            if (cachedRenderer.sharedMaterial == null)
            {
                Debug.LogError($"[DeformableObject] {name}: Material not found on renderer!");
                enabled = false;
                return;
            }
        }
        
        #endregion
        
        #region Manager Integration
        
        private void RegisterWithManager()
        {
            if (DeformationManager.Instance != null && !isRegistered)
            {
                DeformationManager.Instance.RegisterObject(this);
                isRegistered = true;
            }
        }
        
        private void UnregisterWithManager()
        {
            if (DeformationManager.Instance != null && isRegistered)
            {
                DeformationManager.Instance.UnregisterObject(this);
                isRegistered = false;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Obtém a configuração de deformação deste objeto
        /// </summary>
        public DeformableObjectConfig GetConfiguration()
        {
            return configuration;
        }
        
        /// <summary>
        /// Obtém o Renderer deste objeto
        /// </summary>
        public Renderer GetRenderer()
        {
            return cachedRenderer;
        }
        

        
        /// <summary>
        /// Define nova configuração de deformação
        /// </summary>
        public void SetConfiguration(DeformableObjectConfig newConfig)
        {
            configuration = newConfig;
            ValidateConfiguration();
        }
        
        /// <summary>
        /// Ativa/desativa deformação de mesh
        /// </summary>
        public void SetMeshDeformation(bool enabled)
        {
            configuration.allowMeshDeformation = enabled;
        }
        
        /// <summary>
        /// Ativa/desativa deformação de textura
        /// </summary>
        public void SetTextureDeformation(bool enabled)
        {
            configuration.allowTextureDeformation = enabled;
        }
        
        #endregion
        
        #region Editor Support
        
        [ContextMenu("Force Update Deformation")]
        private void ForceUpdateDeformation()
        {
            if (DeformationManager.Instance != null)
            {
                DeformationManager.Instance.ForceUpdateAll();
            }
        }
        
        [ContextMenu("Reset Configuration")]
        private void ResetConfiguration()
        {
            configuration = new DeformableObjectConfig();
        }
        
        [ContextMenu("Auto Configure Based On Material")]
        private void AutoConfigureBasedOnMaterial()
        {
            if (cachedRenderer == null || cachedRenderer.sharedMaterial == null) return;
            
            var material = cachedRenderer.sharedMaterial;
            configuration.allowMeshDeformation = material.HasProperty("_DeformStrength");
            configuration.allowTextureDeformation = material.HasProperty("_InsanityLevel");
            
            Debug.Log($"[DeformableObject] Auto-configured: Mesh={configuration.allowMeshDeformation}, Texture={configuration.allowTextureDeformation}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Configuração de como um objeto deve ser deformado
    /// </summary>
    [System.Serializable]
    public class DeformableObjectConfig
    {
        [Header("🎭 Deformation Types")]
        [Tooltip("Permite deformação da geometria (mesh vertices)")]
        public bool allowMeshDeformation = true;
        
        [Tooltip("Permite deformação das texturas (UV distortion)")]
        public bool allowTextureDeformation = true;
        
        [Header("🔧 Intensity Multipliers")]
        [Tooltip("Multiplicador para intensidade de deformação de mesh")]
        [Range(0f, 3f)] public float meshIntensityMultiplier = 1f;
        
        [Tooltip("Multiplicador para intensidade de deformação de textura")]
        [Range(0f, 3f)] public float textureIntensityMultiplier = 1f;
        
        [Header("⚙️ Advanced Settings")]
        [Tooltip("Usar interpolação suave nas transições")]
        public bool useSmoothTransitions = true;
    }
}