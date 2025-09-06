using System.Diagnostics;
using System.Text.Json;
using JL.Core.Statistics;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;

public static class StatsDBUtils
{
    public static void InsertStats(SqliteConnection connection, Stats stats, int profileId)
    {
        InsertStats(connection, JsonSerializer.Serialize(stats, JsonOptions.s_jsoWithEnumConverterAndIndentation), profileId);
    }

    private static void InsertStats(SqliteConnection connection, string stats, int profileId)
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
        UpdateStats(connection, JsonSerializer.Serialize(stats, JsonOptions.s_jsoWithEnumConverterAndIndentation), profileId);
    }

    private static void UpdateStats(SqliteConnection connection, string stats, int profileId)
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

    private static void UpsertTermLookupCounts(SqliteConnection connection, Dictionary<string, int> lookupStats, int profileId)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertOrUpdateLookupStatsCommand = connection.CreateCommand();
        insertOrUpdateLookupStatsCommand.CommandText =
            """
            INSERT INTO term_lookup_count (profile_id, term, count)
            VALUES (@profile_id, @term, @count)
            ON CONFLICT (profile_id, term)
            DO UPDATE SET count = term_lookup_count.count + excluded.count;
            """;

        _ = insertOrUpdateLookupStatsCommand.Parameters.AddWithValue("@profile_id", profileId);

        SqliteParameter termParam = new("@term", SqliteType.Text);
        SqliteParameter countParam = new("@count", SqliteType.Integer);
        insertOrUpdateLookupStatsCommand.Parameters.AddRange([termParam, countParam]);
        insertOrUpdateLookupStatsCommand.Prepare();

        foreach ((string term, int value) in lookupStats)
        {
            termParam.Value = term;
            countParam.Value = value;
            _ = insertOrUpdateLookupStatsCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static Stats GetStatsFromDB(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT value
            FROM stats
            WHERE profile_id = @profileId;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);

        using SqliteDataReader reader = command.ExecuteReader();

        Debug.Assert(reader.HasRows);

        _ = reader.Read();
        Stats? stats = JsonSerializer.Deserialize<Stats>(reader.GetString(0), JsonOptions.s_jsoWithEnumConverter);
        Debug.Assert(stats is not null);
        return stats;
    }

    public static List<KeyValuePair<string, int>>? GetTermLookupCountsFromDB(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT term, count
            FROM term_lookup_count
            WHERE profile_id = @profileId
            ORDER BY count DESC;
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        List<KeyValuePair<string, int>> termLookupCounts = [];
        while (dataReader.Read())
        {
            string term = dataReader.GetString(0);
            int count = dataReader.GetInt32(1);

            termLookupCounts.Add(KeyValuePair.Create(term, count));
        }

        return termLookupCounts;
    }

    public static void UpdateLifetimeStats(SqliteConnection connection)
    {
        UpdateStats(connection, StatsUtils.LifetimeStats, ProfileUtils.GlobalProfileId);
        if (CoreConfigManager.Instance.TrackTermLookupCounts)
        {
            UpsertTermLookupCounts(connection, StatsUtils.LifetimeStats.TermLookupCountDict, ProfileUtils.GlobalProfileId);
            StatsUtils.LifetimeStats.TermLookupCountDict.Clear();
        }
    }

    public static void UpdateProfileLifetimeStats(SqliteConnection connection)
    {
        UpdateStats(connection, StatsUtils.ProfileLifetimeStats, ProfileUtils.CurrentProfileId);
        if (CoreConfigManager.Instance.TrackTermLookupCounts)
        {
            UpsertTermLookupCounts(connection, StatsUtils.ProfileLifetimeStats.TermLookupCountDict, ProfileUtils.CurrentProfileId);
            StatsUtils.ProfileLifetimeStats.TermLookupCountDict.Clear();
        }
    }

    public static void SetStatsFromDB(SqliteConnection connection)
    {
        StatsUtils.LifetimeStats = GetStatsFromDB(connection, ProfileUtils.GlobalProfileId);
        StatsUtils.ProfileLifetimeStats = GetStatsFromDB(connection, ProfileUtils.CurrentProfileId);
    }

    internal static void ResetAllTermLookupCounts(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            DELETE FROM term_lookup_count
            WHERE profile_id = @profileId
            """;

        _ = command.Parameters.AddWithValue("@profileId", profileId);
        _ = command.ExecuteNonQuery();
    }
}
