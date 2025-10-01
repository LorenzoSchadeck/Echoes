using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "RadioTransmission", menuName = "Echoes/Radio Transmission")]
public class RadioTransmission : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("Nome da transmissão para identificação.")]
    public string transmissionName = "Transmissão 1";
    
    [Header("Configurações de Áudio")]
    [Tooltip("Evento FMOD desta transmissão.")]
    public EventReference radioEvent;
    
    [Tooltip("Nome do parâmetro no FMOD que controla os labels.")]
    public string radioParameter = "dub";
    
    [Header("Tempos de Transmissão")]
    [Tooltip("Tempo que o áudio 'ligando' toca antes de iniciar o mumble (segundos).")]
    public float startupDuration = 1f;
    
    [Tooltip("Duração total da transmissão mumble (segundos).")]
    public float transmissionDuration = 15f;
    
    [Tooltip("Intervalo entre mudanças de parâmetro mumble (segundos).")]
    public float mumbleChangeInterval = 1f;
    
    [Header("Valores dos Parâmetros")]
    [Tooltip("Valor do parâmetro para 'ligando'.")]
    public int startupParameterValue = 1;
    
    [Tooltip("Valor mínimo do parâmetro para mumble.")]
    public int mumbleMinValue = 2;
    
    [Tooltip("Valor máximo do parâmetro para mumble.")]
    public int mumbleMaxValue = 11;
    
    [Tooltip("Valor do parâmetro para estática.")]
    public int staticParameterValue = 12;
}