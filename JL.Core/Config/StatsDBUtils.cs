using System.Text.Json;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;

public static class StatsDBUtils
{
    public static void InsertStats(SqliteConnection connection, Stats stats, int profileId)
    {
        InsertStats(connection, JsonSerializer.Serialize(stats, Utils.s_jsoNotIgnoringNullWithEnumConverterAndIndentation), profileId);
    }

    public static void InsertStats(SqliteConnection connection, string stats, int profileId)
    {
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

    private static void UpdateStats(SqliteConnection connection, Stats stats, int profileId)
    {
        UpdateStats(connection, JsonSerializer.Serialize(stats, Utils.s_jsoNotIgnoringNullWithEnumConverterAndIndentation), profileId);
    }

    public static void UpdateStats(SqliteConnection connection, string stats, int profileId)
    {
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

    public static Stats? GetStatsFromDB(SqliteConnection connection, int profileId)
    {
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
            ? JsonSerializer.Deserialize<Stats>(statsValue, Utils.s_jsoNotIgnoringNullWithEnumConverter)
            : null;
    }

    public static void UpdateLifetimeStats()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        UpdateLifetimeStats(connection);
    }

    public static void UpdateLifetimeStats(SqliteConnection connection)
    {
        UpdateStats(connection, Stats.LifetimeStats, ProfileUtils.DefaultProfileId);
    }

    public static void UpdateProfileLifetimeStats()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        UpdateProfileLifetimeStats(connection);
    }

    public static void UpdateProfileLifetimeStats(SqliteConnection connection)
    {
        UpdateStats(connection, Stats.ProfileLifetimeStats, ProfileUtils.CurrentProfileId);
    }

    public static void SetStatsFromDB(SqliteConnection connection)
    {
        Stats.LifetimeStats = GetStatsFromDB(connection, ProfileUtils.DefaultProfileId)!;
        Stats.ProfileLifetimeStats = GetStatsFromDB(connection, ProfileUtils.CurrentProfileId)!;
    }
}
