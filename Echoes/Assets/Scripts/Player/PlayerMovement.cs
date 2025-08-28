using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimentação")]
    public float moveSpeed = 5f;
    public float groundCheckDistance = 0.6f;
    public LayerMask groundLayer;
    public Transform cameraTransform;

    [Header("Sons de Passo")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float timeBetweenSteps = 0.5f;

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private AudioSource audioSource; // Removido o [SerializeField] pois pegamos no Awake

    private float footstepTimer;

    public static bool canMove = true;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

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
        // Não toca passos se não pode mover
        if (!canMove) return;

        // Verifica se o jogador está se movendo no chão (ignorando o eixo Y)
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
            footstepTimer = 0f; // Reseta o timer se o jogador parar
        }
    }

    void PlayFootstepSound()
    {
        if (footstepClips.Length > 0)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    bool IsGrounded()
    {
        // O Raycast é mais confiável se não começar exatamente do centro do objeto
        Vector3 rayStartPoint = transform.position + Vector3.up * 0.1f; 
        return Physics.Raycast(rayStartPoint, Vector3.down, groundCheckDistance, groundLayer);
    }

    // Opcional: Desenhar o Raycast para depuração no Editor da Unity
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayStartPoint = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStartPoint, rayStartPoint + Vector3.down * groundCheckDistance);
    }
}