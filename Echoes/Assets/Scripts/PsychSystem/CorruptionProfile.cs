using UnityEngine;
using System;

namespace Echoes.PsychSystem
{
    /// <summary>
    /// Perfil de corrupção que define como um objeto específico reage à sanidade.
    /// Permite configuração granular por objeto.
    /// </summary>
    [Serializable]
    public class CorruptionProfile
    {
        [Header("🎭 Object Corruption Settings")]
        [Tooltip("Permite deformação de textura/materiais")]
        public bool allowTextureCorruption = true;
        
        [Tooltip("Permite deformação de mesh/geometria")]
        public bool allowMeshDeformation = true;
        
        [Header("🎨 Texture Corruption")]
        [Tooltip("Multiplicador da intensidade de corrupção de textura")]
        [Range(0f, 2f)]
        public float textureCorruptionMultiplier = 1f;
        
        [Tooltip("Curva que modifica como a corrupção de textura progride")]
        public AnimationCurve textureCorruptionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("🔧 Mesh Deformation")]
        [Tooltip("Multiplicador da força de deformação")]
        [Range(0f, 2f)]
        public float meshDeformationMultiplier = 1f;
        
        [Tooltip("Curva que modifica como a deformação progride")]
        public AnimationCurve meshDeformationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Tooltip("Frequência específica de deformação para este objeto")]
        [Range(0.1f, 10f)]
        public float customDeformationFrequency = 0f; // 0 = usa valor do threshold
        
        [Header("⚡ Responsiveness")]
        [Tooltip("Velocidade de resposta às mudanças de sanidade")]
        [Range(0.1f, 5f)]
        public float responseSpeed = 1f;
        
        [Tooltip("Suavização das transições")]
        [Range(0f, 1f)]
        public float smoothing = 0.5f;
        
        [Header("🎯 Advanced Settings")]
        [Tooltip("Limiar mínimo de sanidade para começar a corrupção")]
        [Range(0f, 1f)]
        public float corruptionStartThreshold = 0.8f;
        
        [Tooltip("Multiplicador geral de todos os efeitos")]
        [Range(0f, 2f)]
        public float globalEffectMultiplier = 1f;
        
        /// <summary>
        /// Calcula a intensidade final de corrupção de textura baseada na sanidade
        /// </summary>
        public float CalculateTextureCorruption(float sanityValue, float baseIntensity)
        {
            if (!allowTextureCorruption || sanityValue > corruptionStartThreshold) 
                return 0f;
            
            float normalizedCorruption = (corruptionStartThreshold - sanityValue) / corruptionStartThreshold;
            float curvedCorruption = textureCorruptionCurve.Evaluate(normalizedCorruption);
            
            return curvedCorruption * baseIntensity * textureCorruptionMultiplier * globalEffectMultiplier;
        }
        
        /// <summary>
        /// Calcula a intensidade final de deformação de mesh baseada na sanidade
        /// </summary>
        public float CalculateMeshDeformation(float sanityValue, float baseStrength)
        {
            if (!allowMeshDeformation || sanityValue > corruptionStartThreshold) 
                return 0f;
            
            float normalizedCorruption = (corruptionStartThreshold - sanityValue) / corruptionStartThreshold;
            float curvedDeformation = meshDeformationCurve.Evaluate(normalizedCorruption);
            
            return curvedDeformation * baseStrength * meshDeformationMultiplier * globalEffectMultiplier;
        }
        
        /// <summary>
        /// Obtém a frequência de deformação a ser usada
        /// </summary>
        public float GetDeformationFrequency(float defaultFrequency)
        {
            return customDeformationFrequency > 0f ? customDeformationFrequency : defaultFrequency;
        }
        
        /// <summary>
        /// Verifica se o objeto deve ser afetado pela corrupção
        /// </summary>
        public bool ShouldApplyCorruption(float sanityValue)
        {
            return sanityValue <= corruptionStartThreshold && 
                   (allowTextureCorruption || allowMeshDeformation);
        }
    }
}