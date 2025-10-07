using UnityEngine;
using System.Collections;

/// <summary>
/// Controla a rotação da câmera no menu
/// Rotaciona para Options e volta para o Menu Principal
/// </summary>
public class MenuCameraController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Rotations")]
    [SerializeField] private Vector3 mainMenuRotation = new Vector3(67f, -90f, 0f);
    [SerializeField] private Vector3 optionsRotation = new Vector3(98.5f, -90f, 0f);
    
    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 1f;
    
    private Coroutine rotationCoroutine;
    
    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        // Começa na rotação do menu principal
        cameraTransform.rotation = Quaternion.Euler(mainMenuRotation);
    }
    
    /// <summary>
    /// Rotaciona para o menu de opções
    /// </summary>
    public void GoToOptions()
    {
        RotateToPosition(optionsRotation);
    }
    
    /// <summary>
    /// Volta para o menu principal
    /// </summary>
    public void GoBackToMainMenu()
    {
        RotateToPosition(mainMenuRotation);
    }
    
    private void RotateToPosition(Vector3 targetRotation)
    {
        if (rotationCoroutine != null)
            StopCoroutine(rotationCoroutine);
            
        rotationCoroutine = StartCoroutine(RotateCamera(targetRotation));
    }
    
    private IEnumerator RotateCamera(Vector3 targetRotation)
    {
        Quaternion startRotation = cameraTransform.rotation;
        Quaternion endRotation = Quaternion.Euler(targetRotation);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < rotationSpeed)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / rotationSpeed;
            
            cameraTransform.rotation = Quaternion.Lerp(startRotation, endRotation, progress);
            
            yield return null;
        }
        
        cameraTransform.rotation = endRotation;
    }
}