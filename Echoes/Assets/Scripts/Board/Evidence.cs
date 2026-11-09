using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Uma pista do mural. Data e lugar ficam à vista no cartão: é por eles que o jogador decide o que
/// ligar com o quê. O nome do asset é o ID salvo no checkpoint, então não renomeie depois de lançado.
/// </summary>
[CreateAssetMenu(menuName = "Echoes/Mural/Evidência", fileName = "EV_")]
public class Evidence : ScriptableObject
{
    public LocalizedString title = new LocalizedString();
    [Tooltip("Texto ao examinar o cartão no mural.")]
    public LocalizedString details = new LocalizedString();
    [Tooltip("Pensamento de Ethan sobre a pista. Interpretação pessoal, separada do conteúdo original.")]
    public LocalizedString thought = new LocalizedString();
    [Tooltip("Data como aparece no cartão (ex: 14.03.1995).")]
    public string date;
    public LocalizedString place = new LocalizedString();
    [Tooltip("Foto ou recorte. Opcional.")]
    public Sprite photo;
    [Tooltip("Modelo visual da pista no quadro. Pode reutilizar o prefab do objeto encontrado.")]
    public GameObject boardPrefab;
    [Tooltip("Tamanho máximo da pista na cortiça, em unidades do cenário.")]
    public Vector2 boardSize = new Vector2(1.25f, 1.5f);
    [Tooltip("Posição do cartão no mural, de 0 a 1 (0,0 é o canto superior esquerdo).")]
    public Vector2 boardPosition = new Vector2(0.5f, 0.5f);

    public string Id => name;
}
