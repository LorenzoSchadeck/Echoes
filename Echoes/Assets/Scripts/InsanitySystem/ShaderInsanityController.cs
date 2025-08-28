using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ShaderInsanityController : MonoBehaviour
{
    [Header("Shader Settings")]
    [Tooltip("Velocidade com que o efeito visual do shader se ajusta à insanidade do jogador.")]
    [SerializeField] private float transitionSpeed = 1.0f;
    
    private Material materialInstance;

    private float targetInsanity = 0f;
    private float currentShaderInsanity = 0f;

    private static readonly int InsanityLevelID = Shader.PropertyToID("_InsanityLevel");

    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        materialInstance = renderer.material; 
    }

    private void OnEnable()
    {
        InsanityManager.OnInsanityChanged += HandleInsanityChange;
    }

    private void OnDisable()
    {
        InsanityManager.OnInsanityChanged -= HandleInsanityChange;
    }

    // Função chamada pelo evento. Apenas atualiza o valor alvo.
    private void HandleInsanityChange(float newInsanityValue)
    {
        targetInsanity = newInsanityValue;
    }

    private void Update()
    {
        // Interpola suavemente o valor atual do shader em direção ao valor alvo
        currentShaderInsanity = Mathf.Lerp(currentShaderInsanity, targetInsanity, Time.deltaTime * transitionSpeed);

        // Aplica o valor suavizado ao material
        if (materialInstance != null)
        {
            materialInstance.SetFloat(InsanityLevelID, currentShaderInsanity);
        }
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}