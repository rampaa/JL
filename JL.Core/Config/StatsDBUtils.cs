using System.Data;
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
        UpdateStats(connection, JsonSerializer.Serialize(stats, Utils.s_jsoNotIgnoringNullWithEnumConverterAndIndentation), profileId);
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
            DO UPDATE SET count = term_lookup_count.count + 1;
            """;

        _ = insertOrUpdateLookupStatsCommand.Parameters.AddWithValue("@profile_id", profileId);
        _ = insertOrUpdateLookupStatsCommand.Parameters.Add("@term", SqliteType.Text);
        _ = insertOrUpdateLookupStatsCommand.Parameters.Add("@count", SqliteType.Integer);
        insertOrUpdateLookupStatsCommand.Prepare();

        foreach ((string term, int value) in lookupStats)
        {
            insertOrUpdateLookupStatsCommand.Parameters["@term"].Value = term;
            insertOrUpdateLookupStatsCommand.Parameters["@count"].Value = value;
            _ = insertOrUpdateLookupStatsCommand.ExecuteNonQuery();
        }

        transaction.Commit();
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

        SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        List<KeyValuePair<string, int>> termLookupCounts = [];
        while (dataReader.Read())
        {
            string term = dataReader.GetString(nameof(term));
            int count = dataReader.GetInt32(nameof(count));

            termLookupCounts.Add(KeyValuePair.Create(term, count));
        }

        return termLookupCounts;
    }

    public static void UpdateLifetimeStats()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        UpdateLifetimeStats(connection);
    }

    public static void UpdateLifetimeStats(SqliteConnection connection)
    {
        UpdateStats(connection, Stats.LifetimeStats, ProfileUtils.GlobalProfileId);
        if (CoreConfigManager.Instance.TrackTermLookupCounts)
        {
            UpsertTermLookupCounts(connection, Stats.LifetimeStats.TermLookupCountDict, ProfileUtils.GlobalProfileId);
            Stats.LifetimeStats.TermLookupCountDict.Clear();
        }
    }

    public static void UpdateProfileLifetimeStats()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        UpdateProfileLifetimeStats(connection);
    }

    public static void UpdateProfileLifetimeStats(SqliteConnection connection)
    {
        UpdateStats(connection, Stats.ProfileLifetimeStats, ProfileUtils.CurrentProfileId);
        if (CoreConfigManager.Instance.TrackTermLookupCounts)
        {
            UpsertTermLookupCounts(connection, Stats.ProfileLifetimeStats.TermLookupCountDict, ProfileUtils.CurrentProfileId);
            Stats.ProfileLifetimeStats.TermLookupCountDict.Clear();
        }
    }

    public static void SetStatsFromDB(SqliteConnection connection)
    {
        Stats.LifetimeStats = GetStatsFromDB(connection, ProfileUtils.GlobalProfileId)!;
        Stats.ProfileLifetimeStats = GetStatsFromDB(connection, ProfileUtils.CurrentProfileId)!;
    }

    internal static void ResetAllTermLookupCounts(int profileId)
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
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
