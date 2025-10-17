using UnityEngine;
using System.Collections;

public class AlarmClockController : MonoBehaviour
{
    // Range padrão para áudio espacial (mesmo que o rádio e outros eventos)
    private const float STANDARD_AUDIO_MAX_RANGE = 70f;
    
    [Header("3D Sanity Bar Components")]
    [Tooltip("Transform do quad da barra (será usado como container).")]
    [SerializeField] private Transform sanityBarContainer;
    [Tooltip("MeshRenderer da barra que será manipulada.")]
    [SerializeField] private MeshRenderer sanityBarRenderer;
    [Tooltip("MeshFilter da barra para manipulação do mesh.")]
    [SerializeField] private MeshFilter sanityBarMeshFilter;

    [Header("Visual Feedback")]
    [SerializeField] private Color highSanityColor = Color.green;
    [SerializeField] private Color midSanityColor = Color.yellow; 
    [SerializeField] private Color lowSanityColor = Color.red;
    [SerializeField] private float colorTransitionSmoothness = 2f;

    [Header("Alarm Components")]
    [SerializeField] private GameObject alarmBlinkObject;
    [SerializeField] private FMODUnity.EventReference alarmEvent;
    [SerializeField] private float blinkInterval = 0.5f;

    [Header("🔊 Audio Spatial Settings")]
    [Tooltip("Distância mínima onde o volume do alarme é máximo")]
    [SerializeField, Range(1f, 50f)] private float alarmMinDistance = 5f;
    
    [Tooltip("Distância máxima onde o alarme ainda é audível (padronizada igual ao rádio)")]
    [SerializeField, Range(10f, 500f)] private float alarmMaxDistance = STANDARD_AUDIO_MAX_RANGE; // Padronizado igual ao rádio

    private FMODAudioTrigger audioTrigger;
    private Coroutine blinkingLightRoutine;
    
    // Estado atual da sanidade para a barra 3D
    private float currentSanityLevel = 1f;
    
    // Mesh original e MaterialPropertyBlock
    private Mesh originalMesh;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
        propBlock = new MaterialPropertyBlock();
        
        // Configura a range do áudio do alarme usando as variáveis configuráveis
        if (audioTrigger != null)
        {
            audioTrigger.SetSpatialRange(alarmMinDistance, alarmMaxDistance);
        }
        
