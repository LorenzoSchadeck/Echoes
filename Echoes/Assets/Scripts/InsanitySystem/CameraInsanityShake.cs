using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

namespace Echoes.InsanitySystem
{
    /// <summary>
    /// Controla o shake da câmera baseado na sanidade usando Cinemachine Basic Multi Channel Perlin.
    /// O shake aumenta progressivamente de 50% a 30% de sanidade, atingindo intensidade máxima aos 30%.
    /// Abaixo de 30%, o shake permanece no máximo, criando tensão visual constante.
    /// </summary>
    public class CameraInsanityShake : MonoBehaviour
    {
        [Header("🎥 Cinemachine Setup")]
        [Tooltip("A Cinemachine Camera com o componente Basic Multi Channel Perlin")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        
        [Header("📊 Shake Configuration")]
        [Tooltip("Sanidade abaixo deste valor ativa o shake da câmera")]
        [SerializeField, Range(0f, 1f)] private float shakeActivationThreshold = 0.5f;
        
        [Tooltip("Sanidade onde o shake atinge seu valor máximo")]
        [SerializeField, Range(0f, 1f)] private float shakeMaxIntensityThreshold = 0.3f;
        
        [Tooltip("Amplitude máxima do shake (0 a 1.5)")]
        [SerializeField, Range(0f, 3f)] private float maxAmplitude = 1.5f;
        
        [Tooltip("Frequência máxima do shake (0 a 0.8)")]
        [SerializeField, Range(0f, 2f)] private float maxFrequency = 0.8f;
        
        [Header("⚡ Transition Settings")]
        [Tooltip("Duração da transição suave quando o remédio é usado")]
        [SerializeField, Range(1f, 10f)] private float remedyTransitionDuration = 3f;
        
        [Tooltip("Velocidade da transição normal do shake")]
        [SerializeField, Range(0.1f, 5f)] private float normalTransitionSpeed = 2f;
        
        [Header("🔧 Advanced Settings")]
        [Tooltip("Curve para controlar a progressão do shake baseado na sanidade")]
        [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("🐛 Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showDebugGUI = false;
        
        // Componentes e referências
        private CinemachineBasicMultiChannelPerlin perlinNoise;
        private float currentSanity = 1f;
        private float targetAmplitude = 0f;
        private float targetFrequency = 0f;
        private float currentAmplitude = 0f;
        private float currentFrequency = 0f;
        
        // Controle de transições
        private bool isRemedyTransitionActive = false;
        private Coroutine remedyTransitionCoroutine;
        
        private void Awake()
        {
            // Busca automaticamente a cinemachine camera se não foi atribuída
            if (cinemachineCamera == null)
            {
                cinemachineCamera = GetComponent<CinemachineCamera>();
                
                if (cinemachineCamera == null)
                {
                    cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
                }
            }
            
            // Obtém o componente Basic Multi Channel Perlin
            if (cinemachineCamera != null)
            {
                perlinNoise = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
                
                if (perlinNoise == null)
                {
                    Debug.LogError($"[CameraInsanityShake] {name}: Cinemachine Camera não tem componente Basic Multi Channel Perlin!");
                    enabled = false;
                    return;
                }
                
                // Inicializa com valores zero
                perlinNoise.AmplitudeGain = 0f;
                perlinNoise.FrequencyGain = 0f;
            }
            else
            {
                Debug.LogError($"[CameraInsanityShake] {name}: Nenhuma Cinemachine Camera encontrada!");
                enabled = false;
            }
        }
        
        private void OnEnable()
        {
            InsanityManager.OnSanityChanged += HandleSanityChange;
            GameEvents.OnRemedyUsed += HandleRemedyUsed;
            GameEvents.OnDeathSequenceCancelled += HandleRemedyUsed;
            GameEvents.OnFlashbackStarted += HandleFlashbackStarted;
            GameEvents.OnFlashbackEnded += HandleFlashbackEnded;
        }
        
        private void OnDisable()
        {
            InsanityManager.OnSanityChanged -= HandleSanityChange;
            GameEvents.OnRemedyUsed -= HandleRemedyUsed;
            GameEvents.OnDeathSequenceCancelled -= HandleRemedyUsed;
            GameEvents.OnFlashbackStarted -= HandleFlashbackStarted;
            GameEvents.OnFlashbackEnded -= HandleFlashbackEnded;
        }
        
        private void Update()
        {
            if (perlinNoise == null || isRemedyTransitionActive) return;
            
            // Transição suave para os valores alvo
            currentAmplitude = Mathf.MoveTowards(currentAmplitude, targetAmplitude, normalTransitionSpeed * Time.deltaTime);
            currentFrequency = Mathf.MoveTowards(currentFrequency, targetFrequency, normalTransitionSpeed * Time.deltaTime);
            
            // Aplica os valores ao componente Cinemachine
            perlinNoise.AmplitudeGain = currentAmplitude;
            perlinNoise.FrequencyGain = currentFrequency;
        }
        
        private void HandleSanityChange(float newSanity)
        {
            currentSanity = newSanity;
            CalculateShakeValues();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[CameraInsanityShake] Sanity: {newSanity:F2} | Target Amplitude: {targetAmplitude:F2} | Target Frequency: {targetFrequency:F2}");
            }
        }
        
        private void CalculateShakeValues()
        {
            if (currentSanity >= shakeActivationThreshold)
            {
                // Acima do threshold: sem shake
                targetAmplitude = 0f;
                targetFrequency = 0f;
            }
            else if (currentSanity >= shakeMaxIntensityThreshold)
            {
                // Entre 50% e 30%: shake aumenta progressivamente
                // Quando sanidade = 50% -> fator = 0
                // Quando sanidade = 30% -> fator = 1
                float intensityFactor = Mathf.InverseLerp(shakeActivationThreshold, shakeMaxIntensityThreshold, currentSanity);
                
                // Aplica a curva de progressão
                intensityFactor = shakeCurve.Evaluate(intensityFactor);
                
                // Calcula os valores finais
                targetAmplitude = intensityFactor * maxAmplitude;
                targetFrequency = intensityFactor * maxFrequency;
            }
            else
            {
                // Abaixo de 30%: shake permanece no máximo
                targetAmplitude = maxAmplitude;
                targetFrequency = maxFrequency;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[CameraInsanityShake] Shake at maximum intensity (Sanity: {currentSanity:F2})");
                }
            }
        }
        
        private void HandleRemedyUsed()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[CameraInsanityShake] Remedy used - starting smooth transition to calm state");
            }
            
