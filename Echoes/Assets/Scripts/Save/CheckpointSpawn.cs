using UnityEngine;

/// <summary>
/// Onde Ethan acorda depois de um colapso (a poltrona). Posição e rotação deste objeto viram
/// a posição do jogador e a direção do olhar ao carregar o checkpoint.
/// </summary>
public class CheckpointSpawn : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.3f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.9f + transform.forward * 0.6f);
    }
}
