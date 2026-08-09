using System.Diagnostics;
using System.Globalization;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;

public static class ProfileDBUtils
{
    public static void InsertProfile(SqliteConnection connection, string profileName)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            INSERT INTO {ConfigDBManager.Profile} ({ConfigDBManager.Name})
            VALUES (@{ConfigDBManager.Name});
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Name}", profileName);
        _ = command.ExecuteNonQuery();
    }

    internal static void InsertProfile(SqliteConnection connection, string profileName, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            INSERT INTO {ConfigDBManager.Profile} ({ConfigDBManager.Id}, {ConfigDBManager.Name})
            VALUES (@{ConfigDBManager.Id}, @{ConfigDBManager.Name});
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Id}", profileId);
        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Name}", profileName);
        _ = command.ExecuteNonQuery();
    }

    internal static int GetCurrentProfileIdFromDB(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT {ConfigDBManager.Value}
            FROM {ConfigDBManager.Setting}
            WHERE {ConfigDBManager.ProfileId} = @{ConfigDBManager.ProfileId} AND {ConfigDBManager.Name} = @{ConfigDBManager.Name};
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.ProfileId}", profileId);
        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Name}", nameof(ProfileUtils.CurrentProfileId));

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static int GetProfileId(SqliteConnection connection, string profileName)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT {ConfigDBManager.Id}
            FROM {ConfigDBManager.Profile}
            WHERE {ConfigDBManager.Name} = @{ConfigDBManager.Name};
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Name}", profileName);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static ReadOnlySpan<string> GetProfileNames()
    {
        using SqliteConnection? connection = ConfigDBManager.CreateReadOnlyDBConnection();
        Debug.Assert(connection is not null);
        return GetProfileNames(connection).AsReadOnlySpan();
    }

    public static List<string> GetProfileNames(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT {ConfigDBManager.Name}
            FROM {ConfigDBManager.Profile}
            WHERE {ConfigDBManager.Id} != 0
            ORDER BY {ConfigDBManager.Name} ASC;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();

        List<string> profiles = [];
        while (dataReader.Read())
        {
            profiles.Add(dataReader.GetString(0));
        }

        return profiles;
    }

    internal static List<int> GetProfileIds(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT {ConfigDBManager.Id}
            FROM {ConfigDBManager.Profile};
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();

        List<int> profiles = [];
        while (dataReader.Read())
        {
            profiles.Add(dataReader.GetInt32(0));
        }

        return profiles;
    }

    internal static bool ProfileExists(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM {ConfigDBManager.Profile}
                WHERE id != 0
            );
            """;

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetBoolean(0);
    }

    internal static bool ProfileExists(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM {ConfigDBManager.Profile}
                WHERE {ConfigDBManager.Id} = @{ConfigDBManager.Id}
            );
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Id}", profileId);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetBoolean(0);
    }

    internal static bool ProfileExists(SqliteConnection connection, string profileName)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT EXISTS
            (
                SELECT 1
                FROM {ConfigDBManager.Profile}
                WHERE {ConfigDBManager.Name} = @{ConfigDBManager.Name}
            );
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Name}", profileName);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetBoolean(0);
    }

    public static bool ProfileExists(string profileName)
    {
        using SqliteConnection? connection = ConfigDBManager.CreateReadOnlyDBConnection();
        Debug.Assert(connection is not null);
        return ProfileExists(connection, profileName);
    }

    private static string GetProfileName(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT {ConfigDBManager.Name}
            FROM {ConfigDBManager.Profile}
            WHERE {ConfigDBManager.Id} = @{ConfigDBManager.Id};
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Id}", profileId);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetString(0);
    }

    public static void DeleteProfile(SqliteConnection connection, string profileName)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            DELETE FROM {ConfigDBManager.Profile}
            WHERE {ConfigDBManager.Name} = @{ConfigDBManager.Name}
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Name}", profileName);
        _ = command.ExecuteNonQuery();
    }

    public static void UpdateCurrentProfile(SqliteConnection connection)
    {
        ConfigDBManager.UpdateSetting(connection, nameof(ProfileUtils.CurrentProfileId), ProfileUtils.CurrentProfileId.ToString(CultureInfo.InvariantCulture), ProfileUtils.GlobalProfileId);
    }

    public static void SetCurrentProfileFromDB(SqliteConnection connection)
    {
        ProfileUtils.CurrentProfileId = GetCurrentProfileIdFromDB(connection, ProfileUtils.GlobalProfileId);
        ProfileUtils.CurrentProfileName = GetProfileName(connection, ProfileUtils.CurrentProfileId);
        ProfileUtils.CurrentProfileSessionStartTime = DateTime.Now;
    }
}
