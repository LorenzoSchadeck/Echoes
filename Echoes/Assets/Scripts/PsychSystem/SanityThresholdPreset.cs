using UnityEngine;
using System.Collections.Generic;

namespace Echoes.PsychSystem
{
    /// <summary>
    /// Configuração pré-definida de thresholds de sanidade para diferentes tipos de jogos de terror.
    /// </summary>
    [CreateAssetMenu(fileName = "SanityThresholdPreset", menuName = "Echoes/Psych System/Sanity Threshold Preset")]
    public class SanityThresholdPreset : ScriptableObject
    {
        [Header("🎮 Preset Information")]
        [Tooltip("Nome descritivo deste preset")]
        public string presetName = "Default Horror";
        
        [Tooltip("Descrição do tipo de experiência de terror")]
        [TextArea(3, 5)]
        public string description = "Configuração balanceada para terror psicológico progressivo";
        
        [Header("🧠 Sanity Thresholds")]
        [Tooltip("Lista de thresholds configurados para este preset")]
        public List<SanityThreshold> thresholds = new List<SanityThreshold>();
        
        [Header("🎵 Global Audio Settings")]
        [Tooltip("Volume global para efeitos de threshold")]
        [Range(0f, 1f)]
        public float globalAudioVolume = 0.7f;
        
        [Tooltip("Pitch variation para diferentes thresholds")]
        [Range(0.5f, 2f)]
        public float pitchVariation = 1.2f;
        
        /// <summary>
        /// Cria um preset padrão para jogos de terror psicológico
        /// </summary>
        public static SanityThresholdPreset CreateDefaultPreset()
        {
            var preset = CreateInstance<SanityThresholdPreset>();
            preset.presetName = "Psychological Horror";
            preset.description = "Progressão clássica de terror psicológico com 4 estágios";
            
            // Threshold 1: Estável
            var stable = new SanityThreshold
            {
                name = "Estável",
                minSanityValue = 0.8f,
                maxSanityValue = 1f,
                textureCorruptionIntensity = 0f,
                meshDeformationStrength = 0f,
                normalStrength = 1f,
                deformationFrequency = 1f,
                transitionDuration = 1f,
                horrorEventChance = 0f
            };
            
            // Threshold 2: Ansiedade
            var anxiety = new SanityThreshold
            {
                name = "Ansiedade",
                minSanityValue = 0.6f,
                maxSanityValue = 0.8f,
                textureCorruptionIntensity = 0.3f,
                meshDeformationStrength = 0.5f,
                normalStrength = 1.2f,
                deformationFrequency = 2f,
                transitionDuration = 2f,
                horrorEventChance = 0.05f
            };
            
            // Threshold 3: Angústia
            var distress = new SanityThreshold
            {
                name = "Angústia",
                minSanityValue = 0.3f,
                maxSanityValue = 0.6f,
                textureCorruptionIntensity = 0.6f,
                meshDeformationStrength = 1.5f,
                normalStrength = 1.8f,
                deformationFrequency = 3f,
                transitionDuration = 3f,
                horrorEventChance = 0.15f
            };
            
            // Threshold 4: Colapso
            var breakdown = new SanityThreshold
            {
                name = "Colapso Mental",
                minSanityValue = 0f,
                maxSanityValue = 0.3f,
                textureCorruptionIntensity = 1f,
                meshDeformationStrength = 3f,
                normalStrength = 2.5f,
                deformationFrequency = 5f,
                transitionDuration = 4f,
                horrorEventChance = 0.3f
            };
            
            preset.thresholds.Add(stable);
            preset.thresholds.Add(anxiety);
            preset.thresholds.Add(distress);
            preset.thresholds.Add(breakdown);
            
            return preset;
        }
        
