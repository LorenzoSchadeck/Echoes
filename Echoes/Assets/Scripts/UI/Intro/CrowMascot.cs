using System;
using UnityEngine;

/// <summary>
/// Um desenho do mascote (Tinker Crow) com os pontos de encaixe dos acessórios.
/// Coordenadas normalizadas na imagem, com a origem no canto superior esquerdo.
/// </summary>
[Serializable]
public class MascotFrame
{
    public Sprite sprite;
    [Tooltip("Onde o acessório do bico encaixa (entre as duas partes do bico aberto).")]
    public Vector2 beak = new Vector2(0.85f, 0.4f);
    [Tooltip("Trecho do sprite repetido na frente do acessório, relativo ao ponto do bico (origem superior esquerda).")]
    public Rect beakForeground = new Rect(-.09f, -.06f, .20f, .13f);
    [Tooltip("Altura do fio desenhado na imagem (0 = topo, 1 = base). Alinha os quadros no mesmo fio.")]
    [Range(0f, 1f)] public float wire = 0.8f;
}

/// <summary>Sequência de desenhos tocada em ordem (voo, pouso) ou alternada (o "traço vivo").</summary>
[Serializable]
public class MascotClip
{
    public string name;
    public MascotFrame[] frames = Array.Empty<MascotFrame>();
    [Tooltip("Quadros por segundo.")]
    public float fps = 8f;
    public bool loop = true;

    public MascotFrame FrameAt(float time)
    {
        if (frames.Length == 0) return null;
        int index = Mathf.FloorToInt(time * Mathf.Max(0.01f, fps));
        index = loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1);
        return frames[index];
    }

    public float Duration => frames.Length / Mathf.Max(0.01f, fps);
}

/// <summary>
/// O corvo base, igual em todos os jogos: as sequências de chegada, pouso e parado.
/// O acessório de cada jogo fica à parte (<see cref="MascotAccessory"/>).
/// </summary>
[CreateAssetMenu(fileName = "Mascote", menuName = "Tinker Crow/Mascote")]
public class CrowMascot : ScriptableObject
{
    [Tooltip("Voando para o fio (as asas batem alternando os desenhos).")]
    public MascotClip arrive = new MascotClip { name = "Chegando", fps = 9f, loop = true };
    [Tooltip("Toca o fio e fecha as asas (uma vez).")]
    public MascotClip land = new MascotClip { name = "Pousando", fps = 7f, loop = false };
    [Tooltip("Parado no fio, com o traço tremendo de leve.")]
    public MascotClip idle = new MascotClip { name = "Parado", fps = 6f, loop = true };
}
