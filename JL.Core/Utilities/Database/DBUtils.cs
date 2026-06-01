using System.Diagnostics;
using System.Globalization;
using System.Text;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Utilities.ObjectPool;
using Microsoft.Data.Sqlite;

namespace JL.Core.Utilities.Database;

public static class DBUtils
{
    internal static readonly string s_freqDBFolderPath = Path.Join(AppInfo.ResourcesPath, "Frequency Databases");
    internal static readonly string s_dictDBFolderPath = Path.Join(AppInfo.ResourcesPath, "Dictionary Databases");

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

    private static readonly string[] s_parameterNames = CreateParameterNames();

    private static string[] CreateParameterNames()
    {
        string[] parameterNames = new string[1024 * 2];
        for (int i = 1; i < parameterNames.Length; i++)
        {
            parameterNames[i] = $"@{i}";
        }

        return parameterNames;
    }

    public static string GetDBPathForDict(string dictName)
    {
        return $"{Path.Join(s_dictDBFolderPath, dictName)}.sqlite";
    }

    public static string GetDBPathForFreqDict(string dictName)
    {
        return $"{Path.Join(s_freqDBFolderPath, dictName)}.sqlite";
    }

    public static void SendOptimizePragmaToAllDBs()
    {
        SendOptimizePragmaToAllDicts();
        SendOptimizePragmaToAllFreqDicts();
        ConfigDBManager.SendOptimizePragma();
    }

    internal static void SendOptimizePragma(string dbPath)
    {
        using SqliteConnection? connection = CreateReadWriteDBConnection(dbPath);
        if (connection is not null)
        {
            SendOptimizePragma(connection);
        }
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
                SendOptimizePragma(dict.DBPath);
            }
        }
    }

    private static void SendOptimizePragmaToAllFreqDicts()
    {
        foreach (Freq freq in FreqUtils.FreqDicts.Values.ToArray().AsSpan())
        {
            if (freq is { Active: true, Ready: true, Options.UseDB.Value: true })
            {
                SendOptimizePragma(freq.DBPath);
            }
        }
    }

    private static int GetVersionFromDB(string readOnlyConnectionString)
    {
        using SqliteConnection? connection = CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        Debug.Assert(connection is not null);

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    internal static bool CheckIfDBSchemaIsOutOfDate(int version, string readonlyConnectionString)
    {
        return version != GetVersionFromDB(readonlyConnectionString);
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
        StringBuilder parameterBuilder = ObjectPoolManager.StringBuilderPool.Get();

        _ = parameterBuilder.Append("(@1");
        for (int i = 1; i < parameterCount; i++)
        {
            _ = parameterBuilder.Append(CultureInfo.InvariantCulture, $", {GetParameterName(i + 1)}");
        }

        string parameter = parameterBuilder.Append(");").ToString();
        ObjectPoolManager.StringBuilderPool.Return(parameterBuilder);
        return parameter;
    }

    public static string GetParameterName(int index)
    {
        Debug.Assert(index <= 32767);
        return (uint)index < (uint)s_parameterNames.Length
            ? s_parameterNames[index]
            : string.Create(CultureInfo.InvariantCulture, $"@{index}");
    }

    internal static SqliteConnection CreateDBConnection(string dbPath)
    {
        SqliteConnection connection = new($"Data Source={dbPath};");
        connection.Open();
        return connection;
    }

    internal static string GetReadOnlyConnectionString(string dbPath)
    {
        return $"Data Source={dbPath};Mode=ReadOnly;";
    }

    internal static SqliteConnection? CreateDBConnectionForReadOnlyConnectionString(string readOnlyConnectionString)
    {
        SqliteConnection connection = new(readOnlyConnectionString);
        try
        {
            connection.Open();
            return connection;
        }
        catch (SqliteException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to open DB connection in read-only mode for path: {DBPath}", readOnlyConnectionString);
            connection.Dispose();
            return null;
        }
    }

    internal static SqliteConnection? CreateReadWriteDBConnection(string dbPath)
    {
        SqliteConnection connection = new($"Data Source={dbPath};Mode=ReadWrite;");
        try
        {
            connection.Open();
            return connection;
        }
        catch (SqliteException ex)
        {
            LoggerManager.Logger.Error(ex, "Failed to open DB connection in ReadWrite mode for path: {DBPath}", dbPath);
            connection.Dispose();
            return null;
        }
    }

    internal static bool RecordExists(string readOnlyConnectionString)
    {
        using SqliteConnection? connection = CreateDBConnectionForReadOnlyConnectionString(readOnlyConnectionString);
        Debug.Assert(connection is not null);

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
