using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gerenciador simples para piscadas de luz no sistema de eventos de horror.
/// Funciona com listas específicas de luzes para efeitos coordenados simples.
/// </summary>
public class SimpleLightFlickerManager : MonoBehaviour
{
    public static SimpleLightFlickerManager Instance { get; private set; }

    [Header("Default Flicker Settings")]
    [Tooltip("Duração padrão das piscadas.")]
    [SerializeField] private float defaultFlickerDuration = 2f;
    
    [Tooltip("Número padrão de piscadas.")]
    [SerializeField] private int defaultFlickerCount = 3;
    
    [Tooltip("Intensidade mínima padrão durante piscadas.")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMinIntensity = 0.1f;

    [Header("Debug")]
    [Tooltip("Mostra logs do sistema de piscadas.")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnSimpleLightFlickerTriggered += OnSimpleLightFlickerTriggered;
    }

    private void OnDisable()
    {
        GameEvents.OnSimpleLightFlickerTriggered -= OnSimpleLightFlickerTriggered;
    }

    private void OnSimpleLightFlickerTriggered(List<Light> lights, FMODUnity.EventReference flickerSound, GameObject soundTarget)
    {
        if (lights == null || lights.Count == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("SimpleLightFlickerManager: Lista de luzes vazia!");
            return;
        }

        StartCoroutine(SimpleFlickerCoroutine(lights, flickerSound, soundTarget));
    }

    private IEnumerator SimpleFlickerCoroutine(List<Light> lights, FMODUnity.EventReference flickerSound, GameObject soundTarget)
    {
        if (enableDebugLogs)
        {
            string audioInfo = "NÃO";
            if (!flickerSound.IsNull)
            {
                audioInfo = soundTarget != null ? "SIM (posição fixa)" : $"SIM ({lights.Count} posições)";
            }
            Debug.Log($"Iniciando piscadas simples em {lights.Count} luzes com áudio: {audioInfo}");
        }

        // Armazena intensidades originais
        Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();
        foreach (Light light in lights)
        {
            if (light != null)
            {
                originalIntensities[light] = light.intensity;
            }
        }

        float flickerInterval = defaultFlickerDuration / defaultFlickerCount;

        for (int i = 0; i < defaultFlickerCount; i++)
        {
            // Toca som da piscada ANTES de escurecer - EM CADA LUZ
            if (!flickerSound.IsNull)
            {
                // Se há soundTarget, usa ele. Senão, toca em cada luz individual
                if (soundTarget != null)
                {
                    PlayFlickerSound(flickerSound, soundTarget.transform.position);
                }
                else
                {
                    // Toca som na posição de cada luz
                    foreach (Light light in lights)
                    {
                        if (light != null)
                        {
                            PlayFlickerSound(flickerSound, light.transform.position);
                        }
                    }
                }
            }

            // Escurece as luzes
            foreach (Light light in lights)
            {
                if (light != null && originalIntensities.ContainsKey(light))
                {
                    light.intensity = originalIntensities[light] * defaultMinIntensity;
                }
            }

            yield return new WaitForSeconds(flickerInterval * 0.3f);

            // Restaura intensidade original
            foreach (Light light in lights)
            {
                if (light != null && originalIntensities.ContainsKey(light))
                {
                    light.intensity = originalIntensities[light];
                }
            }

            yield return new WaitForSeconds(flickerInterval * 0.7f);
        }

        if (enableDebugLogs)
            Debug.Log("Piscadas simples concluídas");
    }

    /// <summary>
    /// Toca o som de uma piscada usando FMOD com posicionamento 3D e range padrão.
    /// </summary>
    private void PlayFlickerSound(FMODUnity.EventReference soundEvent, Vector3 position)
    {
        var eventInstance = FMODUnity.RuntimeManager.CreateInstance(soundEvent);
        
        if (eventInstance.isValid())
        {
            // Define posição 3D
            eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
            
            // Define range máximo igual aos outros eventos de horror (70f)
            eventInstance.setProperty(FMOD.Studio.EVENT_PROPERTY.MAXIMUM_DISTANCE, 70f);
            
            // Inicia reprodução
            eventInstance.start();
            
            // Libera automaticamente quando terminar
            eventInstance.release();

            if (enableDebugLogs)
                Debug.Log($"Som de piscada tocado na posição: {position}");
        }
    }

    /// <summary>
    /// Método público para triggerar piscadas manualmente.
    /// </summary>
    public void TriggerFlicker(List<Light> lights, FMODUnity.EventReference flickerSound = default, GameObject soundTarget = null)
    {
        OnSimpleLightFlickerTriggered(lights, flickerSound, soundTarget);
    }

#if UNITY_EDITOR
    [Header("Editor Tools")]
    [SerializeField] private bool showDebugInfo = true;

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 100, 300, 80));
        GUILayout.Label("Simple Light Flicker Manager");
        GUILayout.Label($"Duração: {defaultFlickerDuration}s");
        GUILayout.Label($"Contagem: {defaultFlickerCount}");
        GUILayout.Label($"Intensidade Mín: {defaultMinIntensity}");
        GUILayout.EndArea();
    }
#endif
}