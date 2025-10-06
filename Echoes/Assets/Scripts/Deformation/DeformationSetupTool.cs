using UnityEngine;
using System.Collections.Generic;
using Echoes.Deformation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Echoes.Deformation.Utils
{
    /// <summary>
    /// Utilitário para configurar objetos deformáveis em massa.
    /// Facilita o setup de cenas com muitos objetos.
    /// </summary>
    public class DeformationSetupTool : MonoBehaviour
    {
        [Header("🎯 Mass Configuration")]
        [Tooltip("Configuração a ser aplicada a todos os objetos selecionados")]
        [SerializeField] private DeformableObjectConfig templateConfig = new DeformableObjectConfig();
        
        [Header("🔍 Auto Detection")]
        [Tooltip("Detectar automaticamente objetos com shader SG_DeformableObject")]
        [SerializeField] private bool autoDetectDeformableObjects = true;
        
        [Header("📋 Manual Selection")]
        [Tooltip("Lista manual de objetos para configurar")]
        [SerializeField] private List<GameObject> manualObjectList = new List<GameObject>();
        
        [Header("🔧 Advanced Options")]
        [Tooltip("Substituir configuração existente")]
        [SerializeField] private bool overrideExistingConfig = true;
        
        [Tooltip("Aplicar apenas a objetos sem DeformableObject")]
        [SerializeField] private bool onlyAddToNew = false;
        
        [Header("📊 Statistics")]
        [SerializeField, ReadOnly] public int lastFoundCount = 0;
        [SerializeField, ReadOnly] public int lastConfiguredCount = 0;
        
        // Cache para evitar recalcular
        private List<GameObject> cachedDeformableObjects = new List<GameObject>();
        
        #region Public API
        
        /// <summary>
        /// Encontra todos os objetos com materiais usando o shader deformável
        /// </summary>
        [ContextMenu("Find All Deformable Objects")]
        public void FindAllDeformableObjects()
        {
            cachedDeformableObjects.Clear();
            
            if (autoDetectDeformableObjects)
            {
                FindObjectsWithDeformableShader();
            }
            
            // Adiciona objetos da lista manual
            foreach (var obj in manualObjectList)
            {
                if (obj != null && !cachedDeformableObjects.Contains(obj))
                {
                    cachedDeformableObjects.Add(obj);
                }
            }
            
            lastFoundCount = cachedDeformableObjects.Count;
            
            Debug.Log($"[DeformationSetupTool] Found {lastFoundCount} deformable objects");
        }
        
        /// <summary>
        /// Aplica configuração a todos os objetos encontrados
        /// </summary>
        [ContextMenu("Apply Configuration To All")]
        public void ApplyConfigurationToAll()
        {
            if (cachedDeformableObjects.Count == 0)
            {
                FindAllDeformableObjects();
            }
            
            int configuredCount = 0;
            
            foreach (var obj in cachedDeformableObjects)
            {
                if (ConfigureObject(obj))
                {
                    configuredCount++;
                }
            }
            
            lastConfiguredCount = configuredCount;
            
            Debug.Log($"[DeformationSetupTool] Configured {configuredCount}/{cachedDeformableObjects.Count} objects");
        }
        
        /// <summary>
        /// Setup completo: encontra e configura automaticamente
        /// </summary>
        [ContextMenu("Complete Auto Setup")]
        public void CompleteAutoSetup()
        {
            FindAllDeformableObjects();
            ApplyConfigurationToAll();
            
            Debug.Log($"[DeformationSetupTool] Complete setup: {lastConfiguredCount} objects configured");
        }
        
        /// <summary>
        /// Remove todos os componentes DeformableObject da cena
        /// </summary>
        [ContextMenu("Remove All Deformable Components")]
        public void RemoveAllDeformableComponents()
        {
            var allDeformableObjects = FindObjectsByType<DeformableObject>(FindObjectsSortMode.None);
            int removedCount = 0;
            
            foreach (var deformableObj in allDeformableObjects)
            {
                if (deformableObj != null)
                {
                    DestroyImmediate(deformableObj);
                    removedCount++;
                }
            }
            
            Debug.Log($"[DeformationSetupTool] Removed {removedCount} DeformableObject components");
        }
        
        #endregion
        
        #region Private Methods
        
        private void FindObjectsWithDeformableShader()
        {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in allRenderers)
            {
                if (HasDeformableShader(renderer))
                {
                    cachedDeformableObjects.Add(renderer.gameObject);
                }
            }
        }
        
        private bool HasDeformableShader(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null) return false;
            
            var material = renderer.sharedMaterial;
            var shaderName = material.shader.name;
            
            // Verifica se usa o shader deformável ou tem propriedades específicas
            return shaderName.Contains("SG_DeformableObject") ||
                   shaderName.Contains("DeformableObject") ||
                   (material.HasProperty("_DeformStrength") && material.HasProperty("_InsanityLevel"));
        }
        
        private bool ConfigureObject(GameObject obj)
        {
            if (obj == null) return false;
            
            var deformableObject = obj.GetComponent<DeformableObject>();
            bool hasExistingComponent = deformableObject != null;
            
            // Se só queremos adicionar a objetos novos e este já tem o componente
            if (onlyAddToNew && hasExistingComponent) return false;
            
            // Adiciona componente se não existir
            if (!hasExistingComponent)
            {
                deformableObject = obj.AddComponent<DeformableObject>();
            }
            
            // Aplica configuração se permitido
            if (overrideExistingConfig || !hasExistingComponent)
            {
                deformableObject.SetConfiguration(templateConfig);
            }
            
            return true;
        }
        
        #endregion
        
        #region Preset Configurations
        
        /// <summary>
        /// Configuração para objetos que só deformam mesh
        /// </summary>
        [ContextMenu("Set Template - Mesh Only")]
        public void SetTemplateMeshOnly()
        {
            templateConfig = new DeformableObjectConfig
            {
                allowMeshDeformation = true,
                allowTextureDeformation = false,
                meshIntensityMultiplier = 1f,
                textureIntensityMultiplier = 0f,
                useSmoothTransitions = true
            };
            
            Debug.Log("[DeformationSetupTool] Template set to Mesh Only");
        }
        
        /// <summary>
        /// Configuração para objetos que só deformam textura
        /// </summary>
        [ContextMenu("Set Template - Texture Only")]
        public void SetTemplateTextureOnly()
        {
            templateConfig = new DeformableObjectConfig
            {
                allowMeshDeformation = false,
                allowTextureDeformation = true,
                meshIntensityMultiplier = 0f,
                textureIntensityMultiplier = 1f,
                useSmoothTransitions = true
            };
            
            Debug.Log("[DeformationSetupTool] Template set to Texture Only");
        }
        
        /// <summary>
        /// Configuração para objetos que deformam ambos
        /// </summary>
        [ContextMenu("Set Template - Both")]
        public void SetTemplateBoth()
        {
            templateConfig = new DeformableObjectConfig
            {
                allowMeshDeformation = true,
                allowTextureDeformation = true,
                meshIntensityMultiplier = 1f,
                textureIntensityMultiplier = 1f,
                useSmoothTransitions = true
            };
            
            Debug.Log("[DeformationSetupTool] Template set to Both");
        }
        
        /// <summary>
        /// Configuração de alta performance (reduzida intensidade)
        /// </summary>
        [ContextMenu("Set Template - Performance Mode")]
        public void SetTemplatePerformanceMode()
        {
            templateConfig = new DeformableObjectConfig
            {
                allowMeshDeformation = true,
                allowTextureDeformation = true,
                meshIntensityMultiplier = 0.5f,
                textureIntensityMultiplier = 0.7f,
                useSmoothTransitions = false
            };
            
            Debug.Log("[DeformationSetupTool] Template set to Performance Mode");
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            // Limita tamanhos das listas para evitar problemas de performance
            if (manualObjectList.Count > 1000)
            {
                Debug.LogWarning("[DeformationSetupTool] Manual object list is very large. Consider using auto-detection instead.");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Atributo para campos readonly no Inspector
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}

#if UNITY_EDITOR
namespace Echoes.Deformation.Utils.Editor
{
    /// <summary>
    /// Property drawer para campos readonly
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool previousGUIState = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = previousGUIState;
        }
    }
    
    /// <summary>
    /// Editor customizado para DeformationSetupTool
    /// </summary>
    [CustomEditor(typeof(DeformationSetupTool))]
    public class DeformationSetupToolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(20);
            GUILayout.Label("🛠️ Setup Tools", EditorStyles.boldLabel);
            
            var tool = (DeformationSetupTool)target;
            
            // Botões principais
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 Find Objects"))
            {
                tool.FindAllDeformableObjects();
            }
            if (GUILayout.Button("⚙️ Apply Config"))
            {
                tool.ApplyConfigurationToAll();
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("🚀 Complete Auto Setup", GUILayout.Height(30)))
            {
                tool.CompleteAutoSetup();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("📋 Template Presets", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔧 Mesh Only")) tool.SetTemplateMeshOnly();
            if (GUILayout.Button("🎨 Texture Only")) tool.SetTemplateTextureOnly();
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🎭 Both")) tool.SetTemplateBoth();
            if (GUILayout.Button("⚡ Performance")) tool.SetTemplatePerformanceMode();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.Label("🗑️ Cleanup", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove All Deformable Components"))
            {
                if (EditorUtility.DisplayDialog("Confirm Removal", 
                    "This will remove ALL DeformableObject components from the scene. Continue?", 
                    "Yes", "Cancel"))
                {
                    tool.RemoveAllDeformableComponents();
                }
            }
            GUI.backgroundColor = Color.white;
            
            // Informações de status
            GUILayout.Space(10);
            EditorGUILayout.HelpBox($"Last Operation: Found {tool.lastFoundCount} objects, configured {tool.lastConfiguredCount}", MessageType.Info);
        }
    }
}
#endif