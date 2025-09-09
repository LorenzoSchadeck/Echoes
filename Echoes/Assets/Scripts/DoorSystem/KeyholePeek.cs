using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class KeyholePeek : MonoBehaviour, IInteractable
{
    [Header("Localization")]
    [Tooltip("Referência à chave do prompt para 'espiar' (ex: PROMPT_PEEK).")]
    [SerializeField] private LocalizedString interactionPrompt;
    public string InteractionPrompt => interactionPrompt.GetLocalizedString();

    [Header("Mechanics")]
    [Tooltip("A quantidade de sanidade (0 a 1) perdida ao começar a espiar.")]
    [SerializeField, Range(0f, 1f)] private float sanityLossAmount = 0.1f;

    [Header("Dependencies")]
    [Tooltip("A Câmera Virtual da Cinemachine posicionada na fechadura.")]
    [SerializeField] private CinemachineCamera peekCamera;
    [SerializeField] private GameObject progressPanel;

    private bool isPeeking = false;
    private PlayerInteractor playerInteractor;
    private static WaitForSeconds _blendCamera = new(2f);

    private void Start()
    {
        playerInteractor = FindAnyObjectByType<PlayerInteractor>();
    }

    private void Update()
    {
        if (isPeeking)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                StopPeeking();
            }
        }
    }

    public bool Interact(Transform interactor)
    {
        if (!isPeeking)
        {
            StartPeeking();
            return true;
        }

        return false;
    }

    private void StartPeeking()
    {
        isPeeking = true;
        StartCoroutine(InProgressPanelActivated());
        GameEvents.TriggerSanityLost(sanityLossAmount);
        playerInteractor?.SetInspectionMode(true);
        if (peekCamera != null) peekCamera.Priority = 2;
    }

    private void StopPeeking()
    {
        isPeeking = false;
        InProgressPanelDeactivated();
        playerInteractor.SetInspectionMode(false);
        if (peekCamera != null) peekCamera.Priority = 0;
    }

    void InProgressPanelDeactivated()
    {
        progressPanel.SetActive(false);
    }

    IEnumerator InProgressPanelActivated()
    {
        yield return _blendCamera;
        progressPanel.SetActive(true);
    }
}