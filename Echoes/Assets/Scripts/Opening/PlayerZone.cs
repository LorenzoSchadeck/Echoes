using System;
using UnityEngine;

/// <summary>
/// Área do roteiro: avisa quando o jogador entra (o colisor precisa ser trigger).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlayerZone : MonoBehaviour
{
    public event Action<PlayerZone> Entered;

    public bool PlayerInside { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        PlayerInside = true;
        Entered?.Invoke(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != null) PlayerInside = false;
    }
}
