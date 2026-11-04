using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Acrescenta textos PT/EN a uma tabela de localização, criando a tabela se preciso.
/// Entradas que já existem não são sobrescritas: traduções revisadas à mão ficam como estão.
/// </summary>
public static class LocalizationSetup
{
    public const string TablesFolder = "Assets/Languages";

    public static int AddStrings(string tableName, params (string key, string pt, string en)[] lines)
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName)
            ?? LocalizationEditorSettings.CreateStringTableCollection(tableName, TablesFolder, LocalizationEditorSettings.GetLocales());

        int added = 0;
        foreach (var (key, pt, en) in lines)
        {
            if (collection.SharedData.GetEntry(key) == null)
            {
                collection.SharedData.AddKey(key);
                added++;
            }

            foreach (StringTable table in collection.StringTables)
            {
                if (table.GetEntry(key) != null) continue;
                table.AddEntry(key, table.LocaleIdentifier.Code.StartsWith("pt") ? pt : en);
                EditorUtility.SetDirty(table);
            }
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LocalizationSetup] Tabela '{tableName}': {added} textos novos.");
        return added;
    }
}
