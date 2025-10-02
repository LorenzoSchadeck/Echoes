using UnityEngine;
using Echoes.Deformation;

namespace Echoes.Examples
{
    /// <summary>
    /// Script de demonstração do sistema de deformação baseado em sanidade.
    /// Mostra como o sistema funciona em tempo real e permite testes manuais.
    /// </summary>
    public class DeformationExample : MonoBehaviour
    {
        [Header("🎮 Demo Controls")]
        [SerializeField] private bool enableDemo = true;
        [SerializeField] private KeyCode decreaseSanityKey = KeyCode.Q;
        [SerializeField] private KeyCode increaseSanityKey = KeyCode.E;
        [SerializeField] private KeyCode resetSanityKey = KeyCode.R;
        
        [Header("📊 Visual Feedback")]
        [SerializeField] private bool showDebugGUI = true;
        [SerializeField] private bool showDebugLogs = false;
        
        [Header("🎯 Test Objects")]
        [Tooltip("Objetos que receberão componente DeformableObject automaticamente")]
        [SerializeField] private GameObject[] testObjects = new GameObject[0];
        
        [Header("⚙️ Test Configuration")]
        [SerializeField] private DeformableObjectConfig meshOnlyConfig = new DeformableObjectConfig 
        { 
            allowMeshDeformation = true, 
            allowTextureDeformation = false 
        };
        
        [SerializeField] private DeformableObjectConfig textureOnlyConfig = new DeformableObjectConfig 
        { 
            allowMeshDeformation = false, 
            allowTextureDeformation = true 
        };
        
        [SerializeField] private DeformableObjectConfig bothConfig = new DeformableObjectConfig 
        { 
            allowMeshDeformation = true, 
            allowTextureDeformation = true 
        };
        
        // Estado interno
        private float testSanity = 1f;
        private DeformationManager deformationManager;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeDemo();
        }
        
        private void Update()
        {
            if (!enableDemo) return;
            
            HandleInput();
        }
        
        private void OnGUI()
        {
            if (!showDebugGUI) return;
            
            DrawDemoGUI();
        }
        
        #endregion
        
        #region Demo Implementation
        
        private void InitializeDemo()
        {
            // Encontra ou cria o DeformationManager
            deformationManager = DeformationManager.Instance;
            if (deformationManager == null)
            {
                var managerObject = new GameObject("DeformationManager");
                deformationManager = managerObject.AddComponent<DeformationManager>();
            }
            
            // Configura objetos de teste
            SetupTestObjects();
            
            Debug.Log("[DeformationExample] Demo initialized");
        }
        
