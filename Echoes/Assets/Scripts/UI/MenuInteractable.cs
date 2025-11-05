using UnityEngine;
using System.Collections;
using FMODUnity;
using UnityEngine.Localization;

/// <summary>
/// Torna o objeto físico do menu interativo durante o gameplay
/// Permite ao jogador retornar ao menu interagindo com o objeto na cena
/// Executa rotação reversa e restaura o estado de menu
/// </summary>
public class MenuInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LocalizedString interactionPrompt = new LocalizedString("UI", "AccessMenu");
    
    [Header("Menu Manager Reference")]
    [SerializeField] private MenuUIManager menuUIManager;
    
    [Header("Rotation Settings")]
    [SerializeField] private Transform objectToRotate;
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private Vector3 gameplayRotation = new Vector3(90f, -90f, 90f); // endRotation do MenuUIManager
    [SerializeField] private Vector3 menuRotation = new Vector3(73f, -90f, 90f);     // startRotation do MenuUIManager
    [SerializeField] private bool useLocalRotation = true;
    
    [Header("Audio Events")]
    [SerializeField] private EventReference menuInteractionSound;
    
    // Control variables
    private bool isRotating = false;
    private bool canInteract = true;
    
    #region IInteractable Implementation
    
    public string InteractionPrompt 
    { 
        get 
        {
            // Only show prompt if menu manager exists, game has started, and not currently rotating
            if (menuUIManager == null || !menuUIManager.IsGameStarted || isRotating)
                return string.Empty;
                
            try
            {
                return interactionPrompt.GetLocalizedString();
            }
            catch
            {
                return "Acessar Menu"; // Fallback text
            }
        } 
    }
    
    public float InteractionDistance => interactionDistance;
    
    public bool Interact(Transform interactor)
    {
        // Only allow interaction if game has started and not currently in a transition
        if (!CanInteract()) return false;
        
        // Play menu interaction sound
        PlayMenuSound();
        
        // Start the return to menu process
        StartCoroutine(ReturnToMenuSequence());
        
        return true;
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    void Start()
    {
        ValidateReferences();
        InitializeRotationSettings();
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Validates all required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (menuUIManager == null)
        {
            menuUIManager = FindFirstObjectByType<MenuUIManager>();
            if (menuUIManager == null)
            {
                Debug.LogError($"MenuUIManager não encontrado! Atribua a referência em {gameObject.name}", this);
            }
        }
        
        if (objectToRotate == null)
        {
            objectToRotate = transform;
            Debug.LogWarning($"objectToRotate não atribuído em {gameObject.name}, usando o próprio transform", this);
        }
    }
    
    /// <summary>
    /// Initializes rotation settings from MenuUIManager if available
    /// </summary>
    private void InitializeRotationSettings()
    {
        // Sync rotation settings with MenuUIManager to ensure they're exactly opposite
        if (menuUIManager != null)
        {
            gameplayRotation = menuUIManager.GetGameplayRotation();
            menuRotation = menuUIManager.GetMenuRotation();
            useLocalRotation = menuUIManager.GetUseLocalRotation();
            rotationDuration = menuUIManager.GetRotationDuration();
            
            Debug.Log($"MenuInteractable synced with MenuUIManager - Menu: {menuRotation}, Gameplay: {gameplayRotation}");
        }
    }
    
    /// <summary>
    /// Checks if interaction is currently allowed
    /// </summary>
    private bool CanInteract()
    {
        return canInteract && 
               !isRotating && 
               menuUIManager != null && 
               !menuUIManager.IsInMainMenu && 
               !menuUIManager.IsTransitioning;
    }
    
    /// <summary>
    /// Executes the complete sequence to return to menu
    /// </summary>
    private IEnumerator ReturnToMenuSequence()
    {
        Debug.Log("=== MenuInteractable: ReturnToMenuSequence STARTED ===");
        isRotating = true;
        canInteract = false;
        
        // TESTE: Trigger menu UI manager FIRST (like the initial menu does)
        Debug.Log("MenuInteractable: Calling MenuUIManager.ReturnToMenuFromGameplay() FIRST...");
        if (menuUIManager != null)
        {
            menuUIManager.ReturnToMenuFromGameplay();
        }
        else
        {
            Debug.LogError("MenuInteractable: MenuUIManager is null!");
        }
        
        // Wait one frame for camera transitions to start
        yield return null;
        
        // Step 2: Then rotate the object back to menu position
        Debug.Log("MenuInteractable: Starting object rotation AFTER menu transition...");
        yield return StartCoroutine(RotateObjectToMenu());
        Debug.Log("MenuInteractable: Object rotation completed.");
        
        isRotating = false;
        // Keep canInteract false while in menu - will be re-enabled when game starts again
        Debug.Log("=== MenuInteractable: ReturnToMenuSequence COMPLETED ===");
    }
    
    /// <summary>
    /// Rotates the object from gameplay position back to menu position
    /// </summary>
    private IEnumerator RotateObjectToMenu()
    {
        if (objectToRotate == null) 
        {
            Debug.LogWarning("Object to rotate is not assigned!");
            yield break;
        }
        
        Debug.Log($"Starting rotation from gameplay ({gameplayRotation}) to menu ({menuRotation})");
        
        // Get current rotation (should be gameplay rotation)
        Vector3 startRotation = useLocalRotation ? 
            objectToRotate.localEulerAngles : 
            objectToRotate.eulerAngles;
        
        // Ensure we're starting from the expected gameplay rotation
        // (This handles cases where the object might have been moved)
        if (useLocalRotation)
        {
            objectToRotate.localRotation = Quaternion.Euler(gameplayRotation);
        }
        else
        {
            objectToRotate.rotation = Quaternion.Euler(gameplayRotation);
        }
        
        float elapsedTime = 0f;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time in case timeScale is modified
            float progress = elapsedTime / rotationDuration;
            
            // Smooth rotation curve
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 currentRotation = Vector3.Lerp(gameplayRotation, menuRotation, smoothProgress);
            
            if (useLocalRotation)
            {
                objectToRotate.localRotation = Quaternion.Euler(currentRotation);
            }
            else
            {
                objectToRotate.rotation = Quaternion.Euler(currentRotation);
            }
            
            yield return null;
        }
        
        // Ensure final rotation is exact
        if (useLocalRotation)
        {
            objectToRotate.localRotation = Quaternion.Euler(menuRotation);
        }
        else
        {
            objectToRotate.rotation = Quaternion.Euler(menuRotation);
        }
        
        Debug.Log($"Rotation completed. Final rotation: {menuRotation}");
    }
    
    /// <summary>
    /// Plays menu interaction sound via FMOD
    /// </summary>
    private void PlayMenuSound()
    {
        if (menuInteractionSound.IsNull) return;
        RuntimeManager.PlayOneShot(menuInteractionSound);
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Re-enables interaction when game starts (called externally if needed)
    /// </summary>
    public void EnableInteraction()
    {
        canInteract = true;
    }
    
    /// <summary>
    /// Disables interaction (called externally if needed)
    /// </summary>
    public void DisableInteraction()
    {
        canInteract = false;
    }
    
    /// <summary>
    /// Gets current interaction state
    /// </summary>
    public bool IsInteractionEnabled => canInteract && !isRotating;
    
    /// <summary>
    /// Gets current rotation state
    /// </summary>
    public bool IsRotating => isRotating;
    
    #endregion
    
    #region Editor Helpers
    
    #if UNITY_EDITOR
    
    /// <summary>
    /// Draws interaction range in Scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw rotation object if assigned
        if (objectToRotate != null && objectToRotate != transform)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(objectToRotate.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, objectToRotate.position);
        }
    }
    
    #endif
    
    #endregion
}