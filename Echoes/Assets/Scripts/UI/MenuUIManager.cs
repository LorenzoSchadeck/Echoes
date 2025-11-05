using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using System.Collections;
using FMODUnity;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerenciador principal da interface do menu
/// Conecta os botões da UI com o CustomInputAxisController e outros sistemas
/// Integrado com GameSettings para controle de volume e sensibilidade
/// </summary>
public class MenuUIManager : MonoBehaviour
{
    
    [Header("Menu Camera")]
    [SerializeField] private MenuCameraController cameraController;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera menuCamera;
    [SerializeField] private CinemachineCamera playerCamera;
    
    [Header("Player Camera Control")]
    [SerializeField] private CustomInputAxisController customInputAxisController; // Custom Input Axis Controller
    
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
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    
    [Header("Options Menu")]
    [SerializeField] private Button backButton;
    
    [Header("Settings Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;
    
    [Header("Settings Labels")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI mouseSensitivityLabel;
    
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
        SetupSettingsSliders();
        ShowMainMenu();
        InitializeMenuState();
        InitializeGameSettings();
        
        // Initialize button states
        UpdateButtonStates();
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
            
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
            
        // Options menu back button
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }
    
    /// <summary>
    /// Sets up settings sliders events and initial values
    /// </summary>
    private void SetupSettingsSliders()
    {
        // Master Volume Slider
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        // Mouse Sensitivity Slider  
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.01f;
            mouseSensitivitySlider.maxValue = 3f;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }
    }
    

    
    /// <summary>
    /// Initializes GameSettings and applies current values to sliders
    /// </summary>
    private void InitializeGameSettings()
    {
        // Ensure GameSettings exists
        if (GameSettings.Instance == null)
        {
            // Create GameSettings if it doesn't exist
            GameObject settingsObj = new GameObject("GameSettings");
            settingsObj.AddComponent<GameSettings>();
        }
        
        // Pass customInputAxisController reference to GameSettings
        if (GameSettings.Instance != null && customInputAxisController != null)
        {
            GameSettings.Instance.SetCustomInputAxisController(customInputAxisController);
        }
        
        // Update sliders with current settings values
        UpdateSlidersFromSettings();
    }
    
    /// <summary>
    /// Initializes menu state - disables player movement, crosshair and player camera
    /// </summary>
    private void InitializeMenuState()
    {
        // Disable player movement while in menu
        PlayerMovement.canMove = false;

        // Set camera priorities - menu active, player inactive
        if (menuCamera != null) 
        {
            menuCamera.Priority = 15; // High priority for menu
        }
        if (playerCamera != null) 
        {
            playerCamera.Priority = -1; // Low priority when in menu
        }
        
        // Disable custom input axis controller (mouse look) during menu
        if (customInputAxisController != null) 
        {
            customInputAxisController.enabled = false;
        }
        
        // Disable crosshair during menu
        if (crosshairObject != null) crosshairObject.SetActive(false);
        
        // Hide cursor and unlock it for menu navigation
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause the game after setting up cameras (allows Cinemachine transitions to work)
        StartCoroutine(PauseGameAfterFrame()); // UNCOMMENTED - with proper transition handling
    }
    
    /// <summary>
    /// Pauses the game after one frame to allow Cinemachine transitions
    /// </summary>
    private IEnumerator PauseGameAfterFrame()
    {
        Debug.Log("PauseGameAfterFrame: Waiting one frame...");
        yield return null; // Wait one frame for camera transitions to start
        Debug.Log($"PauseGameAfterFrame: Pausing game. Current timeScale: {Time.timeScale}");
        Time.timeScale = 0f; // UNCOMMENTED - restore pause functionality
        Debug.Log($"PauseGameAfterFrame: Game paused. New timeScale: {Time.timeScale}");
    }
    
    /// <summary>
    /// Forces Cinemachine Brain to update camera priorities
    /// </summary>
    private void ForceCinemachineBrainUpdate()
    {
        // Find Cinemachine Brain in scene
        var brain = FindFirstObjectByType<Unity.Cinemachine.CinemachineBrain>();
        if (brain != null)
        {
            Debug.Log($"Found CinemachineBrain: {brain.name} - Enabled: {brain.enabled}");
            Debug.Log($"Brain Default Blend: {brain.DefaultBlend.Style} - Time: {brain.DefaultBlend.BlendTime}");
            
            // REMOVED: Don't disable/enable brain as it breaks smooth transitions
            // Just log the state for debugging
            Debug.Log("Brain found and should handle transitions automatically");
        }
        else
        {
            Debug.LogError("No CinemachineBrain found in scene!");
        }
    }
    
    #endregion
    
    #region UI Event Handlers
    
    /// <summary>
    /// Handles click on "Start" button - Transitions from menu to game OR restarts game if already started
    /// </summary>
    private void OnStartClicked()
    {
        if (isTransitioning) return;
        
        PlayButtonClickSound();
        
        // Check if game has already started (pause mode)
        if (gameStarted)
        {
            Debug.Log("MenuUIManager: Start button clicked in pause mode - RESTARTING SCENE");
            RestartScene();
        }
        else
        {
            Debug.Log("MenuUIManager: Start button clicked in initial menu - STARTING GAME");
            // Start transition to game camera
            TransitionToGame();
        }
    }
    
    /// <summary>
    /// Handles click on "Continue" button - Continues from pause
    /// </summary>
    private void OnContinueClicked()
    {
        if (isTransitioning) return;
        
        PlayButtonClickSound();
        
        // Continue from pause - transition back to game
        TransitionToGame();
    }
    
    /// <summary>
    /// Reinicia a cena atual do zero
    /// </summary>
    private void RestartScene()
    {
        Debug.Log("=== RestartScene STARTED ===");
        
        // Reset game state variables
        gameStarted = false;
        isInMainMenu = true;
        isTransitioning = false;
        
        // Reset static variables that don't automatically reset
        PlayerMovement.canMove = true;
        
        // Force cleanup of persistent singletons that use DontDestroyOnLoad
        CleanupPersistentSingletons();
        
        // Ensure time scale is normal before reloading
        Time.timeScale = 1f;
        
        // Reload current scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Restarting scene: {currentSceneName}");
        
        SceneManager.LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Limpa singletons persistentes que usam DontDestroyOnLoad para evitar referências quebradas
    /// </summary>
    private void CleanupPersistentSingletons()
    {
        Debug.Log("RestartScene: Starting cleanup of persistent singletons...");
        
        // STEP 1: Stop all FMOD audio immediately
        StopAllFMODAudio();
        
        // STEP 1.5: Force cleanup of any lingering FMOD event instances
        ForceCleanupFMODInstances();
        
        // STEP 2: Cleanup GameSettings if it exists
        if (GameSettings.Instance != null)
        {
            Debug.Log("RestartScene: Destroying persistent GameSettings instance");
            // Remove DontDestroyOnLoad status and destroy
            GameSettings.Instance.transform.SetParent(null);
            Destroy(GameSettings.Instance.gameObject);
            
            // Force clear static reference through reflection
            ClearSingletonReference<GameSettings>("Instance");
        }
        
        // STEP 3: Cleanup DeformationManager if it exists (also uses DontDestroyOnLoad)
        var deformationManagerType = System.Type.GetType("Echoes.Deformation.DeformationManager");
        if (deformationManagerType != null)
        {
            var instanceProperty = deformationManagerType.GetProperty("Instance", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProperty != null)
            {
                var deformationInstance = instanceProperty.GetValue(null) as MonoBehaviour;
                if (deformationInstance != null)
                {
                    Debug.Log("RestartScene: Destroying persistent DeformationManager instance");
                    deformationInstance.transform.SetParent(null);
                    Destroy(deformationInstance.gameObject);
                    
                    // Clear static reference
                    if (instanceProperty.CanWrite)
                    {
                        instanceProperty.SetValue(null, null);
                    }
                }
            }
        }
        
        // STEP 4: Reset other static variables that might persist
        ResetStaticVariables();
        
        Debug.Log("RestartScene: Persistent singletons cleanup completed");
    }
    
    /// <summary>
    /// Para todos os áudios FMOD ativos para evitar sons persistentes após reset
    /// </summary>
    private void StopAllFMODAudio()
    {
        try
        {
            Debug.Log("RestartScene: Stopping all FMOD audio...");
            
            // Reset global parameters first
            RuntimeManager.StudioSystem.setParameterByName("Sanity", 1f);
            
            // Get and stop master bus
            FMOD.Studio.Bus masterBus;
            var result = RuntimeManager.StudioSystem.getBus("bus:/", out masterBus);
            if (result == FMOD.RESULT.OK)
            {
                masterBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
            
            // Stop all events in the system - using proper FMOD API
            RuntimeManager.StudioSystem.flushCommands();
            
            // Additionally, stop any one-shot events that might be playing
            // Note: FMOD Unity automatically manages most event cleanup
            
            Debug.Log("RestartScene: FMOD audio stopped successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"RestartScene: Error stopping FMOD audio: {e.Message}");
        }
    }
    
    /// <summary>
    /// Força limpeza adicional de instâncias FMOD que podem persistir
    /// </summary>
    private void ForceCleanupFMODInstances()
    {
        try
        {
            Debug.Log("RestartScene: Force cleaning FMOD instances...");
            
            // Stop all event instances that might be tracked by GameEvents
            // This ensures events triggered by other scripts are also stopped
            if (RuntimeManager.IsInitialized)
            {
                // Flush any pending commands
                RuntimeManager.StudioSystem.flushCommands();
                
                // Force update to process stop commands
                RuntimeManager.StudioSystem.update();
                
                Debug.Log("RestartScene: FMOD instances force cleanup completed");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"RestartScene: Error in force FMOD cleanup: {e.Message}");
        }
    }
    
    /// <summary>
    /// Reseta variáveis estáticas que podem persistir entre resets de cena
    /// </summary>
    private void ResetStaticVariables()
    {
        Debug.Log("RestartScene: Resetting static variables...");
        
        // Reset PlayerMovement static variable
        PlayerMovement.canMove = true;
        
        // Add other static variable resets here if needed
        // Example: SomeClass.staticVariable = defaultValue;
        
        Debug.Log("RestartScene: Static variables reset completed");
    }
    
    /// <summary>
    /// Método genérico para limpar referências de singleton via reflection
    /// </summary>
    private void ClearSingletonReference<T>(string propertyName) where T : class
    {
        try
        {
            var instanceProperty = typeof(T).GetProperty(propertyName, 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (instanceProperty != null && instanceProperty.CanWrite)
            {
                instanceProperty.SetValue(null, null);
                Debug.Log($"RestartScene: Cleared {typeof(T).Name}.{propertyName} static reference");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"RestartScene: Failed to clear {typeof(T).Name}.{propertyName}: {e.Message}");
        }
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
        ExitGameAfterDelay();
    }
    
    /// <summary>
    /// Exits game after a small delay to let audio play
    /// </summary>
    public void ExitGameAfterDelay()
    {
        Application.Quit();
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
        isInMainMenu = false; // CORREÇÃO: Player saiu do menu principal
        
        // Resume the game when transitioning to gameplay
        Time.timeScale = 1f; // UNCOMMENTED - need to unpause for gameplay
        
        // Start game initialization rotation
        StartCoroutine(RotateObjectOnGameStart());
        
        // Start camera transition by changing priorities
        if (menuCamera != null) 
        {
            menuCamera.Priority = -1; // Low priority during gameplay
        }
        if (playerCamera != null) 
        {
            playerCamera.Priority = 10; // High priority for gameplay
        }
        
        // Enable custom input axis controller (mouse look) for gameplay
        if (customInputAxisController != null) 
        {
            customInputAxisController.enabled = true;
            
            // Apply current sensitivity settings when enabling the controller
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.ApplyMouseSensitivity();
            }
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
        
        // Re-enable MenuInteractable for future interactions
        EnableMenuInteractable();
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
        
        // Re-enable MenuInteractable for future interactions
        EnableMenuInteractable();
    }
    
    /// <summary>
    /// Re-enables MenuInteractable objects in the scene
    /// </summary>
    private void EnableMenuInteractable()
    {
        var menuInteractables = FindObjectsByType<MenuInteractable>(FindObjectsSortMode.None);
        foreach (var interactable in menuInteractables)
        {
            interactable.EnableInteraction();
        }
        Debug.Log($"Re-enabled {menuInteractables.Length} MenuInteractable objects");
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
        
        // CORREÇÃO: Sliders sempre visíveis - não esconde no menu principal
        ShowSettingsSliders(true);
        
        // Update button states based on game status
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Atualiza o estado dos botões baseado no estado atual do jogo
    /// </summary>
    private void UpdateButtonStates()
    {
        // Continue button: only enabled when game has started (pause mode)
        if (continueButton != null)
        {
            continueButton.interactable = gameStarted;
            Debug.Log($"MenuUIManager: Continue button set to {(gameStarted ? "ENABLED" : "DISABLED")} (gameStarted: {gameStarted})");
        }
        
        // Start button: always enabled
        if (startButton != null)
        {
            startButton.interactable = true;
        }
    }
    
    /// <summary>
    /// Switches to options menu state
    /// </summary>
    private void ShowOptionsMenu()
    {
        isInMainMenu = false;
        
        // Show back button when in options menu
        if (backButton != null) backButton.gameObject.SetActive(true);
        
        // Show settings sliders in options menu
        ShowSettingsSliders(true);
        
        // Update sliders with current values
        UpdateSlidersFromSettings();
    }
    
    #endregion
    
    #region Settings Management
    
    /// <summary>
    /// Updates sliders values from current GameSettings
    /// </summary>
    private void UpdateSlidersFromSettings()
    {
        if (GameSettings.Instance == null) return;
        
        // Update master volume slider
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(GameSettings.Instance.MasterVolume);
        }
        
        // Update mouse sensitivity slider
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(GameSettings.Instance.MouseSensitivity);
        }
        
        // Update labels
        UpdateSettingsLabels();
    }
    
    /// <summary>
    /// Updates settings labels with current values
    /// </summary>
    private void UpdateSettingsLabels()
    {
        UpdateSettingsLabelsWithoutLocalization();
    }
    
    /// <summary>
    /// Fallback method to update settings labels without localization
    /// </summary>
    private void UpdateSettingsLabelsWithoutLocalization()
    {
        if (GameSettings.Instance == null) return;
        
        // Update master volume label
        if (masterVolumeLabel != null)
        {
            int volumePercentage = GameSettings.Instance.GetMasterVolumePercentage();
            masterVolumeLabel.text = $"{volumePercentage}%";
        }
        
        // Update mouse sensitivity label
        if (mouseSensitivityLabel != null)
        {
            int sensitivityPercentage = GameSettings.Instance.GetMouseSensitivityPercentage();
            mouseSensitivityLabel.text = $"{sensitivityPercentage}%";
        }
    }
    

    
    /// <summary>
    /// Called when master volume slider value changes
    /// </summary>
    /// <param name="value">New volume value (0.0 - 1.0)</param>
    private void OnMasterVolumeChanged(float value)
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetMasterVolume(value);
            UpdateSettingsLabels();
        }
    }
    
    /// <summary>
    /// Called when mouse sensitivity slider value changes
    /// </summary>
    /// <param name="value">New sensitivity value (0.01 - 3.0)</param>
    private void OnMouseSensitivityChanged(float value)
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetMouseSensitivity(value);
            UpdateSettingsLabels();
        }
    }
    
