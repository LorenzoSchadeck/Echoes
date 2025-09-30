using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using FMODUnity;
public class DoorController : MonoBehaviour, IInteractable
{
    public enum DoorState { Unlocked, Locked, Jammed }

    [Header("State Settings")]
    [SerializeField] private DoorState currentState = DoorState.Unlocked;

    [Header("Localization")]
    [SerializeField] private LocalizedString openPrompt;
    [SerializeField] private LocalizedString closePrompt;
    [SerializeField] private LocalizedString lockedPrompt;
    [SerializeField] private LocalizedString movingPrompt;

    [Header("Movement Settings")]
    [SerializeField] private float openSpeed = 2.0f;
    [Tooltip("The absolute angle the door will open (e.g., 90). The direction will be determined automatically.")]
    [SerializeField] private float fullOpenAngle = 90.0f;
    [SerializeField] private float jammedOpenAngle = 25.0f;
    [SerializeField] private bool openToPositiveSide = true;

    [Header("Hierarchy")]
    [Tooltip("The pivot object around which the door rotates. Usually the empty parent.")]
    [SerializeField] private Transform pivot;

    [Header("Sons FMOD")]
    [SerializeField] private EventReference openEvent;
    [SerializeField] private EventReference closeEvent;
    [SerializeField] private EventReference lockedEvent;
    [SerializeField] private EventReference jammedEvent;

    private FMODAudioTrigger audioTrigger;
    private Quaternion initialRotation;
    private bool isOpen = false;
    private bool isMoving = false;
    

    public string InteractionPrompt
    {
        get
        {
            if (isMoving) return movingPrompt.GetLocalizedString();
            switch (currentState)
            {
                case DoorState.Locked:
                    return lockedPrompt.GetLocalizedString();
                case DoorState.Jammed:
                case DoorState.Unlocked:
                default:
                    return isOpen ? closePrompt.GetLocalizedString() : openPrompt.GetLocalizedString();
            }
        }
    }

    private void Awake()
    {
        audioTrigger = gameObject.AddComponent<FMODAudioTrigger>();
        if (pivot == null)
        {
            Debug.LogWarning("Pivot da porta não foi definido, tentando usar o pai.", this);
            pivot = transform.parent;
        }
        initialRotation = pivot.rotation;
    }

    public bool Interact(Transform interactor)
    {
        if (isMoving) return false;

        if (isOpen)
        {
            MoveDoor(0, closeEvent);
            return true;
        }

        float direction = openToPositiveSide ? 1f : -1f;

        switch (currentState)
        {
            case DoorState.Unlocked:
                MoveDoor(fullOpenAngle * direction, openEvent);
                return true;
            case DoorState.Locked:
                PlayFMODSound(lockedEvent);
                return false;
            case DoorState.Jammed:
                MoveDoor(jammedOpenAngle * direction, jammedEvent);
                return true;
        }
        return false;
    }

    private void MoveDoor(float targetAngle, EventReference movementEvent)
    {
        Quaternion targetRotation = isOpen ? initialRotation : initialRotation * Quaternion.Euler(0, 0, targetAngle);
        StartCoroutine(AnimateDoor(targetRotation, movementEvent));
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation, EventReference movementEvent)
    {
        isMoving = true;
        PlayFMODSound(movementEvent);

        Quaternion currentRotation = pivot.rotation;
        float time = 0f;

        while (time < 1f)
        {
            pivot.rotation = Quaternion.Slerp(currentRotation, targetRotation, time);
            time += Time.deltaTime * openSpeed;
            yield return null;
        }

        pivot.rotation = targetRotation;

        isOpen = targetRotation != initialRotation;
        isMoving = false;
    }

    private void PlayFMODSound(EventReference evt)
    {
        if (evt.IsNull) return;
        audioTrigger.fmodEvent = evt;
        audioTrigger.PlayAtPosition(transform.position);
    }

    public void LockDoor() { currentState = DoorState.Locked; }
    public void UnlockDoor() { currentState = DoorState.Unlocked; }
    public void JamDoor() { currentState = DoorState.Jammed; }
}