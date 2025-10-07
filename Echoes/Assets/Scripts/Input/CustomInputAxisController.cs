using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System;
using Object = UnityEngine.Object;

/// <summary>
/// Custom Input Axis Controller for Cinemachine with direct sensitivity control
/// Based on Unity Cinemachine documentation for custom controllers
/// Integrates seamlessly with GameSettings for dynamic sensitivity adjustment
/// </summary>
public class CustomInputAxisController : InputAxisControllerBase<CustomInputAxisController.MouseLookReader>
{
    #region Serialized Fields
    
    [Header("Mouse Look Settings")]
    [SerializeField] [Range(0.01f, 3f)] private float mouseSensitivity = 1.0f;
    [SerializeField] private bool invertY = false;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference lookAction;
    
    /// <summary>
    /// Public access to look action for validation
    /// </summary>
    public InputActionReference LookAction => lookAction;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    #endregion
    
    #region Private Fields
    
    private InputAction m_LookAction;
    private Vector2 m_CurrentInput;
    
    #endregion
    
    #region Public Properties
    
    /// <summary>
    /// Mouse sensitivity multiplier (0.01 - 3.0)
    /// </summary>
    public float MouseSensitivity 
    { 
        get => mouseSensitivity; 
        set 
        {
            mouseSensitivity = Mathf.Clamp(value, 0.01f, 3.0f);
            if (debugMode)
                Debug.Log($"[CustomInputAxisController] Sensitivity set to: {mouseSensitivity:F2}");
        }
    }
    
    /// <summary>
    /// Invert Y axis
    /// </summary>
    public bool InvertY 
    { 
        get => invertY; 
        set => invertY = value; 
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (lookAction != null)
        {
            m_LookAction = lookAction.action;
            if (m_LookAction != null)
            {
                m_LookAction.Enable();
                if (debugMode)
                    Debug.Log("[CustomInputAxisController] Input action enabled");
            }
        }
        
        // Register with GameSettings for sensitivity updates
        if (GameSettings.Instance != null)
        {
            MouseSensitivity = GameSettings.Instance.MouseSensitivity;
        }
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        
        if (m_LookAction != null)
        {
            m_LookAction.Disable();
            if (debugMode)
                Debug.Log("[CustomInputAxisController] Input action disabled");
        }
    }
    
    void Update()
    {
        if (Application.isPlaying)
        {
            UpdateControllers();
        }
    }
    
    #endregion
    
    #region GameSettings Integration
    
    /// <summary>
    /// Called by GameSettings to update sensitivity
    /// </summary>
    /// <param name="newSensitivity">New sensitivity value</param>
    public void UpdateSensitivity(float newSensitivity)
    {
        MouseSensitivity = newSensitivity;
        
        if (debugMode)
            Debug.Log($"[CustomInputAxisController] ✅ Sensitivity updated from GameSettings: {newSensitivity:F2}");
    }
    
    #endregion
    
    #region Mouse Look Reader (Inner Class)
    
    /// <summary>
    /// Mouse look input reader - handles both X and Y axis based on context
    /// </summary>
    [Serializable]
    public class MouseLookReader : IInputAxisReader
    {
        /// <summary>
        /// Gets the input value for this axis
        /// The axis is determined by the context (controller index)
        /// </summary>
        public float GetValue(Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            // Get the controller instance
            var controller = context as CustomInputAxisController;
            if (controller == null)
            {
                // Try to find it in the component hierarchy
                if (context is Component comp)
                    controller = comp.GetComponent<CustomInputAxisController>();
                
                if (controller == null)
                    return 0f;
            }
            
            if (controller.m_LookAction == null)
                return 0f;
            
            // Read input from Input System
            Vector2 mouseInput = controller.m_LookAction.ReadValue<Vector2>();
            
            // Apply sensitivity
            mouseInput *= controller.mouseSensitivity;
            
            // Determine which axis this reader represents
            // We need to find our index in the Controllers list
            int axisIndex = -1;
            if (controller.Controllers != null)
            {
                for (int i = 0; i < controller.Controllers.Count; i++)
                {
                    if (controller.Controllers[i] != null && controller.Controllers[i].Input == this)
                    {
                        axisIndex = i;
                        break;
                    }
                }
            }
            
            // If we can't determine the axis, default to 0 (X)
            if (axisIndex == -1) axisIndex = 0;
            
            // Apply Y inversion if needed
            if (controller.invertY && axisIndex == 1)
                mouseInput.y = -mouseInput.y;
            
            // Return the appropriate axis value
            float value = axisIndex == 0 ? mouseInput.x : mouseInput.y;
            
            if (controller.debugMode && Mathf.Abs(value) > 0.1f)
            {
                string axisName = axisIndex == 0 ? "X" : "Y";
                Debug.Log($"[CustomInputAxisController] {axisName} axis: {value:F2} (sensitivity: {controller.mouseSensitivity:F2})");
            }
            
            return value;
        }
        
        /// <summary>
        /// Initialize method for backward compatibility (called by setup script)
        /// </summary>
        public void Initialize(CustomInputAxisController ctrl, int axis)
        {
            // This method is now optional since we auto-detect the axis index
            // Kept for compatibility with the setup script
        }
    }
    
    #endregion
    
    #region Debug Methods
    
    /// <summary>
    /// Debug method to show current state
    /// </summary>
    [ContextMenu("Debug Controller State")]
    public void DebugCurrentState()
    {
        Debug.Log($"[CustomInputAxisController] === STATE DEBUG ===");
        Debug.Log($"  Sensitivity: {mouseSensitivity:F2}");
        Debug.Log($"  Invert Y: {invertY}");
        Debug.Log($"  Input Action: {(m_LookAction != null ? "✅" : "❌")}");
        Debug.Log($"  Controllers Count: {Controllers?.Count ?? 0}");
        
        if (Controllers != null)
        {
            for (int i = 0; i < Controllers.Count; i++)
            {
                string axisName = i == 0 ? "X" : "Y";
                Debug.Log($"    Axis {i} ({axisName}): {(Controllers[i] != null ? "✅" : "❌")}");
            }
        }
    }
    
    /// <summary>
    /// Toggle debug mode
    /// </summary>
    [ContextMenu("Toggle Debug Mode")]
    public void ToggleDebugMode()
    {
        debugMode = !debugMode;
        Debug.Log($"[CustomInputAxisController] Debug mode: {(debugMode ? "ON" : "OFF")}");
    }
    
    #endregion
}