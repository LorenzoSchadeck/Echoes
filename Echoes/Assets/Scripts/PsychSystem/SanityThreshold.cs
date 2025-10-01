using UnityEngine;
using System;

namespace Echoes.PsychSystem
{
    /// <summary>
    /// Define um limiar de sanidade e seus efeitos correspondentes.
    /// Usado para configurar diferentes níveis de terror psicológico.
    /// </summary>
    [Serializable]
    public class SanityThreshold
    {
        [Header("🎯 Threshold Configuration")]
        [Tooltip("Nome identificador deste limiar (ex: 'Ansiedade', 'Angústia', 'Colapso')")]
        public string name = "New Threshold";
        
        [Tooltip("Valor mínimo de sanidade para este limiar (0 = insano, 1 = são)")]
        [Range(0f, 1f)]
        public float minSanityValue = 0.5f;
        
        [Tooltip("Valor máximo de sanidade para este limiar")]
        [Range(0f, 1f)]
        public float maxSanityValue = 1f;
        
        [Header("🎨 Visual Corruption Effects")]
        [Tooltip("Intensidade da corrupção visual (texturas)")]
        [Range(0f, 1f)]
        public float textureCorruptionIntensity = 0.3f;
        
        [Tooltip("Força da deformação de mesh")]
        [Range(0f, 5f)]
        public float meshDeformationStrength = 0.5f;
        
        [Tooltip("Frequência da deformação")]
        [Range(0.1f, 10f)]
        public float deformationFrequency = 2f;
        
        [Tooltip("Intensidade dos normais corrompidos")]
        [Range(0f, 3f)]
        public float normalStrength = 1f;
        
        [Header("⏱️ Transition Settings")]
        [Tooltip("Duração da transição para este limiar")]
        public float transitionDuration = 2f;
        
        [Tooltip("Curva de transição")]
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("🎵 Audio & Events")]
        [Tooltip("Som tocado ao entrar neste limiar")]
        public AudioClip thresholdAudio;
        
        [Tooltip("Volume do áudio")]
        [Range(0f, 1f)]
        public float audioVolume = 0.5f;
        
        [Tooltip("Chance de eventos de terror por segundo (0-1)")]
        [Range(0f, 1f)]
        public float horrorEventChance = 0.1f;
        
        /// <summary>
        /// Verifica se um valor de sanidade está dentro deste limiar
        /// </summary>
        public bool IsWithinThreshold(float sanityValue)
        {
            return sanityValue >= minSanityValue && sanityValue <= maxSanityValue;
        }
        
        /// <summary>
        /// Calcula o progresso dentro deste limiar (0-1)
        /// </summary>
        public float GetProgressInThreshold(float sanityValue)
        {
            if (!IsWithinThreshold(sanityValue)) return 0f;
            
            float range = maxSanityValue - minSanityValue;
            if (range <= 0f) return 1f;
            
            return (sanityValue - minSanityValue) / range;
        }
        
        /// <summary>
        /// Aplica a curva de transição ao progresso
        /// </summary>
        public float GetCurvedProgress(float sanityValue)
        {
            float progress = GetProgressInThreshold(sanityValue);
            return transitionCurve.Evaluate(progress);
        }
    }
}