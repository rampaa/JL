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
            """
            INSERT INTO profile (name)
            VALUES (@name);
            """;

        _ = command.Parameters.AddWithValue("@name", profileName);
        _ = command.ExecuteNonQuery();
    }

    internal static void InsertProfile(SqliteConnection connection, string profileName, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO profile (id, name)
            VALUES (@id, @name);
            """;

        _ = command.Parameters.AddWithValue("@id", profileId);
        _ = command.Parameters.AddWithValue("@name", profileName);
        _ = command.ExecuteNonQuery();
    }

    internal static int GetCurrentProfileIdFromDB(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT value
            FROM setting
            WHERE profile_id = @profileId AND name = @name;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);
        _ = command.Parameters.AddWithValue("@name", nameof(ProfileUtils.CurrentProfileId));

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static int GetProfileId(SqliteConnection connection, string profileName)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT id
            FROM profile
            WHERE name = @name;
            """;

        _ = command.Parameters.AddWithValue("@name", profileName);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetInt32(0);
    }

    public static ReadOnlySpan<string> GetProfileNames()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
        return GetProfileNames(connection).AsReadOnlySpan();
    }

    public static List<string> GetProfileNames(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT name
            FROM profile
            WHERE id != 0
            ORDER BY name ASC;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();

        List<string> profiles = [];
        while (dataReader.Read())
        {
            profiles.Add(dataReader.GetString(0));
        }

        return profiles;
    }

    internal static bool ProfileExists(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM profile
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
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM profile
                WHERE id = @profileId
            );
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetBoolean(0);
    }

    public static bool ProfileExists(SqliteConnection connection, string profileName)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM profile
                WHERE name = @name
            );
            """;

        _ = command.Parameters.AddWithValue("@name", profileName);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetBoolean(0);
    }

    public static bool ProfileExists(string profileName)
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
        return ProfileExists(connection, profileName);
    }

    private static string GetProfileName(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT name
            FROM profile
            WHERE id = @id;
            """;

        _ = command.Parameters.AddWithValue("@id", profileId);

        using SqliteDataReader reader = command.ExecuteReader();
        _ = reader.Read();
        return reader.GetString(0);
    }

    public static void DeleteProfile(SqliteConnection connection, string profileName)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            DELETE FROM profile
            WHERE name = @name
            """;

        _ = command.Parameters.AddWithValue("@name", profileName);
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
    }
}
