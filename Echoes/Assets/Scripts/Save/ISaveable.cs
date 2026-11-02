/// <summary>
/// Componente cujo estado entra no checkpoint. Precisa de um SaveableId no mesmo GameObject
/// (o menu Echoes > Save > Adicionar SaveableId aos salváveis adiciona os que faltam).
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Estado atual em JSON (normalmente JsonUtility.ToJson de um struct privado).
    /// </summary>
    string CaptureState();

    /// <summary>
    /// Aplica um estado salvo. Chamado depois do Start de todos os objetos, sem animação nem som.
    /// </summary>
    void RestoreState(string json);
}