        // Armazena o mesh original da barra
        if (sanityBarMeshFilter != null)
        {
            originalMesh = sanityBarMeshFilter.mesh;
        }
    }

    private void Start()
    {
        // Inicializa a barra com sanidade máxima
        Update3DSanityBar();
    }

    private void OnEnable()
    {
        InsanityManager.OnSanityChanged += UpdateSanityDisplay;
        GameEvents.OnDeathSequenceStarted += StartAlarm;
        GameEvents.OnDeathSequenceCancelled += StopAlarm;
        GameEvents.OnFalseAlarmTriggered += PlayFalseAlarm;
        GameEvents.OnFlashbackEnded += StopAlarm;
    }

    private void OnDisable()
    {
        InsanityManager.OnSanityChanged -= UpdateSanityDisplay;
        GameEvents.OnDeathSequenceStarted -= StartAlarm;
        GameEvents.OnDeathSequenceCancelled -= StopAlarm;
        GameEvents.OnFalseAlarmTriggered -= PlayFalseAlarm;
        GameEvents.OnFlashbackEnded -= StopAlarm;
    }

    /// <summary>
    /// Atualiza o nível de sanidade e atualiza a barra 3D.
    /// </summary>
    private void UpdateSanityDisplay(float currentSanity)
    {
        // Clamp para garantir valores válidos
        currentSanityLevel = Mathf.Clamp01(currentSanity);
        
        // Atualiza a barra 3D
        Update3DSanityBar();
    }

    /// <summary>
    /// Atualiza a barra 3D com efeito de "tanque esvaziando" usando manipulação de mesh.
    /// </summary>
    private void Update3DSanityBar()
    {
        if (sanityBarMeshFilter == null || sanityBarRenderer == null) return;

        // Calcula a cor com transição suave
        Color barColor = CalculateSmoothSanityColor(currentSanityLevel);
        
        // Aplica a cor usando MaterialPropertyBlock
        sanityBarRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", barColor);
        propBlock.SetColor("_Color", barColor); // Fallback para shaders padrão
        sanityBarRenderer.SetPropertyBlock(propBlock);

        // Cria o mesh com clipping vertical para simular o esvaziamento
        CreateClippedMesh(currentSanityLevel);
    }

    /// <summary>
    /// Cria um mesh "cortado" verticalmente para simular o efeito de tanque esvaziando.
    /// </summary>
    private void CreateClippedMesh(float fillPercentage)
    {
        if (originalMesh == null) return;

        // Clamp para garantir valores válidos
        fillPercentage = Mathf.Clamp01(fillPercentage);

        // Cria uma cópia do mesh original
        Mesh clippedMesh = new Mesh();
        clippedMesh.name = "Sanity Bar Clipped";

        Vector3[] originalVertices = originalMesh.vertices;
        Vector2[] originalUVs = originalMesh.uv;
        int[] originalTriangles = originalMesh.triangles;

        // Encontra os valores mín e máx de Y para calcular a altura correta
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (originalVertices[i].y < minY) minY = originalVertices[i].y;
            if (originalVertices[i].y > maxY) maxY = originalVertices[i].y;
        }
        
        float totalHeight = maxY - minY;
        float targetHeight = totalHeight * fillPercentage;
        float newMaxY = minY + targetHeight;

        // Debug.Log($"Mesh Analysis: MinY={minY}, MaxY={maxY}, TotalHeight={totalHeight}, FillPercentage={fillPercentage}, NewMaxY={newMaxY}");

        Vector3[] newVertices = new Vector3[originalVertices.Length];
        Vector2[] newUVs = new Vector2[originalUVs.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            newVertices[i] = originalVertices[i];
            newUVs[i] = originalUVs[i];

            // Para vértices que estão acima do novo topo, reposiciona para o novo topo
            if (originalVertices[i].y > newMaxY)
            {
                newVertices[i].y = newMaxY;
                
                // Ajusta o UV proporcionalmente
                float uvProgress = (newMaxY - minY) / totalHeight;
                float originalUVRange = originalUVs[i].y - (originalUVs[i].y * (originalVertices[i].y - minY) / totalHeight);
                newUVs[i].y = originalUVRange + (originalUVs[i].y * uvProgress);
            }
        }

        clippedMesh.vertices = newVertices;
        clippedMesh.uv = newUVs;
        clippedMesh.triangles = originalTriangles;
        clippedMesh.RecalculateBounds();
        clippedMesh.RecalculateNormals();

        // Aplica o novo mesh
        sanityBarMeshFilter.mesh = clippedMesh;
    }

    /// <summary>
    /// Calcula a cor da barra de sanidade com transição suave entre verde, amarelo e vermelho.
    /// </summary>
    private Color CalculateSmoothSanityColor(float sanityValue)
    {
        // Normaliza o valor de sanidade para aplicar suavização
        float smoothValue = Mathf.Pow(sanityValue, 1f / colorTransitionSmoothness);

        if (smoothValue > 0.5f)
        {
            // Transição de verde para amarelo (sanidade alta para média)
            float t = (smoothValue - 0.5f) * 2f; // Mapeia 0.5-1.0 para 0-1
            return Color.Lerp(midSanityColor, highSanityColor, t);
        }
        else
        {
            // Transição de vermelho para amarelo (sanidade baixa para média)
            float t = smoothValue * 2f; // Mapeia 0-0.5 para 0-1
            return Color.Lerp(lowSanityColor, midSanityColor, t);
        }
    }

    /// <summary>
    /// Toca o alarme real e contínuo quando a sequência de morte começa.
    /// </summary>
    private void StartAlarm(float ignoredDuration = 0f)
    {
        // Se o alarme já estiver tocando, não faz nada
        if (blinkingLightRoutine != null) return;
        Debug.Log("<color=orange>ALARM STARTED</color>");
        if (alarmBlinkObject != null)
        {
            blinkingLightRoutine = StartCoroutine(BlinkingObjectRoutine());
        }
        if (!alarmEvent.IsNull)
        {
            audioTrigger.fmodEvent = alarmEvent;
            audioTrigger.PlayAtPosition(transform.position);
        }
    }

    // Função única para PARAR o alarme
    public void StopAlarm()
    {
        Debug.Log("<color=cyan>ALARM STOPPED</color>");
        if (blinkingLightRoutine != null)
        {
            StopCoroutine(blinkingLightRoutine);
            blinkingLightRoutine = null;
        }
        if (alarmBlinkObject != null) alarmBlinkObject.SetActive(false);
        audioTrigger.Stop();
    }
    
    // Alarme falso agora chama as funções principais
    private void PlayFalseAlarm(float duration)
    {
        StartCoroutine(FalseAlarmRoutine(duration));
    }

    private IEnumerator FalseAlarmRoutine(float duration)
    {
        StartAlarm(); // Usa a função de início padrão
        // FMOD: evento de alarme deve ser one-shot ou controlado via lógica do evento
        yield return new WaitForSeconds(duration);
        StopAlarm(); // Usa a função de parada padrão
    }

    private IEnumerator BlinkingObjectRoutine()
    {
        if (alarmBlinkObject == null) yield break;
        alarmBlinkObject.SetActive(false);
        
        while (true)
        {
            alarmBlinkObject.SetActive(!alarmBlinkObject.activeSelf);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    /// <summary>
    /// Atualiza as configurações de range espacial do áudio do alarme
    /// </summary>
    /// <param name="minDistance">Nova distância mínima</param>
    /// <param name="maxDistance">Nova distância máxima</param>
    public void UpdateAlarmAudioRange(float minDistance, float maxDistance)
    {
        alarmMinDistance = Mathf.Clamp(minDistance, 1f, 50f);
        alarmMaxDistance = Mathf.Clamp(maxDistance, 10f, 500f);
        
        if (audioTrigger != null)
        {
            audioTrigger.SetSpatialRange(alarmMinDistance, alarmMaxDistance);
        }
        
        Debug.Log($"[AlarmClockController] Audio range atualizada: {alarmMinDistance:F1}m - {alarmMaxDistance:F1}m");
    }

    /// <summary>
    /// Método de teste para verificar diferentes níveis de sanidade.
    /// Remove depois dos testes!
    /// </summary>
    public void TestSanityLevel(float testLevel)
    {
        Debug.Log($"<color=yellow>TESTING SANITY LEVEL: {testLevel}</color>");
        UpdateSanityDisplay(testLevel);
    }

    // Métodos de teste no Inspector (durante desenvolvimento)
    [ContextMenu("Test Sanity 100%")]
    private void TestSanity100() => TestSanityLevel(1.0f);
    
    [ContextMenu("Test Sanity 75%")]
    private void TestSanity75() => TestSanityLevel(0.75f);
    
    [ContextMenu("Test Sanity 50%")]
    private void TestSanity50() => TestSanityLevel(0.5f);
    
    [ContextMenu("Test Sanity 25%")]
    private void TestSanity25() => TestSanityLevel(0.25f);
    
    [ContextMenu("Test Sanity 0%")]
    private void TestSanity0() => TestSanityLevel(0.0f);

    // Métodos de teste para configurações de áudio
    [ContextMenu("Test Audio - Short Range")]
    private void TestAudioShortRange() => UpdateAlarmAudioRange(2f, 50f);
    
    [ContextMenu("Test Audio - Medium Range")]
    private void TestAudioMediumRange() => UpdateAlarmAudioRange(5f, 100f);
    
    [ContextMenu("Test Audio - Long Range")]
    private void TestAudioLongRange() => UpdateAlarmAudioRange(5f, 200f);
    
    [ContextMenu("Test Audio - Maximum Range")]
    private void TestAudioMaxRange() => UpdateAlarmAudioRange(10f, 500f);
}