using System.Globalization;
using System.Text.Json;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;
internal class StatsDBUtils
{
    public static void InsertStats(string stats, int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateDBConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO stats (profile_id, stats)
            VALUES (@profile_id, @stats);
            """;

        _ = command.Parameters.AddWithValue("@profile_id", profileId);
        _ = command.Parameters.AddWithValue("@stats", stats);
        _ = command.ExecuteNonQuery();
    }

    public static void UpdateStats(string stats, int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateDBConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE stats
            SET value = @value
            WHERE profile_id = @profileId;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);
        _ = command.Parameters.AddWithValue("@value", stats);
        _ = command.ExecuteNonQuery();
    }

    public static bool StatsExists(int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM stats
                WHERE profile_id = @profileId
                LIMIT 1
            );
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);

        return Convert.ToBoolean(command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    public static Stats? GetStats(int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT value
            FROM stats
            WHERE WHERE profile_id = @profileId;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);
        string? statsValue = (string?)command.ExecuteScalar();

        return statsValue is not null
            ? JsonSerializer.Deserialize<Stats>(statsValue, Utils.s_jsoWithEnumConverter)
            : null;
    }


}