            // Para qualquer transição anterior
            if (remedyTransitionCoroutine != null)
            {
                StopCoroutine(remedyTransitionCoroutine);
            }
            
            // Inicia transição suave para estado calmo
            remedyTransitionCoroutine = StartCoroutine(RemedyTransitionRoutine());
        }
        
        private void HandleFlashbackStarted()
        {
            // Durante flashbacks, o shake é desativado para não interferir na narrativa
            if (enableDebugLogs)
            {
                Debug.Log("[CameraInsanityShake] Flashback started - disabling shake");
            }
            
            targetAmplitude = 0f;
            targetFrequency = 0f;
        }
        
        private void HandleFlashbackEnded()
        {
            // Quando o flashback termina, recalcula baseado na sanidade atual
            if (enableDebugLogs)
            {
                Debug.Log("[CameraInsanityShake] Flashback ended - recalculating shake");
            }
            
            CalculateShakeValues();
        }
        
        private IEnumerator RemedyTransitionRoutine()
        {
            isRemedyTransitionActive = true;
            
            float startAmplitude = currentAmplitude;
            float startFrequency = currentFrequency;
            float targetAmplitudeClean = 0f;
            float targetFrequencyClean = 0f;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < remedyTransitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / remedyTransitionDuration;
                
                // Aplica curva suave para transição mais natural
                t = Mathf.SmoothStep(0f, 1f, t);
                
                // Interpola suavemente para estado calmo
                currentAmplitude = Mathf.Lerp(startAmplitude, targetAmplitudeClean, t);
                currentFrequency = Mathf.Lerp(startFrequency, targetFrequencyClean, t);
                
                // Aplica ao componente Cinemachine
                if (perlinNoise != null)
                {
                    perlinNoise.AmplitudeGain = currentAmplitude;
                    perlinNoise.FrequencyGain = currentFrequency;
                }
                
                yield return null;
            }
            