        private void SetupTestObjects()
        {
            for (int i = 0; i < testObjects.Length; i++)
            {
                var obj = testObjects[i];
                if (obj == null) continue;
                
                // Adiciona componente DeformableObject se não existir
                var deformableObject = obj.GetComponent<DeformableObject>();
                if (deformableObject == null)
                {
                    deformableObject = obj.AddComponent<DeformableObject>();
                }
                
                // Configura diferentes tipos baseado no índice
                switch (i % 3)
                {
                    case 0: // Mesh Only
                        deformableObject.SetConfiguration(meshOnlyConfig);
                        obj.name = $"{obj.name} (Mesh Only)";
                        break;
                        
                    case 1: // Texture Only
                        deformableObject.SetConfiguration(textureOnlyConfig);
                        obj.name = $"{obj.name} (Texture Only)";
                        break;
                        
                    case 2: // Both
                        deformableObject.SetConfiguration(bothConfig);
                        obj.name = $"{obj.name} (Both)";
                        break;
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"[DeformationExample] Configured {obj.name}");
                }
            }
        }
        
        private void HandleInput()
        {
            // Controle manual de sanidade para testes
            if (Input.GetKey(decreaseSanityKey))
            {
                testSanity = Mathf.Max(0f, testSanity - Time.deltaTime * 0.3f);
                SimulateSanityChange(testSanity);
            }
            
            if (Input.GetKey(increaseSanityKey))
            {
                testSanity = Mathf.Min(1f, testSanity + Time.deltaTime * 0.3f);
                SimulateSanityChange(testSanity);
            }
            
            if (Input.GetKeyDown(resetSanityKey))
            {
                testSanity = 1f;
                SimulateSanityChange(testSanity);
            }
        }
        
        private void SimulateSanityChange(float newSanity)
        {
            // ⚠️ NOTA IMPORTANTE: Este é um script de DEMONSTRAÇÃO apenas!
            // Em produção, a sanidade deve ser controlada exclusivamente pelo InsanityManager
            
            if (showDebugLogs)
            {
                Debug.Log($"[DeformationExample] Demo sanity change: {newSanity:F2} (This is for testing only!)");
                Debug.LogWarning("[DeformationExample] This is a demo script. In production, use InsanityManager.SetSanity() method.");
            }
            
            // Para demonstração, apenas atualiza a variável interna
            // O sistema real deve integrar com o InsanityManager através de seus métodos públicos
            testSanity = newSanity;
        }
        
        #endregion
        
        #region GUI
        
        private void DrawDemoGUI()
        {
            GUILayout.BeginArea(new Rect(10, 200, 400, 300));
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label("🎭 Deformation System Demo", GUI.skin.label);
            GUILayout.Space(10);
            
            // Status do sistema
            if (deformationManager != null)
            {
                GUILayout.Label($"📊 {deformationManager.GetSystemStats()}");
                GUILayout.Label($"🎯 Deformation Level: {deformationManager.GetCurrentDeformationLevel():P0}");
            }
            else
            {
                GUILayout.Label("❌ DeformationManager not found!");
            }
            
            GUILayout.Space(10);
            
            // Controles
            GUILayout.Label("🎮 Manual Controls:");
            GUILayout.Label($"{decreaseSanityKey} - Decrease Sanity");
            GUILayout.Label($"{increaseSanityKey} - Increase Sanity");
            GUILayout.Label($"{resetSanityKey} - Reset to 100%");
            
            GUILayout.Space(10);
            
            // Slider de sanidade
            GUILayout.Label($"🧠 Test Sanity: {testSanity:P0}");
            float newSanity = GUILayout.HorizontalSlider(testSanity, 0f, 1f);
            
            if (Mathf.Abs(newSanity - testSanity) > 0.01f)
            {
                testSanity = newSanity;
                SimulateSanityChange(testSanity);
            }
            
            if (GUILayout.Button("Force Update Demo"))
            {
                DemoForceUpdate();
            }
            
            GUILayout.Space(10);
            
            // Informações importantes
            if (testSanity > 0.3f)
            {
                GUILayout.Label("ℹ️ Sanity > 30% - No deformation", GUI.skin.box);
            }
            else
            {
                GUILayout.Label("⚠️ Sanity ≤ 30% - Deformation ACTIVE", GUI.skin.box);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
        
        #region Demo API
        
        /// <summary>
        /// Método público seguro para definir sanidade durante demonstrações
        /// </summary>
        public void SetSanityForDemo(float sanityValue)
        {
            testSanity = Mathf.Clamp01(sanityValue);
            SimulateSanityChange(testSanity);
        }
        
        /// <summary>
        /// Força atualização da demonstração (apenas para propósitos de demo)
        /// </summary>
        private void DemoForceUpdate()
        {
            if (deformationManager != null)
            {
                deformationManager.ForceUpdateAll();
            }
            
            if (showDebugLogs)
            {
                Debug.Log("[DeformationExample] Demo force update executed");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Configura todos os objetos para permitir apenas deformação de mesh
        /// </summary>
        [ContextMenu("Set All Objects - Mesh Only")]
        public void SetAllObjectsMeshOnly()
        {
            SetAllObjectsConfiguration(meshOnlyConfig);
        }
        
        /// <summary>
        /// Configura todos os objetos para permitir apenas deformação de textura
        /// </summary>
        [ContextMenu("Set All Objects - Texture Only")]
        public void SetAllObjectsTextureOnly()
        {
            SetAllObjectsConfiguration(textureOnlyConfig);
        }
        
        /// <summary>
        /// Configura todos os objetos para permitir ambas as deformações
        /// </summary>
        [ContextMenu("Set All Objects - Both")]
        public void SetAllObjectsBoth()
        {
            SetAllObjectsConfiguration(bothConfig);
        }
        
        private void SetAllObjectsConfiguration(DeformableObjectConfig config)
        {
            foreach (var obj in testObjects)
            {
                if (obj != null)
                {
                    var deformableObject = obj.GetComponent<DeformableObject>();
                    if (deformableObject != null)
                    {
                        deformableObject.SetConfiguration(config);
                    }
                }
            }
            
            Debug.Log($"[DeformationExample] Applied configuration to all test objects");
        }
        
        /// <summary>
        /// Força atualização de todos os objetos deformáveis
        /// </summary>
        [ContextMenu("Force Update All Deformations")]
        public void ForceUpdateAllDeformations()
        {
            if (deformationManager != null)
            {
                deformationManager.ForceUpdateAll();
                Debug.Log("[DeformationExample] Forced update of all deformations");
            }
        }
        
        #endregion
    }
}

#if UNITY_EDITOR
namespace Echoes.Examples.Editor
{
    using UnityEditor;
    
    [CustomEditor(typeof(DeformationExample))]
    public class DeformationExampleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(20);
            GUILayout.Label("🎭 Quick Demo Actions", EditorStyles.boldLabel);
            
            var example = (DeformationExample)target;
            
            if (Application.isPlaying)
            {
                GUILayout.BeginHorizontal();
                
                if (GUILayout.Button("😇 Sane (100%)"))
                {
                    example.SetSanityForDemo(1f);
                }
                
                if (GUILayout.Button("😐 Threshold (30%)"))
                {
                    example.SetSanityForDemo(0.3f);
                }
                
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                
                if (GUILayout.Button("😰 Low (15%)"))
                {
                    example.SetSanityForDemo(0.15f);
                }
                
                if (GUILayout.Button("🔥 Insane (0%)"))
                {
                    example.SetSanityForDemo(0f);
                }
                
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button("🔄 Force Update All"))
                {
                    example.ForceUpdateAllDeformations();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test deformation effects", MessageType.Info);
            }
            
            GUILayout.Space(10);
            GUILayout.Label("📝 Configuration Presets:", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mesh Only")) example.SetAllObjectsMeshOnly();
            if (GUILayout.Button("Texture Only")) example.SetAllObjectsTextureOnly();
            if (GUILayout.Button("Both")) example.SetAllObjectsBoth();
            GUILayout.EndHorizontal();
        }
    }
}
#endif