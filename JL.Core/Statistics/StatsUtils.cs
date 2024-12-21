using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Timers;
using JL.Core.Config;
using Microsoft.Data.Sqlite;
using Timer = System.Timers.Timer;

namespace JL.Core.Statistics;

public static class StatsUtils
{
    [JsonIgnore] public static Stats SessionStats { get; } = new();
    [JsonIgnore] public static Stats ProfileLifetimeStats { get; set; } = new();
    [JsonIgnore] public static Stats LifetimeStats { get; internal set; } = new();

    public static Stopwatch StatsStopWatch { get; } = new();
    private static Timer StatsTimer { get; } = new();

    public static void StartStatsTimer()
    {
        if (!StatsTimer.Enabled)
        {
            StatsTimer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
            StatsTimer.Elapsed += OnTimedEvent;
            StatsTimer.AutoReset = true;
            StatsTimer.Enabled = true;
        }
    }

    public static void StopStatsTimer()
    {
        StatsTimer.Enabled = false;
    }

    private static void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        IncrementStat(StatType.Time, StatsStopWatch.ElapsedTicks);

        if (StatsStopWatch.IsRunning)
        {
            StatsStopWatch.Restart();
        }

        else
        {
            StatsStopWatch.Reset();
        }

        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        StatsDBUtils.UpdateLifetimeStats(connection);
        StatsDBUtils.UpdateProfileLifetimeStats(connection);
    }

    public static void IncrementStat(StatType type, long amount = 1)
    {
        switch (type)
        {
            case StatType.Characters:
            {
                bool positive = amount >= 0;
                ulong unsignedAmount = positive
                    ? (ulong)amount
                    : (ulong)-amount;

                if (positive)
                {
                    SessionStats.Characters += unsignedAmount;
                    ProfileLifetimeStats.Characters += unsignedAmount;
                    LifetimeStats.Characters += unsignedAmount;
                }
                else
                {
                    SessionStats.Characters -= unsignedAmount;
                    ProfileLifetimeStats.Characters -= unsignedAmount;
                    LifetimeStats.Characters -= unsignedAmount;
                }

                break;
            }

            case StatType.Lines:
            {
                bool positive = amount >= 0;
                ulong unsignedAmount = positive
                    ? (ulong)amount
                    : (ulong)-amount;

                if (positive)
                {
                    SessionStats.Lines += unsignedAmount;
                    ProfileLifetimeStats.Lines += unsignedAmount;
                    LifetimeStats.Lines += unsignedAmount;
                }
                else
                {
                    SessionStats.Lines -= unsignedAmount;
                    ProfileLifetimeStats.Lines -= unsignedAmount;
                    LifetimeStats.Lines -= unsignedAmount;
                }

                break;
            }

            case StatType.Time:
            {
                SessionStats.Time = SessionStats.Time.Add(TimeSpan.FromTicks(amount));
                ProfileLifetimeStats.Time = ProfileLifetimeStats.Time.Add(TimeSpan.FromTicks(amount));
                LifetimeStats.Time = LifetimeStats.Time.Add(TimeSpan.FromTicks(amount));

                break;
            }

            case StatType.CardsMined:
            {
                ulong unsignedAmount = (ulong)amount;
                SessionStats.CardsMined += unsignedAmount;
                ProfileLifetimeStats.CardsMined += unsignedAmount;
                LifetimeStats.CardsMined += unsignedAmount;

                break;
            }

            case StatType.TimesPlayedAudio:
            {
                ulong unsignedAmount = (ulong)amount;
                SessionStats.TimesPlayedAudio += unsignedAmount;
                ProfileLifetimeStats.TimesPlayedAudio += unsignedAmount;
                LifetimeStats.TimesPlayedAudio += unsignedAmount;

                break;
            }

            case StatType.NumberOfLookups:
            {
                ulong unsignedAmount = (ulong)amount;
                SessionStats.NumberOfLookups += unsignedAmount;
                ProfileLifetimeStats.NumberOfLookups += unsignedAmount;
                LifetimeStats.NumberOfLookups += unsignedAmount;

                break;
            }

            case StatType.Imoutos:
            {
                ulong unsignedAmount = (ulong)amount;
                SessionStats.Imoutos += unsignedAmount;
                ProfileLifetimeStats.Imoutos += unsignedAmount;
                LifetimeStats.Imoutos += unsignedAmount;

                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid StatType");
        }
    }

    public static void ResetStats(StatsMode statsMode)
    {
        Stats stats = statsMode switch
        {
            StatsMode.Lifetime => LifetimeStats,
            StatsMode.Profile => ProfileLifetimeStats,
            StatsMode.Session => SessionStats,
            _ => throw new ArgumentOutOfRangeException(nameof(statsMode), statsMode, "Invalid StatsMode")
        };

        stats.Characters = 0;
        stats.Lines = 0;
        stats.Time = TimeSpan.Zero;
        stats.CardsMined = 0;
        stats.TimesPlayedAudio = 0;
        stats.Imoutos = 0;

        stats.TermLookupCountDict.Clear();
        if (statsMode is StatsMode.Profile or StatsMode.Lifetime)
        {
            StatsDBUtils.ResetAllTermLookupCounts(statsMode is StatsMode.Profile ? ProfileUtils.CurrentProfileId : ProfileUtils.GlobalProfileId);
        }
    }

    public static void IncrementTermLookupCount(string primarySpelling)
    {
        IncrementLookupStat(SessionStats, primarySpelling);
        IncrementLookupStat(ProfileLifetimeStats, primarySpelling);
        IncrementLookupStat(LifetimeStats, primarySpelling);
    }

    private static void IncrementLookupStat(Stats stats, string primarySpelling)
    {
        Dictionary<string, int> lookupStatsDict = stats.TermLookupCountDict;
        if (lookupStatsDict.TryGetValue(primarySpelling, out int count))
        {
            lookupStatsDict[primarySpelling] = count + 1;
        }
        else
        {
            lookupStatsDict[primarySpelling] = 1;
        }
    }
}