        /// <summary>
        /// Cria um preset para terror sutil
        /// </summary>
        public static SanityThresholdPreset CreateSubtleHorrorPreset()
        {
            var preset = CreateInstance<SanityThresholdPreset>();
            preset.presetName = "Subtle Horror";
            preset.description = "Terror mais sutil com transições graduais";
            
            // Valores mais baixos e transições mais longas
            var stable = new SanityThreshold
            {
                name = "Normal",
                minSanityValue = 0.9f,
                maxSanityValue = 1f,
                textureCorruptionIntensity = 0f,
                meshDeformationStrength = 0f,
                transitionDuration = 5f,
                horrorEventChance = 0f
            };
            
            var unease = new SanityThreshold
            {
                name = "Inquietação",
                minSanityValue = 0.7f,
                maxSanityValue = 0.9f,
                textureCorruptionIntensity = 0.2f,
                meshDeformationStrength = 0.3f,
                transitionDuration = 8f,
                horrorEventChance = 0.02f
            };
            
            var tension = new SanityThreshold
            {
                name = "Tensão",
                minSanityValue = 0.4f,
                maxSanityValue = 0.7f,
                textureCorruptionIntensity = 0.4f,
                meshDeformationStrength = 0.8f,
                transitionDuration = 10f,
                horrorEventChance = 0.08f
            };
            
            var dread = new SanityThreshold
            {
                name = "Pavor",
                minSanityValue = 0f,
                maxSanityValue = 0.4f,
                textureCorruptionIntensity = 0.7f,
                meshDeformationStrength = 1.5f,
                transitionDuration = 12f,
                horrorEventChance = 0.2f
            };
            
            preset.thresholds.Add(stable);
            preset.thresholds.Add(unease);
            preset.thresholds.Add(tension);
            preset.thresholds.Add(dread);
            
            return preset;
        }
        
        /// <summary>
        /// Cria um preset para terror intenso
        /// </summary>
        public static SanityThresholdPreset CreateIntenseHorrorPreset()
        {
            var preset = CreateInstance<SanityThresholdPreset>();
            preset.presetName = "Intense Horror";
            preset.description = "Terror intenso com efeitos dramáticos";
            
            var calm = new SanityThreshold
            {
                name = "Calmo",
                minSanityValue = 0.75f,
                maxSanityValue = 1f,
                textureCorruptionIntensity = 0.1f,
                meshDeformationStrength = 0f,
                transitionDuration = 0.5f,
                horrorEventChance = 0.01f
            };
            
            var nervous = new SanityThreshold
            {
                name = "Nervoso",
                minSanityValue = 0.5f,
                maxSanityValue = 0.75f,
                textureCorruptionIntensity = 0.5f,
                meshDeformationStrength = 1f,
                transitionDuration = 1f,
                horrorEventChance = 0.1f
            };
            
            var terrified = new SanityThreshold
            {
                name = "Aterrorizado",
                minSanityValue = 0.25f,
                maxSanityValue = 0.5f,
                textureCorruptionIntensity = 0.8f,
                meshDeformationStrength = 2.5f,
                transitionDuration = 1.5f,
                horrorEventChance = 0.25f
            };
            
            var madness = new SanityThreshold
            {
                name = "Loucura",
                minSanityValue = 0f,
                maxSanityValue = 0.25f,
                textureCorruptionIntensity = 1f,
                meshDeformationStrength = 5f,
                transitionDuration = 2f,
                horrorEventChance = 0.5f
            };
            
            preset.thresholds.Add(calm);
            preset.thresholds.Add(nervous);
            preset.thresholds.Add(terrified);
            preset.thresholds.Add(madness);
            
            return preset;
        }
        
        /// <summary>
        /// Aplica este preset a um HorrorPsychSystem
        /// </summary>
        public void ApplyToSystem(HorrorPsychSystem system)
        {
            if (system == null) return;
            
            // Nota: Este método seria implementado quando o HorrorPsychSystem 
            // tiver um método público para definir thresholds
            Debug.Log($"[SanityThresholdPreset] Applied preset '{presetName}' to {system.name}");
        }
        
        /// <summary>
        /// Valida se os thresholds estão configurados corretamente
        /// </summary>
        public bool ValidateThresholds()
        {
            if (thresholds.Count == 0) return false;
            
            // Verifica overlaps
            for (int i = 0; i < thresholds.Count - 1; i++)
            {
                for (int j = i + 1; j < thresholds.Count; j++)
                {
                    var a = thresholds[i];
                    var b = thresholds[j];
                    
                    // Verifica se há overlap
                    if (!(a.maxSanityValue <= b.minSanityValue || b.maxSanityValue <= a.minSanityValue))
                    {
                        Debug.LogWarning($"[SanityThresholdPreset] Overlap between '{a.name}' and '{b.name}'");
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}