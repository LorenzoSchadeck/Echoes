using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory")]
    [Tooltip("Quantidade de remédios que o jogador possui atualmente.")]
    [SerializeField] private int remedyCount = 0;
    private PlayerInputActions inputActions;

    public int RemedyCount => remedyCount;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Remedy.performed += OnUseRemedy;
        inputActions.Player.Remedy.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Remedy.performed -= OnUseRemedy;
        inputActions.Player.Remedy.Disable();
    }

    /// <summary>
    /// Adiciona uma quantidade de remédios ao inventário do jogador.
    /// </summary>
    public void AddRemedies(int amount)
    {
        remedyCount += amount;
        Debug.Log($"Adicionado {amount} remédio(s). Total: {remedyCount}");
        // atualizar a UI
    }

    /// <summary>
    /// Função chamada quando a ação "UseRemedy" do Input System é disparada.
    /// </summary>
    private void OnUseRemedy(InputAction.CallbackContext context)
    {
        if (remedyCount > 0)
        {
            remedyCount--;
            Debug.Log($"Remédio usado! Restam: {remedyCount}");
            
            GameEvents.TriggerRemedyUsed();
            
            // atualizar a UI
        }
        else
        {
            Debug.Log("Sem remédios para usar!");
        }
    }
}