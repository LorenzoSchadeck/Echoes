using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ShaderInsanityController : MonoBehaviour
{
    [Header("Sanity Thresholds")]
    [Tooltip("A sanidade precisa cair ABAIXO deste valor para que o shader comece a se distorcer.")]
    [SerializeField, Range(0f, 1f)] private float shaderEffectStartThreshold = 0.5f;

    private Material materialInstance;
    private static readonly int InsanityLevelID = Shader.PropertyToID("_InsanityLevel");
    private float currentSanity = 1.0f;

    private void Awake()
    {
        Renderer renderer = GetComponent<Renderer>();
        materialInstance = renderer.material; 
    }

    private void OnEnable()
    {
        InsanityManager.OnSanityChanged += HandleSanityChange;
    }

    private void OnDisable()
    {
        InsanityManager.OnSanityChanged -= HandleSanityChange;
    }

    private void Update()
    {
        // Calcula o 'insanityLevel' (0 a 1) para o shader.
        // O efeito vai de 0% a 100% conforme a sanidade cai do limiar até 0.
        float insanityLevel = Mathf.InverseLerp(shaderEffectStartThreshold, 0f, currentSanity);

        if (materialInstance != null)
        {
            materialInstance.SetFloat(InsanityLevelID, insanityLevel);
        }
    }

    // Função chamada pelo evento. Apenas atualiza o valor alvo.
    private void HandleSanityChange(float newSanity)
    {
        currentSanity = newSanity;
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}