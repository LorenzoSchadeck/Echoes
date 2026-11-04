using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Texto de um prompt de interação: o configurado no objeto ou, se ele ficou vazio, o padrão
/// da tabela interaction_prompts. Assim nenhum prompt cai num texto fixo em um idioma só.
/// </summary>
public static class InteractionText
{
    public const string Table = "interaction_prompts";

    public static string Get(LocalizedString text, string fallbackKey)
    {
        if (text != null && !text.IsEmpty) return text.GetLocalizedString();
        return string.IsNullOrEmpty(fallbackKey) ? string.Empty : LocalizationSettings.StringDatabase.GetLocalizedString(Table, fallbackKey);
    }
}
