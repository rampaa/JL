using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Timers;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Freqs;
using Microsoft.Data.Sqlite;
using Timer = System.Timers.Timer;

namespace JL.Core.Utilities;

public static class DBUtils
{
    private static readonly Timer s_optimizePragmaTimer = new();
    internal static FrozenDictionary<string, string> DictDBPaths { get; set; } = FrozenDictionary<string, string>.Empty;
    internal static FrozenDictionary<string, string> FreqDBPaths { get; set; } = FrozenDictionary<string, string>.Empty;

    internal static readonly string s_freqDBFolderPath = Path.Join(Utils.ResourcesPath, "Frequency Databases");
    internal static readonly string s_dictDBFolderPath = Path.Join(Utils.ResourcesPath, "Dictionary Databases");

    internal static readonly DictType[] s_dictTypesWithDBSupport =
    [
        DictType.JMdict,
        DictType.JMnedict,
        DictType.Kanjidic,
        DictType.PitchAccentYomichan,
        DictType.NonspecificWordYomichan,
        DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiWithWordSchemaYomichan,
        DictType.NonspecificNameYomichan,
        DictType.NonspecificYomichan,
        DictType.NonspecificWordNazeka,
        DictType.NonspecificKanjiNazeka,
        DictType.NonspecificNameNazeka,
        DictType.NonspecificNazeka
    ];

    public static string GetDictDBPath(string dbName)
    {
        return DictDBPaths.TryGetValue(dbName, out string? dbPath)
            ? dbPath
            : $"{Path.Join(s_dictDBFolderPath, dbName)}.sqlite";
    }

    public static string GetFreqDBPath(string dbName)
    {
        return FreqDBPaths.TryGetValue(dbName, out string? dbPath)
            ? dbPath
            : $"{Path.Join(s_freqDBFolderPath, dbName)}.sqlite";
    }

    internal static void InitializeOptimizePragmaTimer()
    {
        s_optimizePragmaTimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
        s_optimizePragmaTimer.Elapsed += SendOptimizePragmaToAllDBs;
        s_optimizePragmaTimer.AutoReset = true;
        s_optimizePragmaTimer.Enabled = true;
    }

    private static void SendOptimizePragmaToAllDBs(object? sender, ElapsedEventArgs e)
    {
        SendOptimizePragmaToAllDBs();
    }

    public static void SendOptimizePragmaToAllDBs()
    {
        SendOptimizePragmaToAllDicts();
        SendOptimizePragmaToAllFreqDicts();
        ConfigDBManager.SendOptimizePragma();
    }

    internal static void SendOptimizePragma(string path)
    {
        using SqliteConnection connection = CreateReadWriteDBConnection(path);
        SendOptimizePragma(connection);
    }

    private static void SendOptimizePragma(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA optimize=0x10002;";
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
        using SqliteConnection connection = CreateReadOnlyDBConnection(dbPath);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    internal static bool CheckIfDBSchemaIsOutOfDate(int version, string dbPath)
    {
        return version > GetVersionFromDB(dbPath);
    }

    public static void DeleteDB(string dbPath)
    {
        SendOptimizePragmaToAllDBs();
        SqliteConnection.ClearAllPools();
        File.Delete(dbPath);
    }

    internal static string GetParameter(int parameterCount)
    {
        StringBuilder parameterBuilder = new("(@1");
        for (int i = 1; i < parameterCount; i++)
        {
            _ = parameterBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }
        return parameterBuilder.Append(");").ToString();
    }

    internal static SqliteConnection CreateDBConnection(string path)
    {
        SqliteConnection connection = new($"Data Source={path};");
        connection.Open();
        return connection;
    }

    internal static SqliteConnection CreateReadOnlyDBConnection(string path)
    {
        SqliteConnection connection = new($"Data Source={path};Mode=ReadOnly;");
        connection.Open();
        return connection;
    }

    internal static SqliteConnection CreateReadWriteDBConnection(string path)
    {
        SqliteConnection connection = new($"Data Source={path};Mode=ReadWrite;");
        connection.Open();
        return connection;
    }

    internal static bool RecordExists(string dbPath)
    {
        using SqliteConnection connection = CreateReadOnlyDBConnection(dbPath);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM record
            );
            """;

        return Convert.ToBoolean(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    //public static string GetSqliteVersion()
    //{
    //    using SqliteConnection connection = new();
    //    connection.Open();
    //    using SqliteCommand command = connection.CreateCommand();
    //    command.CommandText = "SELECT SQLITE_VERSION();";
    //    return (string)command.ExecuteScalar()!;
    //}

    //public static List<string> GetCompileOptions()
    //{
    //    using SqliteConnection connection = new();
    //    connection.Open();
    //    using SqliteCommand command = connection.CreateCommand();
    //    command.CommandText = "PRAGMA compile_options";
    //    List<string> compileOptions = [];
    //    using SqliteDataReader sqliteDataReader = command.ExecuteReader();
    //    while (sqliteDataReader.Read())
    //    {
    //        compileOptions.Add(sqliteDataReader.GetString(0));
    //    }
    //    return compileOptions;
    //}
}
