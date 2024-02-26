using System.Globalization;
using JL.Core.Dicts;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Freqs;
public static class FreqDBUtils
{
    internal static readonly string s_dbFolderPath = Path.Join(Utils.ResourcesPath, "Frequency Databases");

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

    public static bool DeleteOldDB(bool dbExists, int version, string dictName, string dbPath)
    {
        if (dbExists)
        {
            if (version > GetVersionFromDB(dictName))
            {
                DictDBUtils.SendOptimizePragmaToAllDicts();
                SendOptimizePragmaToAllFreqDicts();
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

    public static void SendOptimizePragmaToAllFreqDicts()
    {
        foreach (Freq dict in FreqUtils.FreqDicts.Values.Where(freq => freq is { Active: true, Ready: true, Options.UseDB.Value: true }))
        {
            SendOptimizePragma(dict.Name);
        }
    }
}
