using UnityEngine;

/// <summary>
/// Componente que identifica o tipo de superfície para sons de footsteps dinâmicos.
/// Deve ser colocado em objetos que representam diferentes tipos de chão.
/// </summary>
public class SurfaceDetector : MonoBehaviour
{
    [Header("Surface Properties")]
    [Tooltip("Tipo de superfície que afeta o som dos passos.")]
    [SerializeField] private SurfaceType surfaceType = SurfaceType.Carpet;
    
    [Tooltip("Valor numérico do parâmetro 'surface' no FMOD (0-based).")]
    [SerializeField] private float surfaceParameterValue = 0f;

    public SurfaceType Surface => surfaceType;
    public float SurfaceParameterValue => surfaceParameterValue;

    private void OnValidate()
    {
        // Sincroniza automaticamente o valor do parâmetro com o enum
        surfaceParameterValue = (float)surfaceType;
    }
}