    /// <summary>
    /// Shows or hides settings sliders and labels
    /// </summary>
    /// <param name="show">True to show, false to hide</param>
    private void ShowSettingsSliders(bool show)
    {
        // Show/hide volume slider and label
        if (masterVolumeSlider != null) 
            masterVolumeSlider.gameObject.SetActive(show);
        if (masterVolumeLabel != null) 
            masterVolumeLabel.gameObject.SetActive(show);
            
        // Show/hide sensitivity slider and label
        if (mouseSensitivitySlider != null) 
            mouseSensitivitySlider.gameObject.SetActive(show);
        if (mouseSensitivityLabel != null) 
            mouseSensitivityLabel.gameObject.SetActive(show);
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
    
    /// <summary>
    /// Returns to menu from gameplay (for interaction with menu object during game)
    /// </summary>
    public void ReturnToMenuFromGameplay()
    {
        // FIXED: Allow return to menu if we're not already in menu (removed gameStarted check)
        if (isInMainMenu || isTransitioning) return;
        
        Debug.Log("=== ReturnToMenuFromGameplay STARTED ===");
        Debug.Log($"Current timeScale: {Time.timeScale}");
        Debug.Log($"MenuCamera: {(menuCamera != null ? menuCamera.name : "NULL")} - Position: {(menuCamera != null ? menuCamera.transform.position.ToString() : "NULL")}");
        Debug.Log($"PlayerCamera: {(playerCamera != null ? playerCamera.name : "NULL")} - Position: {(playerCamera != null ? playerCamera.transform.position.ToString() : "NULL")}");
        Debug.Log($"Current menuCamera priority: {(menuCamera != null ? menuCamera.Priority.Value.ToString() : "null")}");
        Debug.Log($"Current playerCamera priority: {(playerCamera != null ? playerCamera.Priority.Value.ToString() : "null")}");
        
        PlayButtonClickSound();
        
        // Mark as transitioning and return to menu state
        isTransitioning = true;
        isInMainMenu = true;
        // CORREÇÃO: Manter gameStarted = true no modo pause para habilitar botão Continue
        // gameStarted = false; // REMOVED - não resetar no pause, apenas no restart da cena
        
        // Disable player movement immediately
        PlayerMovement.canMove = false;
        
        // Switch camera priorities back to menu IMMEDIATELY (like TransitionToGame does)
        if (menuCamera != null) 
        {
            menuCamera.Priority = 15; // High priority for menu
            Debug.Log($"Menu camera priority changed to: {menuCamera.Priority.Value}");
        }
        if (playerCamera != null) 
        {
            playerCamera.Priority = -1; // Low priority when returning to menu
            Debug.Log($"Player camera priority changed to: {playerCamera.Priority.Value}");
        }
        
        // Force Cinemachine Brain update
        ForceCinemachineBrainUpdate();
        
        // Disable custom input axis controller (mouse look) immediately
        if (customInputAxisController != null) 
        {
            customInputAxisController.enabled = false;
        }
        
        // Disable crosshair
        if (crosshairObject != null) crosshairObject.SetActive(false);
        
        // Setup menu cursor state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Show main menu UI
        ShowMainMenu();
        
        // Go back to main menu camera position
        if (cameraController != null)
            cameraController.GoBackToMainMenu();
        
        // Pause the game after one frame (same as InitializeMenuState)
        StartCoroutine(PauseGameAfterFrame()); // UNCOMMENTED - need to pause in menu
        
        isTransitioning = false;
        
        // Update button states after returning to menu (pause mode)
        UpdateButtonStates();
        
        Debug.Log("=== ReturnToMenuFromGameplay COMPLETED ===");
    }
    
    /// <summary>
    
    /// <summary>
    /// Resets all settings to default values (for Reset button)
    /// </summary>
    public void ResetSettingsToDefaults()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ResetToDefaults();
            UpdateSlidersFromSettings();
        }
    }
    
    /// <summary>
    /// Gets the start rotation used for menu position
    /// </summary>
    public Vector3 GetMenuRotation() => startRotation;
    
    /// <summary>
    /// Gets the end rotation used for gameplay position
    /// </summary>
    public Vector3 GetGameplayRotation() => endRotation;
    
    /// <summary>
    /// Gets whether local rotation is used
    /// </summary>
    public bool GetUseLocalRotation() => useLocalRotation;
    
    /// <summary>
    /// Gets the rotation duration
    /// </summary>
    public float GetRotationDuration() => rotationDuration;
    
    /// <summary>
    /// Reinicia a cena atual (para uso externo)
    /// </summary>
    public void RestartGame()
    {
        RestartScene();
    }
    
    #endregion
}