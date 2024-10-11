using System.Globalization;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;

public static class ConfigDBManager
{
    private static readonly string s_configsPath = Path.Join(Utils.ConfigPath, "Configs.sqlite");
    public delegate bool TryParseHandler<T>(string value, out T? result);
    public delegate bool TryParseHandlerWithCultureInfo<T>(string value, NumberStyles numberStyles, CultureInfo cultureInfo, out T result);

    public static void CreateDB()
    {
        if (File.Exists(s_configsPath))
        {
            if (File.Exists($"{s_configsPath}-journal"))
            {
                RestoreDatabase();
            }

            return;
        }

        _ = Directory.CreateDirectory(Utils.ConfigPath);

        using SqliteConnection connection = DBUtils.CreateDBConnection(s_configsPath);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS profile
            (
                id INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE
            ) STRICT;

            CREATE TABLE IF NOT EXISTS setting
            (
                profile_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                value TEXT NOT NULL,
                PRIMARY KEY (name, profile_id),
                FOREIGN KEY (profile_id) REFERENCES profile (id) ON DELETE CASCADE
            ) WITHOUT ROWID, STRICT;

            CREATE TABLE IF NOT EXISTS stats
            (
                profile_id INTEGER NOT NULL PRIMARY KEY,
                value TEXT NOT NULL,
                FOREIGN KEY (profile_id) REFERENCES profile (id) ON DELETE CASCADE
            ) STRICT;
            """;
        _ = command.ExecuteNonQuery();

        ProfileDBUtils.InsertDefaultProfile(connection);
        StatsDBUtils.InsertStats(connection, Stats.LifetimeStats, ProfileUtils.DefaultProfileId);
    }

    private static void RestoreDatabase()
    {
        using SqliteConnection connection = CreateReadWriteDBConnection();

        // Simply opening a connection does not actually access the database file
        // In order to trigger the restoration process we need to actually retrieve something from the database
        // https://www.sqlite.org/atomiccommit.html#_hot_rollback_journals
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM profile LIMIT 1";
        _ = command.ExecuteNonQuery();
    }

    public static SqliteConnection CreateReadOnlyDBConnection()
    {
        return DBUtils.CreateReadOnlyDBConnection(s_configsPath);
    }

    public static SqliteConnection CreateReadWriteDBConnection()
    {
        return DBUtils.CreateReadWriteDBConnection(s_configsPath);
    }

    public static void InsertSetting(SqliteConnection connection, string settingName, string value, int? profileId = null)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO setting (profile_id, name, value)
            VALUES (@profileId, @name, @value);
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId ?? ProfileUtils.CurrentProfileId);
        _ = command.Parameters.AddWithValue("@name", settingName);
        _ = command.Parameters.AddWithValue("@value", value);
        _ = command.ExecuteNonQuery();
    }

    public static void UpdateSetting(SqliteConnection connection, string settingName, string value, int? profileId = null)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE setting
            SET value = @value
            WHERE profile_id = @profileId AND name = @name;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId ?? ProfileUtils.CurrentProfileId);
        _ = command.Parameters.AddWithValue("@name", settingName);
        _ = command.Parameters.AddWithValue("@value", value);
        _ = command.ExecuteNonQuery();
    }

    public static void DeleteAllSettingsFromProfile(params string[] excludedSettings)
    {
        using SqliteConnection connection = CreateReadWriteDBConnection();
        using SqliteCommand command = connection.CreateCommand();

        string parameter = DBUtils.GetParameter(excludedSettings.Length + 1);

        string query = string.Create(CultureInfo.InvariantCulture,
            $"""
            DELETE FROM setting
            WHERE profile_id = @profileId AND name NOT IN {parameter}
            """);

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = query;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.Parameters.AddWithValue("@profileId", ProfileUtils.CurrentProfileId);
        _ = command.Parameters.AddWithValue("@1", nameof(ProfileUtils.CurrentProfileId));

        for (int i = 0; i < excludedSettings.Length; i++)
        {
            _ = command.Parameters.AddWithValue($"@{i + 2}", excludedSettings[i]);
        }

        _ = command.ExecuteNonQuery();
    }

    public static string? GetSettingValue(SqliteConnection connection, string settingName, int? profileId = null)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT value
            FROM setting
            WHERE profile_id = @profileId AND name = @name;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId ?? ProfileUtils.CurrentProfileId);
        _ = command.Parameters.AddWithValue("@name", settingName);

        return (string?)command.ExecuteScalar();
    }

    public static void CopyProfileSettings(SqliteConnection connection, int sourceProfileId, int targetProfileId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO setting (profile_id, name, value)
            SELECT @targetProfileId, name, value
            FROM setting
            WHERE profile_id = @sourceProfileId
            """;

        _ = command.Parameters.AddWithValue("@sourceProfileId", sourceProfileId);
        _ = command.Parameters.AddWithValue("@targetProfileId", targetProfileId);
        _ = command.ExecuteNonQuery();
    }

    public static T GetValueFromConfig<T>(SqliteConnection connection, T defaultValue, string configKey, TryParseHandler<T> tryParseHandler) where T : struct
    {
        string? configValue = GetSettingValue(connection, configKey);
        if (configValue is not null && tryParseHandler(configValue, out T value))
        {
            return value;
        }

        if (configValue is null)
        {
            InsertSetting(connection, configKey, Convert.ToString(defaultValue, CultureInfo.InvariantCulture)!);
        }
        else
        {
            UpdateSetting(connection, configKey, Convert.ToString(defaultValue, CultureInfo.InvariantCulture)!);
        }

        return defaultValue;
    }

    public static string GetValueFromConfig(SqliteConnection connection, string defaultValue, string configKey)
    {
        string? configValue = GetSettingValue(connection, configKey);
        if (configValue is not null)
        {
            return configValue;
        }

        InsertSetting(connection, configKey, defaultValue);
        return defaultValue;
    }

    public static T GetNumberWithDecimalPointFromConfig<T>(SqliteConnection connection, T defaultValue, string configKey, TryParseHandlerWithCultureInfo<T> tryParseHandler) where T : struct
    {
        string? configValue = GetSettingValue(connection, configKey);
        if (configValue is not null && tryParseHandler(configValue, NumberStyles.Number, CultureInfo.InvariantCulture, out T value))
        {
            return value;
        }

        if (configValue is null)
        {
            InsertSetting(connection, configKey, Convert.ToString(defaultValue, CultureInfo.InvariantCulture)!);
        }
        else
        {
            UpdateSetting(connection, configKey, Convert.ToString(defaultValue, CultureInfo.InvariantCulture)!);
        }

        return defaultValue;
    }

    internal static void SendOptimizePragma()
    {
        DBUtils.SendOptimizePragma(s_configsPath);
    }

    public static void AnalyzeAndVacuum(SqliteConnection connection)
    {
        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }
}
