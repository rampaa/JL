using System.Data;
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

        ConfigDBManager.InsertSetting(connection, nameof(ProfileUtils.CurrentProfileId), ProfileUtils.DefaultProfileId.ToString(CultureInfo.InvariantCulture), ProfileUtils.DefaultProfileId);
    }

    public static int GetCurrentProfileIdFromDB(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT value
            FROM setting
            WHERE profile_id = 1 AND name = '{nameof(ProfileUtils.CurrentProfileId)}';
            """;

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
        List<string> profiles = [];
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT id, name
            FROM profile;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            profiles.Add(dataReader.GetString("name"));
        }

        return profiles;
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

    public static string GetProfileName(SqliteConnection connection, int profileId)
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
        ConfigDBManager.UpdateSetting(connection, nameof(ProfileUtils.CurrentProfileId), ProfileUtils.CurrentProfileId.ToString(CultureInfo.InvariantCulture), ProfileUtils.DefaultProfileId);
    }

    public static void SetCurrentProfileFromDB(SqliteConnection connection)
    {
        ProfileUtils.CurrentProfileId = GetCurrentProfileIdFromDB(connection);
        ProfileUtils.CurrentProfileName = GetProfileName(connection, ProfileUtils.CurrentProfileId);
    }
}
