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
        foreach (Dict dict in DictUtils.Dicts.Values.ToArray().AsSpan())
        {
            if (dict is { Active: true, Ready: true, Options.UseDB.Value: true })
            {
                SendOptimizePragma(GetDictDBPath(dict.Name));
            }
        }
    }

    private static void SendOptimizePragmaToAllFreqDicts()
    {
        foreach (Freq freq in FreqUtils.FreqDicts.Values.ToArray().AsSpan())
        {
            if (freq is { Active: true, Ready: true, Options.UseDB.Value: true })
            {
                SendOptimizePragma(GetFreqDBPath(freq.Name));
            }
        }
    }

    private static int GetVersionFromDB(string dbPath)
    {
        using SqliteConnection connection = CreateReadOnlyDBConnection(dbPath);
        SetCacheSizeToZero(connection);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
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

    //private static int CalculateParameterStringCapacity(int parameterCount)
    //{
    //    // Total non-digit characters:
    //    // '(', ')', ';' = 3 chars
    //    // '@' symbol per parameter = parameterCount chars
    //    // ", " separator for N-1 parameters = 2 * (parameterCount - 1) chars
    //    // Simplified formula: 3 * parameterCount + 1
    //    int nonDigitChars = (3 * parameterCount) + 1;

    //    int digitChars;
    //    if (parameterCount < 10)
    //    {
    //        digitChars = parameterCount;
    //    }
    //    else if (parameterCount < 100)
    //    {
    //        digitChars = 9 + ((parameterCount - 9) * 2);
    //    }
    //    else // if (parameterCount < 1000)
    //    {
    //        digitChars = 189 + ((parameterCount - 99) * 3);
    //    }

    //    return nonDigitChars + digitChars;
    //}


    internal static string GetParameter(int parameterCount)
    {
        StringBuilder parameterBuilder = Utils.StringBuilderPool.Get();

        _ = parameterBuilder.Append("(@1");
        for (int i = 1; i < parameterCount; i++)
        {
            _ = parameterBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        string parameter = parameterBuilder.Append(");").ToString();
        Utils.StringBuilderPool.Return(parameterBuilder);
        return parameter;
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
        SetCacheSizeToZero(connection);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM record
            );
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetBoolean(0);
    }

    internal static void SetSynchronousModeToNormal(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA synchronous = 1;";
        _ = command.ExecuteNonQuery();
    }

    internal static void SetCacheSizeToZero(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA cache_size = 0;";
        _ = command.ExecuteNonQuery();
    }

    //internal static bool IsDBCorrupt(SqliteConnection connection)
    //{
    //    using SqliteCommand command = connection.CreateCommand();
    //    command.CommandText = "PRAGMA integrity_check;";

    //    SqliteDataReader reader = command.ExecuteReader();
    //    _ = reader.Read();
    //    return reader.GetString(0) is not "ok";
    //}

    //public static string GetSqliteVersion()
    //{
    //    using SqliteConnection connection = new();
    //    connection.Open();
    //    using SqliteCommand command = connection.CreateCommand();
    //    command.CommandText = "SELECT SQLITE_VERSION();";

    //    SqliteDataReader reader = command.ExecuteReader();
    //    _ = reader.Read();
    //    return reader.GetString(0);
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
