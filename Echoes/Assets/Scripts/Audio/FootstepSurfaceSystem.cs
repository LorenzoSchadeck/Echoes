using UnityEngine;
using FMODUnity;

/// <summary>
/// Sistema responsável por detectar a superfície sob os pés do jogador
/// e aplicar os parâmetros corretos no evento FMOD de footsteps.
/// </summary>
public class FootstepSurfaceSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Distância máxima para detectar superfícies.")]
    [SerializeField] private float detectionDistance = 1.0f;
    
    [Tooltip("Layer das superfícies que podem ser detectadas.")]
    [SerializeField] private LayerMask surfaceLayer = -1;
    
    [Tooltip("Superfície padrão quando nenhuma é detectada.")]
    [SerializeField] private SurfaceType defaultSurface = SurfaceType.Tiles;
    
    [Tooltip("Ignorar colliders do próprio jogador (opcional).")]
    [SerializeField] private bool ignorePlayerColliders = true;

    private SurfaceType currentSurface;
    private float currentSurfaceValue;

    public SurfaceType CurrentSurface => currentSurface;
    public float CurrentSurfaceValue => currentSurfaceValue;

    private void Start()
    {
        // Inicializa com a superfície padrão
        SetCurrentSurface(defaultSurface);
    }

    /// <summary>
    /// Detecta a superfície sob os pés do jogador usando raycast.
    /// </summary>
    /// <returns>True se uma superfície foi detectada, false caso contrário.</returns>
    public bool DetectSurface()
    {
        // Começar o raycast bem acima do jogador para garantir que não comece dentro do collider
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 rayDirection = Vector3.down;
        // Aumentar a distância para compensar o ponto de início mais alto
        float totalDistance = detectionDistance + 0.5f;

        // Usar RaycastAll para poder filtrar colliders do jogador se necessário
        RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDirection, totalDistance, surfaceLayer);
        
        // Ordenar por distância (mais próximo primeiro)
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        foreach (RaycastHit hit in hits)
        {
            // Se deve ignorar colliders do jogador, pula se for do mesmo GameObject
            if (ignorePlayerColliders && hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }
            
            // Procura por um SurfaceDetector no objeto atingido
            SurfaceDetector surfaceDetector = hit.collider.GetComponent<SurfaceDetector>();
            
            if (surfaceDetector != null)
            {
                SetCurrentSurface(surfaceDetector.Surface, surfaceDetector.SurfaceParameterValue);
                return true;
            }
            else
            {
                // Se não há SurfaceDetector, usa a superfície padrão
                SetCurrentSurface(defaultSurface);
                return true;
            }
        }
        
        // Se chegou aqui, nenhuma superfície válida foi encontrada
        return false;
    }

    /// <summary>
    /// Define a superfície atual e seu valor de parâmetro.
    /// </summary>
    private void SetCurrentSurface(SurfaceType surface, float? customValue = null)
    {
        currentSurface = surface;
        currentSurfaceValue = customValue ?? (float)surface;
    }

    /// <summary>
    /// Aplica o parâmetro de superfície em um evento FMOD.
    /// </summary>
    public void ApplySurfaceParameter(FMODAudioTrigger audioTrigger, string parameterName)
    {
        if (audioTrigger == null || string.IsNullOrEmpty(parameterName)) return;
        
        audioTrigger.SetParameter(parameterName, currentSurfaceValue);
    }
    
    // Debug visual
    private void OnDrawGizmosSelected()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        float totalDistance = detectionDistance + 0.5f;
        Vector3 rayEnd = rayStart + Vector3.down * totalDistance;
        
        // Testa o raycast em tempo real para mostrar resultado correto
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, totalDistance, surfaceLayer);
        
        // Cor do ray baseada no resultado
        Gizmos.color = hits.Length > 0 ? Color.green : Color.red;
        Gizmos.DrawLine(rayStart, rayEnd);
        
        // Desenha uma esfera na ponta do ray
        Gizmos.DrawWireSphere(rayEnd, 0.1f);
        
        // Se atingiu algo, desenha o ponto de colisão
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(hit.point, 0.05f);
            }
        }
    }
}

/// <summary>
/// Tipos de superfície suportados pelo sistema de footsteps.
/// Os valores correspondem aos parâmetros no FMOD Studio.
/// </summary>
public enum SurfaceType
{
    Tiles = 0,
    Carpet = 1 
}