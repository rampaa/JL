using System.Diagnostics;
using System.Text.Json;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using Microsoft.Data.Sqlite;

namespace JL.Core.Config;

public static class StatsDBUtils
{
    internal static readonly Lock s_statsDBLock = new();

    private static readonly byte[] s_statsQuery = TextUtils.s_utf8NoBom.GetBytes(
        $"""
        SELECT {ConfigDBManager.Value}
        FROM {ConfigDBManager.Stats}
        WHERE {ConfigDBManager.ProfileId} = @{ConfigDBManager.ProfileId};{"\0"}
        """);

    public static void InsertStats(SqliteConnection connection, Stats stats, int profileId)
    {
        InsertStats(connection, JsonSerializer.Serialize(stats, JsonOptions.s_jsoWithEnumConverterAndIndentation), profileId);
    }

    private static void InsertStats(SqliteConnection connection, string stats, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            INSERT INTO {ConfigDBManager.Stats} ({ConfigDBManager.ProfileId}, {ConfigDBManager.Value})
            VALUES (@{ConfigDBManager.ProfileId}, @{ConfigDBManager.Stats});
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.ProfileId}", profileId);
        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Stats}", stats);
        _ = command.ExecuteNonQuery();
    }

    private static void UpdateStats(SqliteConnection connection, string stats, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            $"""
            UPDATE {ConfigDBManager.Stats}
            SET {ConfigDBManager.Value} = @{ConfigDBManager.Value}
            WHERE {ConfigDBManager.ProfileId} = @{ConfigDBManager.ProfileId};
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.ProfileId}", profileId);
        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.Value}", stats);
        _ = command.ExecuteNonQuery();
    }

    private static void UpsertTermLookupCounts(SqliteConnection connection, Dictionary<string, int> lookupStats, int profileId)
    {
        if (lookupStats.Count is 0)
        {
            return;
        }

        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertOrUpdateLookupStatsCommand = connection.CreateCommand();
        insertOrUpdateLookupStatsCommand.CommandText =
            $"""
            INSERT INTO {ConfigDBManager.TermLookupCount} ({ConfigDBManager.ProfileId}, {ConfigDBManager.Term}, {ConfigDBManager.Count})
            VALUES (@{ConfigDBManager.ProfileId}, @{ConfigDBManager.Term}, @{ConfigDBManager.Count})
            ON CONFLICT ({ConfigDBManager.ProfileId}, {ConfigDBManager.Term})
            DO UPDATE SET {ConfigDBManager.Count} = {ConfigDBManager.TermLookupCount}.{ConfigDBManager.Count} + excluded.{ConfigDBManager.Count};
            """;

        _ = insertOrUpdateLookupStatsCommand.Parameters.AddWithValue($"@{ConfigDBManager.ProfileId}", profileId);

        SqliteParameter termParam = new($"@{ConfigDBManager.Term}", SqliteType.Text);
        SqliteParameter countParam = new($"@{ConfigDBManager.Count}", SqliteType.Integer);
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
        using SqliteRecordReader reader = new(connection, s_statsQuery);
        reader.Bind(1, profileId);
        bool hasRow = reader.Read();
        Debug.Assert(hasRow);
        Stats? stats = JsonSerializer.Deserialize<Stats>(reader.GetStringSpan(0), JsonOptions.s_jsoWithEnumConverter);
        Debug.Assert(stats is not null);
        return stats;
    }

    public static List<KeyValuePair<string, int>>? GetTermLookupCountsFromDB(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT {ConfigDBManager.Term}, {ConfigDBManager.Count}
            FROM {ConfigDBManager.TermLookupCount}
            WHERE {ConfigDBManager.ProfileId} = @{ConfigDBManager.ProfileId}
            ORDER BY {ConfigDBManager.Count} DESC;
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.ProfileId}", profileId);

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
        lock (s_statsDBLock)
        {
            Stats stats;
            string statsJson;
            lock (StatsUtils.TimeStatsLock)
            {
                stats = StatsUtils.LifetimeStats;
                statsJson = JsonSerializer.Serialize(stats, JsonOptions.s_jsoWithEnumConverterAndIndentation);
            }

            UpdateStats(connection, statsJson, ProfileUtils.GlobalProfileId);
            if (CoreConfigManager.Instance.TrackTermLookupCounts)
            {
                lock (StatsUtils.TermLookupCountsLock)
                {
                    UpsertTermLookupCounts(connection, stats.TermLookupCountDict, ProfileUtils.GlobalProfileId);
                    stats.TermLookupCountDict.Clear();
                }
            }
        }
    }

    public static void UpdateProfileLifetimeStats(SqliteConnection connection)
    {
        lock (s_statsDBLock)
        {
            Stats stats;
            string statsJson;
            int profileId;
            lock (StatsUtils.TimeStatsLock)
            {
                stats = StatsUtils.ProfileLifetimeStats;
                profileId = ProfileUtils.CurrentProfileId;
                statsJson = JsonSerializer.Serialize(stats, JsonOptions.s_jsoWithEnumConverterAndIndentation);
            }

            UpdateStats(connection, statsJson, profileId);
            if (CoreConfigManager.Instance.TrackTermLookupCounts)
            {
                lock (StatsUtils.TermLookupCountsLock)
                {
                    UpsertTermLookupCounts(connection, stats.TermLookupCountDict, profileId);
                    stats.TermLookupCountDict.Clear();
                }
            }
        }
    }

    public static void SetStatsFromDB(SqliteConnection connection)
    {
        Stats lifetimeStats = GetStatsFromDB(connection, ProfileUtils.GlobalProfileId);
        Stats profileLifetimeStats = GetStatsFromDB(connection, ProfileUtils.CurrentProfileId);
        lock (StatsUtils.TimeStatsLock)
        {
            lock (StatsUtils.TermLookupCountsLock)
            {
                StatsUtils.LifetimeStats = lifetimeStats;
                StatsUtils.ProfileLifetimeStats = profileLifetimeStats;
            }
        }
    }

    internal static void ResetAllTermLookupCounts(SqliteConnection connection, int profileId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            DELETE FROM {ConfigDBManager.TermLookupCount}
            WHERE {ConfigDBManager.ProfileId} = @{ConfigDBManager.ProfileId}
            """;

        _ = command.Parameters.AddWithValue($"@{ConfigDBManager.ProfileId}", profileId);
        _ = command.ExecuteNonQuery();
    }
}
