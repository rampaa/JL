using System.Globalization;
using JL.Core.Profile;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;
public static class ConfigDBManager
{
    public static readonly string ConfigsPath = Path.Join(Utils.ConfigPath, "Configs.sqlite");
    public delegate bool TryParseHandler<T>(string value, out T? result);
    public delegate bool TryParseHandlerWithCultureInfo<T>(string value, NumberStyles numberStyles, CultureInfo cultureInfo, out T result);

    public static void CreateDB()
    {
        if (File.Exists(ConfigsPath))
        {
            return;
        }

        using SqliteConnection connection = new($"Data Source={ConfigsPath};");
        connection.Open();
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
    }

    public static SqliteConnection CreateReadOnlyDBConnection()
    {
        SqliteConnection connection = new($"Data Source={ConfigsPath};Mode=ReadOnly;");
        connection.Open();
        return connection;
    }

    public static SqliteConnection CreateDBConnection()
    {
        SqliteConnection connection = new($"Data Source={ConfigsPath};Mode=ReadWrite;");
        connection.Open();
        return connection;
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

    public static void CopyProfileSettings(int sourceProfileId, int targetProfileId)
    {
        using SqliteConnection connection = CreateDBConnection();
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

    public static T GetValueFromConfig<T>(SqliteConnection connection, T variable, string configKey, TryParseHandler<T> tryParseHandler) where T : struct
    {
        string? configValue = GetSettingValue(connection, configKey);
        if (configValue is not null && tryParseHandler(configValue, out T value))
        {
            return value;
        }

        if (configValue is null)
        {
            InsertSetting(connection, configKey, Convert.ToString(variable, CultureInfo.InvariantCulture)!);
        }
        else
        {
            UpdateSetting(connection, configKey, Convert.ToString(variable, CultureInfo.InvariantCulture)!);
        }

        return variable;
    }

    public static T GetNumberWithDecimalPointFromConfig<T>(SqliteConnection connection, T number, string configKey, TryParseHandlerWithCultureInfo<T> tryParseHandler) where T : struct
    {
        string? configValue = GetSettingValue(connection, configKey);
        if (configValue is not null && tryParseHandler(configValue, NumberStyles.Number, CultureInfo.InvariantCulture, out T value))
        {
            return value;
        }

        if (configValue is null)
        {
            InsertSetting(connection, configKey, Convert.ToString(number, CultureInfo.InvariantCulture)!);
        }
        else
        {
            UpdateSetting(connection, configKey, Convert.ToString(number, CultureInfo.InvariantCulture)!);
        }

        return number;
    }

    public static void OptimizeAnalyzeAndVacuum(SqliteConnection connection)
    {
        using SqliteCommand optimizeCommand = connection.CreateCommand();
        optimizeCommand.CommandText = "PRAGMA optimize;";
        _ = optimizeCommand.ExecuteNonQuery();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }
}
