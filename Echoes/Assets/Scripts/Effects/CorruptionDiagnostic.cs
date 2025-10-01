using UnityEngine;

namespace Echoes.Effects
{
    /// <summary>
    /// Script de diagnóstico para verificar se o sistema de corrupção está funcionando corretamente.
    /// Use temporariamente em objetos que estão apresentando problemas de deformação.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class CorruptionDiagnostic : MonoBehaviour
    {
        [Header("🔍 Diagnostic Settings")]
        [SerializeField] private bool enableRealTimeCheck = true;
        [SerializeField] private float checkInterval = 1f;
        
        private Renderer cachedRenderer;
        private Material material;
        private float lastCheckTime;
        
        // Shader property IDs
        private static readonly int DeformStrengthId = Shader.PropertyToID("_DeformStrength");
        private static readonly int DeformFrequencyId = Shader.PropertyToID("_DeformFrequency");
        private static readonly int CorruptionInfluenceId = Shader.PropertyToID("_CorruptionInfluence");
        private static readonly int InsanityLevelId = Shader.PropertyToID("_InsanityLevel");
        
        private void Start()
        {
            cachedRenderer = GetComponent<Renderer>();
            material = cachedRenderer.material;
            
            Debug.Log($"[CorruptionDiagnostic] Started diagnostic for {gameObject.name}");
            PerformDiagnostic();
        }
        
        private void Update()
        {
            if (enableRealTimeCheck && Time.time - lastCheckTime >= checkInterval)
            {
                PerformDiagnostic();
                lastCheckTime = Time.time;
            }
        }
        
        private void PerformDiagnostic()
        {
            if (material == null) return;
            
            // Check shader properties
            float deformStrength = material.GetFloat(DeformStrengthId);
            float deformFrequency = material.GetFloat(DeformFrequencyId);
            float corruptionInfluence = material.GetFloat(CorruptionInfluenceId);
            float insanityLevel = material.GetFloat(InsanityLevelId);
            
            // Check for corruption controllers
            var simpleController = GetComponent<SimpleCorruptionController>();
            var enhancedController = GetComponent<EnhancedCorruptionController>();
            
            string simpleControllerInfo = simpleController != null ? 
                $"Found (MeshDeform: {simpleController.IsMeshDeformationEnabled()})" : "Not Found";
            string enhancedControllerInfo = enhancedController != null ? 
                $"Found (MeshDeform: {enhancedController.IsMeshDeformationEnabled()})" : "Not Found";
            
            Debug.Log($"[DIAGNOSTIC] {gameObject.name}:\n" +
                     $"  Shader Values:\n" +
                     $"    - DeformStrength: {deformStrength}\n" +
                     $"    - DeformFrequency: {deformFrequency}\n" +
                     $"    - CorruptionInfluence: {corruptionInfluence}\n" +
                     $"    - InsanityLevel: {insanityLevel}\n" +
                     $"  Controllers:\n" +
                     $"    - SimpleController: {simpleControllerInfo}\n" +
                     $"    - EnhancedController: {enhancedControllerInfo}");
            
            // Check for issues
            if (deformStrength > 0f)
            {
                if (simpleController != null && !simpleController.IsMeshDeformationEnabled())
                {
                    Debug.LogWarning($"[DIAGNOSTIC] {gameObject.name}: PROBLEM DETECTED! DeformStrength is {deformStrength} but SimpleController has MeshDeformation DISABLED!");
                }
                if (enhancedController != null && !enhancedController.IsMeshDeformationEnabled())
                {
                    Debug.LogWarning($"[DIAGNOSTIC] {gameObject.name}: PROBLEM DETECTED! DeformStrength is {deformStrength} but EnhancedController has MeshDeformation DISABLED!");
                }
            }
        }
        
        [ContextMenu("Force Diagnostic Check")]
        public void ForceDiagnosticCheck()
        {
            PerformDiagnostic();
        }
        
        [ContextMenu("Force Zero Deformation")]
        public void ForceZeroDeformation()
        {
            if (material != null)
            {
                material.SetFloat(DeformStrengthId, 0f);
                Debug.Log($"[DIAGNOSTIC] {gameObject.name}: FORCED DeformStrength to 0");
            }
        }
        
        private void OnDestroy()
        {
            Debug.Log($"[CorruptionDiagnostic] Diagnostic ended for {gameObject.name}");
        }
    }
}