using UnityEngine;

/// <summary>
/// Ativa transmissões de rádio via GameEvents ao detectar o player entrando no trigger.
/// Segue o padrão de arquitetura do projeto usando comunicação desacoplada.
/// </summary>
public class RadioTriggerActivator : MonoBehaviour
{
    [Header("Configuração da Transmissão")]
    [Tooltip("Índice da transmissão a ser ativada (baseado no array do RadioController).")]
    [SerializeField] private int transmissionIndex = 0;
    
    [Tooltip("Se true, usa o evento OnRadioActivated (primeira vez). Se false, usa OnRadioTransmissionStarted.")]
    [SerializeField] private bool isFirstActivation = true;
    
    [Tooltip("Se true, só pode ser ativado uma vez. Se false, pode ser ativado múltiplas vezes.")]
    [SerializeField] private bool onlyOnce = true;
    
    private bool hasActivated = false;

    private void Start()
    {
        // Garantir que o collider seja um trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"RadioTriggerActivator em {gameObject.name} não possui Collider!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onlyOnce && hasActivated) return;
        
        Debug.Log($"RadioTriggerActivator: Trigger ativado por {other.name} com tag {other.tag}");
        
        if (other.CompareTag("Player"))
        {
            if (isFirstActivation)
            {
                Debug.Log("RadioTriggerActivator: Player detectado! Primeira ativação do rádio...");
                GameEvents.TriggerRadioActivation();
            }
            else
            {
                Debug.Log($"RadioTriggerActivator: Player detectado! Iniciando transmissão {transmissionIndex}...");
                GameEvents.TriggerRadioTransmission(transmissionIndex);
            }
            
            hasActivated = true;
        }
    }
}
