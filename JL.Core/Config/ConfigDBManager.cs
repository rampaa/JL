using System.Diagnostics;
using System.Globalization;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;

public static class ConfigDBManager
{
    private const string GetAllSettingsQuery =
        """
        SELECT name, value
        FROM setting
        WHERE profile_id = @profileId;
        """;

    private const string GetAllSettingsCountQuery =
        """
        SELECT COUNT(*)
        FROM setting
        WHERE profile_id = @profileId;
        """;

    private const string UpdateSettingQuery =
        """
        UPDATE setting
        SET value = @value
        WHERE profile_id = @profileId AND name = @name;
        """;

    private static readonly string s_configsPath = Path.Join(Utils.ConfigPath, "Configs.sqlite");

    public static void CreateDB()
    {
        bool dbExists = File.Exists(s_configsPath);
        if (dbExists)
        {
            if (File.Exists($"{s_configsPath}-journal"))
            {
                RestoreDatabase();
            }
        }
        else
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
        }

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
                PRIMARY KEY (profile_id, name),
                FOREIGN KEY (profile_id) REFERENCES profile (id) ON DELETE CASCADE
            ) WITHOUT ROWID, STRICT;

            CREATE TABLE IF NOT EXISTS stats
            (
                profile_id INTEGER NOT NULL PRIMARY KEY,
                value TEXT NOT NULL,
                FOREIGN KEY (profile_id) REFERENCES profile (id) ON DELETE CASCADE
            ) STRICT;

            CREATE TABLE IF NOT EXISTS term_lookup_count
            (
                profile_id INTEGER NOT NULL,
                term TEXT NOT NULL,
                count INTEGER NOT NULL,
                PRIMARY KEY (profile_id, term),
                FOREIGN KEY (profile_id) REFERENCES profile (id) ON DELETE CASCADE
            ) STRICT;
            """;
        _ = command.ExecuteNonQuery();

        if (dbExists && NeedToMigrate(connection))
        {
            Migrate(connection);
        }

        bool globalProfileExists = ProfileDBUtils.ProfileExists(connection, ProfileUtils.GlobalProfileId);
        bool defaultProfileExists = ProfileDBUtils.ProfileExists(connection, ProfileUtils.DefaultProfileId);

        if (!globalProfileExists)
        {
            if (defaultProfileExists)
            {
                ProfileUtils.CurrentProfileId = ProfileDBUtils.GetCurrentProfileIdFromDB(connection, ProfileUtils.DefaultProfileId);
                StatsUtils.LifetimeStats = StatsDBUtils.GetStatsFromDB(connection, ProfileUtils.DefaultProfileId);
            }

            ProfileDBUtils.InsertGlobalProfile(connection);
            StatsDBUtils.InsertStats(connection, StatsUtils.LifetimeStats, ProfileUtils.GlobalProfileId);
        }

        if (!defaultProfileExists && !ProfileDBUtils.ProfileExists(connection))
        {
            ProfileDBUtils.InsertDefaultProfile(connection);
            StatsDBUtils.InsertStats(connection, StatsUtils.ProfileLifetimeStats, ProfileUtils.CurrentProfileId);
        }
    }

    public static bool NeedToMigrate(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(setting);";
        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(reader.GetOrdinal("pk")) is 2;
    }

    public static void Migrate(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA foreign_keys = OFF;

            BEGIN TRANSACTION;

            CREATE TABLE setting_new
            (
                profile_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                value TEXT NOT NULL,
                PRIMARY KEY (profile_id, name),
                FOREIGN KEY (profile_id) REFERENCES profile (id) ON DELETE CASCADE
            ) WITHOUT ROWID, STRICT;

            INSERT INTO setting_new (profile_id, name, value)
            SELECT profile_id, name, value FROM setting;

            DROP TABLE setting;

            ALTER TABLE setting_new RENAME TO setting;

            COMMIT;

            PRAGMA foreign_keys = ON;
          """;

        _ = command.ExecuteNonQuery();
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

    public static void UpdateSetting(SqliteConnection connection, string settingName, string? value, int? profileId = null)
    {
        Debug.Assert(value is not null);

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = UpdateSettingQuery;
        _ = command.Parameters.AddWithValue("@profileId", profileId ?? ProfileUtils.CurrentProfileId);
        _ = command.Parameters.AddWithValue("@name", settingName);
        _ = command.Parameters.AddWithValue("@value", value);
        _ = command.ExecuteNonQuery();
    }

    public static void DeleteAllSettingsFromProfile(SqliteConnection connection, params ReadOnlySpan<string> excludedSettings)
    {
        using SqliteCommand command = connection.CreateCommand();

        string parameter = DBUtils.GetParameter(excludedSettings.Length + 1);

        string query =
            $"""
            DELETE FROM setting
            WHERE profile_id = @profileId AND name NOT IN {parameter}
            """;

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

    public static Dictionary<string, string> GetSettingValues(SqliteConnection connection, params ReadOnlySpan<string> settingNames)
    {
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText =
        $"""
        SELECT name, value
        FROM setting
        WHERE profile_id = @profileId AND name IN {DBUtils.GetParameter(settingNames.Length)}
        """;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.Parameters.AddWithValue("@profileId", ProfileUtils.CurrentProfileId);
        for (int i = 0; i < settingNames.Length; i++)
        {
            _ = command.Parameters.AddWithValue($"@{i + 1}", settingNames[i]);
        }

        using SqliteDataReader reader = command.ExecuteReader();
        Dictionary<string, string> settings = new(settingNames.Length, StringComparer.Ordinal);
        while (reader.Read())
        {
            settings.Add(reader.GetString(0), reader.GetString(1));
        }

        return settings;
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

    public static T GetValueFromConfig<T>(SqliteConnection connection, Dictionary<string, string> configs, T defaultValue, string configKey) where T : struct, IConvertible, IParsable<T>
    {
        if (configs.TryGetValue(configKey, out string? configValue) && T.TryParse(configValue, CultureInfo.InvariantCulture, out T value))
        {
            return value;
        }

        if (configValue is null)
        {
            InsertSetting(connection, configKey, defaultValue.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            UpdateSetting(connection, configKey, defaultValue.ToString(CultureInfo.InvariantCulture));
        }

        return defaultValue;
    }

    public static Dictionary<string, string> GetAllConfigs(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = GetAllSettingsQuery;
        _ = command.Parameters.AddWithValue("@profileId", ProfileUtils.CurrentProfileId);

        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.HasRows)
        {
            return [];
        }

        Dictionary<string, string> settings = GetAllSettingDictWithCapacity(connection);
        while (reader.Read())
        {
            settings.Add(reader.GetString(0), reader.GetString(1));
        }

        return settings;
    }

    private static Dictionary<string, string> GetAllSettingDictWithCapacity(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = GetAllSettingsCountQuery;
        _ = command.Parameters.AddWithValue("@profileId", ProfileUtils.CurrentProfileId);

        using SqliteDataReader countReader = command.ExecuteReader();
        _ = countReader.Read();
        int count = countReader.GetInt32(0);

        return new Dictionary<string, string>(count, StringComparer.Ordinal);
    }

    public static T GetValueEnumValueFromConfig<T>(SqliteConnection connection, Dictionary<string, string> configs, T defaultValue, string configKey) where T : struct, Enum
    {
        if (configs.TryGetValue(configKey, out string? configValue) && Enum.TryParse(configValue, out T value))
        {
            return value;
        }

        if (configValue is null)
        {
            InsertSetting(connection, configKey, defaultValue.ToString());
        }
        else
        {
            UpdateSetting(connection, configKey, defaultValue.ToString());
        }

        return defaultValue;
    }

    public static string GetValueFromConfig(SqliteConnection connection, Dictionary<string, string> configs, string defaultValue, string configKey)
    {
        if (configs.TryGetValue(configKey, out string? configValue))
        {
            return configValue;
        }

        InsertSetting(connection, configKey, defaultValue);
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
