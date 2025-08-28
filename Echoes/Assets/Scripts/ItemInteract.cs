using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; 

[RequireComponent(typeof(Collider))]
public class ItemInteract : MonoBehaviour, IInteractable
{
    [Header("Item Info")]
    [SerializeField] private string itemName;
    [TextArea]
    [SerializeField] private string itemDescription;

    [Header("UI References")]
    [Tooltip("O GameObject do painel que será ativado.")]
    [SerializeField] private GameObject inspectionPanel;
    [Tooltip("O campo de texto para o nome do item.")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [Tooltip("O campo de texto para a descrição do item.")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Inspection Settings")]
    [SerializeField] private float inspectionDistance = 0.8f;
    [SerializeField] private float transitionSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;

    // Referências privadas
    private Transform cameraTransform;
    private bool isInspecting = false;
    private PlayerInteractor playerInteractor;
    private Coroutine activeTransition = null;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    
    public string InteractionPrompt => $"(E) Inspecionar {itemName}";

    public bool Interact(Transform interactor)
    {
        if (isInspecting) return false;

        if (playerInteractor == null) playerInteractor = interactor.GetComponent<PlayerInteractor>();
        
        if (playerInteractor != null)
        {
            cameraTransform = playerInteractor.CameraTransform;
            if (cameraTransform != null)
            {
                StartInspection();
                return true;
            }
        }
        return false;
    }

    private void Update()
    {
        if (!isInspecting) return;

        if (Mouse.current.leftButton.isPressed)
        {
            RotateItem();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ExitInspection();
        }
    }

    private void StartInspection()
    {
        isInspecting = true;
        PlayerMovement.canMove = false; // Impede movimento e passos
        playerInteractor.SetInspectionMode(true);

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        
        GetComponent<Collider>().enabled = false;

        Vector3 inspectionPosition = cameraTransform.position + cameraTransform.forward * inspectionDistance;
        Quaternion inspectionRotation = cameraTransform.rotation * Quaternion.Euler(0, 180, 0);

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(MoveToTarget(inspectionPosition, inspectionRotation));
        
        ShowInspectionPanel();
    }

    private void ExitInspection()
    {
        if (!isInspecting) return;
        isInspecting = false;
        PlayerMovement.canMove = true; // Libera movimento e passos
        playerInteractor.SetInspectionMode(false);

        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(MoveToTarget(originalPosition, originalRotation, true));
        
        HideInspectionPanel();
    }
    
    private void ShowInspectionPanel()
    {
        if (inspectionPanel != null && itemNameText != null && itemDescriptionText != null)
        {
            itemNameText.text = itemName;
            itemDescriptionText.text = itemDescription;
            inspectionPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Referências da UI não foram definidas no item: " + gameObject.name, this);
        }
    }

    private void HideInspectionPanel()
    {
        if (inspectionPanel != null)
        {
            inspectionPanel.SetActive(false);
        }
    }

    private IEnumerator MoveToTarget(Vector3 targetPos, Quaternion targetRot, bool isReturning = false)
    {
        if (!isReturning) transform.SetParent(null);
        
        float time = 0;
        float duration = 0.4f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        if (isReturning)
        {
            transform.SetParent(originalParent);
            GetComponent<Collider>().enabled = true;
        }
        activeTransition = null;
    }

    private void RotateItem()
    {
        float rotationX = Mouse.current.delta.x.ReadValue() * rotationSpeed * Time.deltaTime;
        float rotationY = Mouse.current.delta.y.ReadValue() * rotationSpeed * Time.deltaTime;
        
        transform.Rotate(cameraTransform.up, -rotationX, Space.World);
        transform.Rotate(cameraTransform.right, rotationY, Space.World);
    }
}