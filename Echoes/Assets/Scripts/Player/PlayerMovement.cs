using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;

/// <summary>
/// Sistema de movimento do jogador com footsteps dinâmicos baseados em superfície.
/// Integra detecção de superfície para variar sons de passos via FMOD.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimentação")]
    public float moveSpeed = 5f;
    public float groundCheckDistance = 0.6f;
    public LayerMask groundLayer;
    public Transform cameraTransform;

    [Header("Sons de Passo FMOD")]
    [SerializeField] private EventReference footstepEvent;
    [SerializeField] private float timeBetweenSteps = 0.5f;
    [SerializeField] private string surfaceParameterName = "surface";

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private FMODAudioTrigger audioTrigger;
    private FootstepSurfaceSystem surfaceSystem;

    private float footstepTimer;

    public static bool canMove = true;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody>();
        audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
        
        // Adiciona o sistema de superfície se não existir
        surfaceSystem = GetComponent<FootstepSurfaceSystem>();
        if (surfaceSystem == null)
        {
            surfaceSystem = gameObject.AddComponent<FootstepSurfaceSystem>();
        }

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    void OnEnable()
    {
        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleFootsteps();
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        if (!canMove)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x);
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
    }

    void HandleFootsteps()
    {
        if (!canMove) return;

        // Verifica se o jogador está se movendo (temporariamente ignorando ground check)
        if (moveInput.magnitude > 0.1f)
        {
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= timeBetweenSteps)
            {
                footstepTimer = 0f;
                PlayFootstepSound();
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void PlayFootstepSound()
    {
        if (footstepEvent.IsNull || audioTrigger == null) return;

        // Detecta a superfície atual
        if (surfaceSystem != null)
        {
            bool detected = surfaceSystem.DetectSurface();
            // Debug.Log($"Footstep: Surface={surfaceSystem.CurrentSurface}, Value={surfaceSystem.CurrentSurfaceValue}, Detected={detected}");
            surfaceSystem.ApplySurfaceParameter(audioTrigger, surfaceParameterName);
        }
        
        // Configura e toca o evento FMOD
        audioTrigger.fmodEvent = footstepEvent;
        audioTrigger.PlayAtPosition(transform.position);
    }
}