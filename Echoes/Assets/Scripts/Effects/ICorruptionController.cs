namespace Echoes.Effects
{
    /// <summary>
    /// Interface comum para controladores de corrupção
    /// </summary>
    public interface ICorruptionController
    {
        /// <summary>
        /// Define a intensidade da corrupção (0-1)
        /// </summary>
        void SetCorruptionIntensity(float intensity);
        
        /// <summary>
        /// Força atualização dos efeitos de corrupção
        /// </summary>
        void ForceUpdate();
        
        /// <summary>
        /// Obtém a intensidade atual da corrupção
        /// </summary>
        float GetCorruptionIntensity();
        
        /// <summary>
        /// Verifica se o objeto está visível/dentro da área de renderização
        /// </summary>
        bool IsVisible();
        
        /// <summary>
        /// Nome do GameObject para identificação
        /// </summary>
        string name { get; }
    }
}