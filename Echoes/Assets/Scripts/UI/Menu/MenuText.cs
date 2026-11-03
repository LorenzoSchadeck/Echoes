using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

/// <summary>
/// Textos do menu vindos da tabela de localização "menu". Nos UXML, um texto que começa com
/// "M_" é a chave da entrada; <see cref="Localize"/> troca pela tradução do idioma atual.
/// </summary>
public static class MenuText
{
    public const string Table = "menu";
    private const string KeyPrefix = "M_";

    public static string Get(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);

    /// <summary>
    /// Traduz todos os textos-chave da árvore. Pode ser chamado de novo ao trocar o idioma:
    /// a chave original fica guardada no userData do elemento.
    /// </summary>
    public static void Localize(VisualElement root)
    {
        root.Query<TextElement>().ForEach(element =>
        {
            if (element.userData is not string key)
            {
                if (element.text == null || !element.text.StartsWith(KeyPrefix)) return;
                key = element.text;
                element.userData = key;
            }

            // Sem notificar: trocar o texto de um rótulo dispara um ChangeEvent<string> que sobe até o
            // campo pai (um DropdownField, por exemplo) e seria lido como mudança de valor
            ((INotifyValueChanged<string>)element).SetValueWithoutNotify(Get(key));
        });
    }
}
