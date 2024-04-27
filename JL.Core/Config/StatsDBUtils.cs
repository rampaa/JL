using System.Text.Json;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;
public static class StatsDBUtils
{
    public static void InsertStats(Stats stats, int profileId)
    {
        InsertStats(JsonSerializer.Serialize(stats, Utils.s_jsoWithEnumConverterAndIndentation), profileId);
    }

    public static void InsertStats(string stats, int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateDBConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO stats (profile_id, value)
            VALUES (@profile_id, @stats);
            """;

        _ = command.Parameters.AddWithValue("@profile_id", profileId);
        _ = command.Parameters.AddWithValue("@stats", stats);
        _ = command.ExecuteNonQuery();
    }

    public static void UpdateStats(Stats stats, int profileId)
    {
        UpdateStats(JsonSerializer.Serialize(stats, Utils.s_jsoWithEnumConverterAndIndentation), profileId);
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

    public static Stats? GetStatsFromConfig(int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT value
            FROM stats
            WHERE profile_id = @profileId;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);
        string? statsValue = (string?)command.ExecuteScalar();

        return statsValue is not null
            ? JsonSerializer.Deserialize<Stats>(statsValue, Utils.s_jsoWithEnumConverter)
            : null;
    }
}