            // Garante estado final limpo
            currentAmplitude = 0f;
            currentFrequency = 0f;
            targetAmplitude = 0f;
            targetFrequency = 0f;
            
            if (perlinNoise != null)
            {
                perlinNoise.AmplitudeGain = 0f;
                perlinNoise.FrequencyGain = 0f;
            }
            
            isRemedyTransitionActive = false;
            remedyTransitionCoroutine = null;
            
            if (enableDebugLogs)
            {
                Debug.Log("[CameraInsanityShake] Remedy transition completed - camera stabilized");
            }
        }
        
        /// <summary>
        /// Aplica um shake temporário instantâneo (para eventos específicos como jump scares)
        /// </summary>
        /// <param name="amplitude">Amplitude do shake temporário</param>
        /// <param name="frequency">Frequência do shake temporário</param>
        /// <param name="duration">Duração do shake temporário</param>
        public void ApplyTemporaryShake(float amplitude, float frequency, float duration)
        {
            StartCoroutine(TemporaryShakeRoutine(amplitude, frequency, duration));
        }
        
        private IEnumerator TemporaryShakeRoutine(float amplitude, float frequency, float duration)
        {
            if (perlinNoise == null) yield break;
            
            // Salva valores atuais
            float savedAmplitude = perlinNoise.AmplitudeGain;
            float savedFrequency = perlinNoise.FrequencyGain;
            
            // Aplica shake temporário
            perlinNoise.AmplitudeGain = amplitude;
            perlinNoise.FrequencyGain = frequency;
            
            // Espera a duração
            yield return new WaitForSeconds(duration);
            
            // Restaura valores anteriores
            perlinNoise.AmplitudeGain = savedAmplitude;
            perlinNoise.FrequencyGain = savedFrequency;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[CameraInsanityShake] Temporary shake completed (A:{amplitude:F2}, F:{frequency:F2}, D:{duration:F2}s)");
            }
        }
        
        /// <summary>
        /// Força uma atualização imediata do shake baseado na sanidade atual
        /// </summary>
        public void ForceUpdateShake()
        {
            CalculateShakeValues();
            
            if (perlinNoise != null)
            {
                perlinNoise.AmplitudeGain = targetAmplitude;
                perlinNoise.FrequencyGain = targetFrequency;
            }
        }
        
        private void OnDestroy()
        {
            if (remedyTransitionCoroutine != null)
            {
                StopCoroutine(remedyTransitionCoroutine);
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 320, 300, 200));
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label("🎥 Camera Shake Debug");
            GUILayout.Label($"Current Sanity: {currentSanity:F2}");
            GUILayout.Label($"Activation Threshold: {shakeActivationThreshold:F2}");
            GUILayout.Label($"Max Intensity Threshold: {shakeMaxIntensityThreshold:F2}");
            GUILayout.Label($"Current Amplitude: {currentAmplitude:F2}");
            GUILayout.Label($"Current Frequency: {currentFrequency:F2}");
            GUILayout.Label($"Target Amplitude: {targetAmplitude:F2}");
            GUILayout.Label($"Target Frequency: {targetFrequency:F2}");
            
            // Status visual
            if (isRemedyTransitionActive)
            {
                GUILayout.Label("💊 REMEDY TRANSITION", GUI.skin.box);
            }
            else if (currentSanity < shakeMaxIntensityThreshold)
            {
                GUILayout.Label("🔴 MAXIMUM SHAKE", GUI.skin.box);
            }
            else if (currentSanity < shakeActivationThreshold)
            {
                GUILayout.Label("📳 SHAKE ACTIVE", GUI.skin.box);
            }
            else
            {
                GUILayout.Label("😌 CALM", GUI.skin.box);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Retorna informações de status do sistema para debug
        /// </summary>
        public string GetShakeStats()
        {
            string status = "Inactive";
            if (currentSanity < shakeMaxIntensityThreshold)
                status = "Maximum";
            else if (currentSanity < shakeActivationThreshold)
                status = "Active";
                
            return $"Sanity: {currentSanity:F2} | Shake: {status} | Amplitude: {currentAmplitude:F2} | Frequency: {currentFrequency:F2}";
        }
    }
}