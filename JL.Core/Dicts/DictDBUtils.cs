using System.Globalization;
using JL.Core.Freqs;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts;
public static class DictDBUtils
{
    internal static readonly string s_dbFolderPath = Path.Join(Utils.ResourcesPath, "Dictionary Databases");

    internal static readonly DictType[] s_dictTypesWithDBSupport = {
        DictType.JMdict,
        DictType.JMnedict,
        DictType.Kanjidic,
        DictType.Daijirin,
        DictType.Daijisen,
        DictType.Gakken,
        DictType.GakkenYojijukugoYomichan,
        DictType.IwanamiYomichan,
        DictType.JitsuyouYomichan,
        DictType.KanjigenYomichan,
        DictType.Kenkyuusha,
        DictType.KireiCakeYomichan,
        DictType.Kotowaza,
        DictType.Koujien,
        DictType.Meikyou,
        DictType.NikkokuYomichan,
        DictType.OubunshaYomichan,
        DictType.ShinjirinYomichan,
        DictType.ShinmeikaiYomichan,
        DictType.ShinmeikaiYojijukugoYomichan,
        DictType.WeblioKogoYomichan,
        DictType.ZokugoYomichan,
        DictType.PitchAccentYomichan,
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiWithWordSchemaYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan,
        DictType.DaijirinNazeka,
        DictType.KenkyuushaNazeka,
        DictType.ShinmeikaiNazeka,
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka
    };

    public static string GetDBPath(string dbName)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Path.Join(s_dbFolderPath, dbName)}.sqlite");
    }

    private static int GetVersionFromDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    internal static bool DeleteOldDB(bool dbExists, int version, string dictName, string dbPath)
    {
        if (dbExists)
        {
            if (version > GetVersionFromDB(dictName))
            {
                SendOptimizePragmaToAllDicts();
                FreqDBUtils.SendOptimizePragmaToAllFreqDicts();
                SqliteConnection.ClearAllPools();
                File.Delete(dbPath);
                return false;
            }
            return true;
        }
        return false;
    }

    private static void SendOptimizePragma(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA optimize;";
        _ = command.ExecuteNonQuery();
    }

    public static void SendOptimizePragmaToAllDicts()
    {
        foreach (Dict dict in DictUtils.Dicts.Values.Where(static dict => dict is { Active: true, Ready: true, Options.UseDB.Value: true }))
        {
            SendOptimizePragma(dict.Name);
        }
    }
}
