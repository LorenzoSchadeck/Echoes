using UnityEngine;

/// <summary>
/// O item do corvo em cada jogo (a chave de Echoes, um chapéu, uma bússola...).
/// O ponto de encaixe do item vai no ponto do mascote; o tamanho é relativo à imagem do corvo.
/// </summary>
[CreateAssetMenu(fileName = "Acessorio", menuName = "Tinker Crow/Acessório")]
public class MascotAccessory : ScriptableObject
{
    public enum Slot { Beak }

    public Sprite sprite;
    public Slot slot = Slot.Beak;
    [Tooltip("Ponto do item que encaixa no mascote (0 a 1, origem no canto superior esquerdo). Na chave, a argola.")]
    public Vector2 attachPoint = new Vector2(0.55f, 0.08f);
    [Tooltip("Tamanho do item em relação à largura da imagem do corvo.")]
    [Range(0.02f, 1f)] public float size = 0.2f;
    [Tooltip("Balança como pêndulo quando o corvo pousa (a chave pendurada no bico).")]
    public bool swings = true;
}
