using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// Textos PT/EN nas tabelas de localização, criando a tabela se preciso.
/// AddStrings só acrescenta (traduções revisadas à mão ficam como estão); SetStrings sobrescreve.
/// </summary>
public static class LocalizationSetup
{
    public const string TablesFolder = "Assets/Languages";

    public static int AddStrings(string tableName, params (string key, string pt, string en)[] lines)
    {
        StringTableCollection collection = GetOrCreate(tableName);

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

    /// <summary>
    /// Grava os textos PT/EN, sobrescrevendo o que já existe. Use para aplicar um texto aprovado
    /// (por exemplo o roteiro) por cima de uma versão antiga.
    /// </summary>
    public static void SetStrings(string tableName, params (string key, string pt, string en)[] lines)
    {
        StringTableCollection collection = GetOrCreate(tableName);

        foreach (var (key, pt, en) in lines)
        {
            if (collection.SharedData.GetEntry(key) == null) collection.SharedData.AddKey(key);

            foreach (StringTable table in collection.StringTables)
            {
                string value = table.LocaleIdentifier.Code.StartsWith("pt") ? pt : en;
                StringTableEntry entry = table.GetEntry(key);
                if (entry != null) entry.Value = value;
                else table.AddEntry(key, value);
                EditorUtility.SetDirty(table);
            }
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LocalizationSetup] Tabela '{tableName}': {lines.Length} textos gravados.");
    }

    /// <summary>
    /// Tira chaves que saíram do jogo. Chaves que não existem são ignoradas.
    /// </summary>
    public static void RemoveKeys(string tableName, params string[] keys)
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
        if (collection == null) return;

        foreach (string key in keys)
        {
            if (collection.SharedData.GetEntry(key) != null) collection.RemoveEntry(key);
        }

        EditorUtility.SetDirty(collection.SharedData);
        foreach (StringTable table in collection.StringTables) EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Corrige chaves com espaço ou quebra de linha sobrando no nome. As referências do jogo
    /// apontam para o id da chave, então continuam valendo.
    /// </summary>
    public static void TrimKeys(string tableName)
    {
        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
        if (collection == null) return;

        foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries.ToArray())
        {
            string trimmed = entry.Key.Trim();
            if (trimmed != entry.Key && collection.SharedData.GetEntry(trimmed) == null)
                collection.SharedData.RenameKey(entry.Key, trimmed);
        }

        EditorUtility.SetDirty(collection.SharedData);
    }

    private static StringTableCollection GetOrCreate(string tableName) =>
        LocalizationEditorSettings.GetStringTableCollection(tableName)
        ?? LocalizationEditorSettings.CreateStringTableCollection(tableName, TablesFolder, LocalizationEditorSettings.GetLocales());
}
