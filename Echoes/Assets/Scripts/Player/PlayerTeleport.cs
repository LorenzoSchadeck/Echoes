using UnityEngine;

/// <summary>
/// Põe o jogador num ponto, olhando na direção dele (checkpoint, tempo perdido).
/// </summary>
public static class PlayerTeleport
{
    public static void To(Transform point) => To(point.position, point.rotation);

    public static void To(Vector3 position, Quaternion rotation)
    {
        PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player == null) return;

        // Só a direção horizontal: o corpo não inclina
        Quaternion bodyRotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);

        if (player.TryGetComponent(out Rigidbody body))
        {
            body.position = position;
            body.rotation = bodyRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        player.transform.SetPositionAndRotation(position, bodyRotation);

        // A câmera do jogador é controlada pelo Pan/Tilt do Cinemachine, não pela rotação do corpo
        Object.FindAnyObjectByType<PlayerInteractor>()?.SetLookRotation(bodyRotation.eulerAngles.y, 0f);
    }
}
