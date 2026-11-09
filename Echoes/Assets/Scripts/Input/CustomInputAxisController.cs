using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

/// <summary>Olhar: deslocamento do mouse e velocidade do analógico têm unidades distintas.</summary>
public class CustomInputAxisController : InputAxisControllerBase<CustomInputAxisController.MouseLookReader>
{
    public const float MouseDegreesPerPixel = 0.08f;
    public const float StickDegreesPerSecond = 120f;

    [Header("Mouse Look Settings")]
    [SerializeField, Range(GameOptions.MinSensitivity, GameOptions.MaxSensitivity)]
    private float mouseSensitivity = 1f;
    [SerializeField] private bool invertY;
    [Header("Input Actions")]
    [SerializeField] private InputActionReference lookAction;

    public InputActionReference LookAction => lookAction;
    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = Mathf.Clamp(value, GameOptions.MinSensitivity, GameOptions.MaxSensitivity);
    }
    public bool InvertY { get => invertY; set => invertY = value; }

    private InputAction m_LookAction;
    private Vector2 m_CurrentInput;
    private CinemachinePanTilt m_PanTilt;
    private bool m_WasAcceptingInput;

    protected override void OnEnable()
    {
        base.OnEnable();
        m_PanTilt = GetComponent<CinemachinePanTilt>();
        // Cada câmera possui sua ação. Desligar uma não pode desabilitar a ação das demais.
        if (lookAction != null && lookAction.action != null)
        {
            m_LookAction = lookAction.action.Clone();
            m_LookAction.Enable();
        }
        ResetInput();
        ApplyOptions();
        GameOptions.Changed += ApplyOptions;
    }

    protected override void OnDisable()
    {
        GameOptions.Changed -= ApplyOptions;
        m_LookAction?.Dispose();
        m_LookAction = null;
        ResetInput();
        base.OnDisable();
    }

    private void ApplyOptions()
    {
        MouseSensitivity = GameOptions.Current.mouseSensitivity;
        InvertY = GameOptions.Current.invertY;
    }

    private void OnApplicationFocus(bool focused) => ResetInput();

    private void ResetInput()
    {
        m_CurrentInput = Vector2.zero;
        m_WasAcceptingInput = false;
        // Remove velocidade residual sem recentralizar ou alterar os ângulos do jogador.
        foreach (var controller in Controllers)
        {
            if (controller == null) continue;
            controller.Driver = new DefaultInputAxisDriver { AccelTime = 0f, DecelTime = 0f };
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        bool accepting = m_LookAction != null && Application.isFocused && Time.timeScale > 0f
            && Cursor.lockState == CursorLockMode.Locked && !PlayerControlLock.IsLocked
            && (m_PanTilt == null || m_PanTilt.isActiveAndEnabled);
        if (!accepting)
        {
            ResetInput();
            return;
        }
        // Descarta o quadro em que o cursor foi preso, recuperou foco ou saiu da inspeção.
        if (!m_WasAcceptingInput)
        {
            m_WasAcceptingInput = true;
            m_CurrentInput = Vector2.zero;
            return;
        }

        float deltaTime = IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        m_CurrentInput = ConvertLookInput(m_LookAction.ReadValue<Vector2>(),
            m_LookAction.activeControl?.device is Pointer, mouseSensitivity, invertY, deltaTime);
        UpdateControllers(deltaTime);
    }

    /// <summary>Graus/segundo para o driver Cinemachine; mouse entra em pixels/quadro.</summary>
    public static Vector2 ConvertLookInput(Vector2 input, bool pointer, float sensitivity, bool inverted, float deltaTime)
    {
        if (deltaTime <= 0f || !float.IsFinite(deltaTime)
            || !float.IsFinite(input.x) || !float.IsFinite(input.y)) return Vector2.zero;
        float gain = Mathf.Clamp(sensitivity, GameOptions.MinSensitivity, GameOptions.MaxSensitivity);
        input *= gain * (pointer ? MouseDegreesPerPixel / deltaTime : StickDegreesPerSecond);
        // Tilt positivo gira para baixo. Sem inversão, mover para cima deve olhar para cima.
        input.y *= inverted ? 1f : -1f;
        return input;
    }

    public void UpdateSensitivity(float newSensitivity) => MouseSensitivity = newSensitivity;

    [Serializable]
    public class MouseLookReader : IInputAxisReader
    {
        public float GetValue(Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            var controller = context as CustomInputAxisController;
            if (controller == null && context is Component component)
                controller = component.GetComponent<CustomInputAxisController>();
            if (controller == null) return 0f;
            // Usa o eixo real, sem depender da ordem ou quantidade de câmeras na lista.
            return hint == IInputAxisOwner.AxisDescriptor.Hints.Y
                ? controller.m_CurrentInput.y : controller.m_CurrentInput.x;
        }

        public void Initialize(CustomInputAxisController controller, int axis) { }
    }
}
