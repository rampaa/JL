using System.Globalization;
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

    internal static void InsertGlobalProfile(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO profile (id, name)
            VALUES (@id, @name);
            """;

        _ = command.Parameters.AddWithValue("@id", ProfileUtils.GlobalProfileId);
        _ = command.Parameters.AddWithValue("@name", ProfileUtils.GlobalProfileName);
        _ = command.ExecuteNonQuery();

        ConfigDBManager.InsertSetting(connection, nameof(ProfileUtils.CurrentProfileId), ProfileUtils.CurrentProfileId.ToString(CultureInfo.InvariantCulture), ProfileUtils.GlobalProfileId);
    }

    internal static void InsertDefaultProfile(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO profile (id, name)
            VALUES (@id, @name);
            """;

        _ = command.Parameters.AddWithValue("@id", ProfileUtils.DefaultProfileId);
        _ = command.Parameters.AddWithValue("@name", ProfileUtils.DefaultProfileName);
        _ = command.ExecuteNonQuery();
    }

    internal static int GetCurrentProfileIdFromDB(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText =
            $"""
            SELECT value
            FROM setting
            WHERE profile_id = {profileId} AND name = '{nameof(ProfileUtils.CurrentProfileId)}';
            """;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        return Convert.ToInt32(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
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

        return Convert.ToInt32(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    public static List<string> GetProfileNames()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
        return GetProfileNames(connection);
    }

    public static List<string> GetProfileNames(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT name
            FROM profile
            WHERE id != 0;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();

        List<string> profiles = [];
        while (dataReader.Read())
        {
            profiles.Add(dataReader.GetString(0));
        }

        return profiles;
    }

    internal static bool ProfileExists(int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
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
        return Convert.ToBoolean(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    public static bool ProfileExists(string profileName)
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
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
        return Convert.ToBoolean(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
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

        return (string)command.ExecuteScalar()!;
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
