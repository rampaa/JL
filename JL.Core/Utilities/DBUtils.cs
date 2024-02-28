using System.Globalization;
using System.Timers;
using JL.Core.Dicts;
using JL.Core.Freqs;
using Microsoft.Data.Sqlite;
using Timer = System.Timers.Timer;

namespace JL.Core.Utilities;
public static class DBUtils
{
    private static readonly Timer s_optimizePragmaTimer = new();

    internal static readonly string s_freqDBFolderPath = Path.Join(Utils.ResourcesPath, "Frequency Databases");
    internal static readonly string s_dictDBFolderPath = Path.Join(Utils.ResourcesPath, "Dictionary Databases");

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

    public static string GetDictDBPath(string dbName)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Path.Join(s_dictDBFolderPath, dbName)}.sqlite");
    }

    public static string GetFreqDBPath(string dbName)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Path.Join(s_freqDBFolderPath, dbName)}.sqlite");
    }

    internal static void StartOptimizePragmaTimer()
    {
        if (!s_optimizePragmaTimer.Enabled)
        {
            s_optimizePragmaTimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
            s_optimizePragmaTimer.Elapsed += SendOptimizePragmaToAllDBs;
            s_optimizePragmaTimer.AutoReset = true;
            s_optimizePragmaTimer.Enabled = true;
        }
    }

    private static void SendOptimizePragmaToAllDBs(object? sender, ElapsedEventArgs e)
    {
        SendOptimizePragmaToAllDBs();
    }

    public static void SendOptimizePragmaToAllDBs()
    {
        SendOptimizePragmaToAllDicts();
        SendOptimizePragmaToAllFreqDicts();
    }

    private static void SendOptimizePragma(string dbPath)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={dbPath};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA optimize;";
        _ = command.ExecuteNonQuery();
    }

    private static void SendOptimizePragmaToAllDicts()
    {
        foreach (Dict dict in DictUtils.Dicts.Values.Where(static dict => dict is { Active: true, Ready: true, Options.UseDB.Value: true }))
        {
            SendOptimizePragma(GetDictDBPath(dict.Name));
        }
    }

    private static void SendOptimizePragmaToAllFreqDicts()
    {
        foreach (Freq freq in FreqUtils.FreqDicts.Values.Where(static freq => freq is { Active: true, Ready: true, Options.UseDB.Value: true }))
        {
            SendOptimizePragma(GetFreqDBPath(freq.Name));
        }
    }

    private static int GetVersionFromDB(string dbPath)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={dbPath};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    internal static bool DeleteOldDB(bool dbExists, int version, string dbPath)
    {
        if (dbExists)
        {
            if (version > GetVersionFromDB(dbPath))
            {
                SendOptimizePragmaToAllDBs();
                SqliteConnection.ClearAllPools();
                File.Delete(dbPath);
                return false;
            }
            return true;
        }
        return false;
    }
}
