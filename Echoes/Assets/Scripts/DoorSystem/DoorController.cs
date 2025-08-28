using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DoorController : MonoBehaviour, IInteractable
{
    public enum DoorState { Unlocked, Locked, Jammed }

    [Header("State Settings")]
    [SerializeField] private DoorState currentState = DoorState.Unlocked;

    [Header("Movement Settings")]
    [SerializeField] private float openSpeed = 2.0f;
    [Tooltip("The absolute angle the door will open (e.g., 90). The direction will be determined automatically.")]
    [SerializeField] private float fullOpenAngle = 90.0f;
    [SerializeField] private float jammedOpenAngle = 25.0f;

    [Header("Movement Settings")]
    [SerializeField] private bool openToPositiveSide = true;

    [Header("Hierarchy")]
    [Tooltip("The pivot object around which the door rotates. Usually the empty parent.")]
    [SerializeField] private Transform pivot;

    [Header("Sounds")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] private AudioClip jammedSound;

    private AudioSource audioSource;
    private Quaternion initialRotation;
    private bool isOpen = false;
    private bool isMoving = false;
    
    public string InteractionPrompt
    {
        get
        {
            if (isMoving) return string.Empty;

            switch (currentState)
            {
                case DoorState.Locked: return "Tentar abrir (Trancada)";
                case DoorState.Jammed: return isOpen ? "(E) Fechar a porta" : "(E) Abrir a porta";
                default:               return isOpen ? "(E) Fechar a porta" : "(E) Abrir a porta";
            }
        }
    }

    public bool Interact(Transform interactor)
    {
        if (isMoving) return false;

        if (isOpen)
        {
            MoveDoor(0, closeSound);
            return true;
        }

        float direction = openToPositiveSide ? 1f : -1f;

        switch (currentState)
        {
            case DoorState.Unlocked:
                MoveDoor(fullOpenAngle * direction, openSound);
                return true;
            case DoorState.Locked:
                PlaySound(lockedSound);
                return false;
            case DoorState.Jammed:
                MoveDoor(jammedOpenAngle * direction, jammedSound);
                return true;
        }
        return false;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (pivot == null)
        {
            Debug.LogWarning("Pivot da porta não foi definido, tentando usar o pai.", this);
            pivot = transform.parent;
        }
        initialRotation = pivot.rotation;
    }

    private void MoveDoor(float targetAngle, AudioClip movementSound)
    {
        // Esta lógica de calcular o alvo da rotação já estava correta!
        // A chave é que o 'targetAngle' agora vem com o sinal (+ ou -) correto.
        Quaternion targetRotation = isOpen ? initialRotation : initialRotation * Quaternion.Euler(0, 0, targetAngle);
        
        StartCoroutine(AnimateDoor(targetRotation, movementSound));
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation, AudioClip movementSound)
    {
        isMoving = true;
        PlaySound(movementSound);

        Quaternion currentRotation = pivot.rotation;
        float time = 0f;

        while (time < 1f)
        {
            pivot.rotation = Quaternion.Slerp(currentRotation, targetRotation, time);
            time += Time.deltaTime * openSpeed;
            yield return null;
        }

        pivot.rotation = targetRotation;
        
        if (targetRotation != initialRotation)
        {
            isOpen = true;
        }
        else
        {
            isOpen = false;
        }
        
        isMoving = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void LockDoor() { currentState = DoorState.Locked; }
    public void UnlockDoor() { currentState = DoorState.Unlocked; }
    public void JamDoor() { currentState = DoorState.Jammed; }
}