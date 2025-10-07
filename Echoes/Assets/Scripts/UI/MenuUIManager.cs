using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;
using FMODUnity;

/// <summary>
/// Gerenciador principal da interface do menu
/// Conecta os botões da UI com o controlador da câmera e outros sistemas
/// </summary>
public class MenuUIManager : MonoBehaviour
{
    [Header("Menu Camera")]
    [SerializeField] private MenuCameraController cameraController;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera menuCamera;
    [SerializeField] private CinemachineCamera playerCamera;
    
    [Header("Player Camera Control")]
    [SerializeField] private CinemachineInputAxisController inputAxisController; // Input Axis Controller do Cinemachine
    
    [Header("Game Start Rotation")]
    [SerializeField] private Transform objectToRotate;
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private Vector3 startRotation = new Vector3(73f, -90f, 90f);
    [SerializeField] private Vector3 endRotation = new Vector3(90f, -90f, 90f);
    [SerializeField] private bool useLocalRotation = true;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject crosshairObject;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    
    [Header("Options Menu")]
    [SerializeField] private Button backButton;
    
    [Header("Audio Events")]
    [SerializeField] private EventReference clickEvent;
    [SerializeField] private EventReference rotationStartEvent;
    
    // Menu and game states
    private bool isInMainMenu = true;
    private bool isTransitioning = false;
    private bool gameStarted = false;
    
    #region Unity Lifecycle
    
    void Start()
    {
        SetupButtonEvents();
        ShowMainMenu();
        InitializeMenuState();
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Sets up button events
    /// </summary>
    private void SetupButtonEvents()
    {
        // Main menu buttons
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
            
        // Options menu back button
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }
    
    /// <summary>
    /// Initializes menu state - disables player movement, crosshair and player camera
    /// </summary>
    private void InitializeMenuState()
    {
        // Disable player movement while in menu
        PlayerMovement.canMove = false;

        // Set camera priorities - menu active, player inactive
        if (menuCamera != null) menuCamera.Priority = 2;
        if (playerCamera != null) 
        {
            playerCamera.Priority = 0;
            // Disable player camera to prevent mouse look during menu
            playerCamera.enabled = false;
        }
        
        // Disable input axis controller (mouse look) during menu
        if (inputAxisController != null) 
        {
            inputAxisController.enabled = false;
        }
        
        // Disable crosshair during menu
        if (crosshairObject != null) crosshairObject.SetActive(false);
        
        // Hide cursor and unlock it for menu navigation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    #endregion
    
    #region UI Event Handlers
    
    /// <summary>
    /// Handles click on "Start" button - Transitions from menu to game
    /// </summary>
    private void OnStartClicked()
    {
        if (isTransitioning || gameStarted) return;
        
        PlayButtonClickSound();
        
    // Start transition to game camera
    TransitionToGame();
    }
    
    /// <summary>
    /// Handles click on "Options" button
    /// </summary>
    private void OnOptionsClicked()
    {
        PlayButtonClickSound();
        
        if (cameraController != null)
            cameraController.GoToOptions();
            
        ShowOptionsMenu();
    }
    
    /// <summary>
    /// Handles click on "Exit" button - Closes the game
    /// </summary>
    private void OnExitClicked()
    {
        PlayButtonClickSound();
        
        // Small delay to let sound play before closing
        StartCoroutine(ExitGameAfterDelay());
    }
    
    /// <summary>
    /// Exits game after a small delay to let audio play
    /// </summary>
    private IEnumerator ExitGameAfterDelay()
    {
        yield return new WaitForSeconds(0.2f); // Small delay for audio feedback
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Handles click on "Back" button (from options menu)
    /// </summary>
    private void OnBackClicked()
    {
        PlayButtonClickSound();
        
        if (cameraController != null)
            cameraController.GoBackToMainMenu();
            
        ShowMainMenu();
    }
    
    #endregion
    
    #region Game Transition
    
    /// <summary>
    /// Transitions from menu camera to game camera and enables player movement
    /// </summary>
    private void TransitionToGame()
    {
        isTransitioning = true;
        
        // Start game initialization rotation
        StartCoroutine(RotateObjectOnGameStart());
        
        // Start camera transition by changing priorities
        if (menuCamera != null) menuCamera.Priority = 0;
        if (playerCamera != null) 
        {
            playerCamera.Priority = 1;
            // Enable player camera for mouse look
            playerCamera.enabled = true;
        }
        
        // Enable input axis controller (mouse look) for gameplay
        if (inputAxisController != null) 
        {
            inputAxisController.enabled = true;
        }
        
        // Enable crosshair for gameplay
        if (crosshairObject != null) crosshairObject.SetActive(true);
        
        // Enable player movement immediately (Cinemachine Brain faz o blend)
        PlayerMovement.canMove = true;
        gameStarted = true;
        // Setup game cursor state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isTransitioning = false;
    }
    
    /// <summary>
    /// Rotates an object using configurable rotation values when game starts
    /// </summary>
    private IEnumerator RotateObjectOnGameStart()
    {
        if (objectToRotate == null) 
        {
            Debug.LogWarning("Object to rotate is not assigned!");
            yield break;
        }
        
        // Play rotation start sound
        if (!rotationStartEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(rotationStartEvent);
        }
        
        // Set initial rotation (considering if it's a child object)
        if (useLocalRotation)
        {
            objectToRotate.localRotation = Quaternion.Euler(startRotation);
        }
        else
        {
            objectToRotate.rotation = Quaternion.Euler(startRotation);
        }
        
        float elapsedTime = 0f;
        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / rotationDuration;
            
            Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, progress);
            
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
            objectToRotate.localRotation = Quaternion.Euler(endRotation);
        }
        else
        {
            objectToRotate.rotation = Quaternion.Euler(endRotation);
        }
    }
    
    #endregion
    
    #region Menu State Management
    
    /// <summary>
    /// Switches to main menu state
    /// </summary>
    private void ShowMainMenu()
    {
        isInMainMenu = true;
        
        // Hide back button when in main menu
        if (backButton != null) backButton.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Switches to options menu state
    /// </summary>
    private void ShowOptionsMenu()
    {
        isInMainMenu = false;
        
        // Show back button when in options menu
        if (backButton != null) backButton.gameObject.SetActive(true);
    }
    
    #endregion
    
    #region Audio Methods
    
    /// <summary>
    /// Plays button click sound via FMOD
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (clickEvent.IsNull) return;
        RuntimeManager.PlayOneShot(clickEvent);
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Checks if currently in main menu
    /// </summary>
    public bool IsInMainMenu => isInMainMenu;
    
    /// <summary>
    /// Checks if game has started (transitioned from menu)
    /// </summary>
    public bool IsGameStarted => gameStarted;
    
    /// <summary>
    /// Checks if currently transitioning between menu and game
    /// </summary>
    public bool IsTransitioning => isTransitioning;
    
    /// <summary>
    /// Forces return to main menu (for external use)
    /// </summary>
    public void ForceReturnToMainMenu()
    {
        OnBackClicked();
    }
    
    #endregion
}