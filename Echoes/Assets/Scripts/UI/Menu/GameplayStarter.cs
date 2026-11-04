using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Põe a cena do jogo direto em estado de jogo ao carregar (o menu agora é uma cena separada):
/// câmera do jogador ativa, olhar ligado, mira visível e cursor preso.
/// </summary>
public class GameplayStarter : MonoBehaviour
{
    [SerializeField] private CinemachineCamera playerCamera;
    [Tooltip("Câmeras que não devem disputar com a do jogador ao começar (ex.: a antiga câmera do menu).")]
    [SerializeField] private CinemachineCamera[] camerasToLower;
    [SerializeField] private CustomInputAxisController lookController;

    private void Start()
    {
        Time.timeScale = 1f;

        if (playerCamera != null) playerCamera.Priority = 10;
        foreach (CinemachineCamera cam in camerasToLower)
        {
            if (cam != null) cam.Priority = -1;
        }

        if (lookController != null) lookController.enabled = true;
        GameHud.Crosshair.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
