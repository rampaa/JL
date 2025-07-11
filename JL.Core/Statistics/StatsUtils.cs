using System.Diagnostics;
using System.Timers;
using JL.Core.Config;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;
using Timer = System.Timers.Timer;

namespace JL.Core.Statistics;

public static class StatsUtils
{
    public static Stats SessionStats { get; } = new();
    public static Stats ProfileLifetimeStats { get; set; } = new();
    public static Stats LifetimeStats { get; internal set; } = new();

    public static Stopwatch TimeStatStopWatch { get; } = new();
    private static Timer StatsTimer { get; } = new();
    private static Timer IdleTimeTimer { get; } = new()
    {
        AutoReset = false
    };

    private static int s_textLength; // = 0

    static StatsUtils()
    {
        IdleTimeTimer.Elapsed += IdleTimeTimer_OnTimedEvent;
        StatsTimer.Elapsed += StatsTimer_OnTimedEvent;
    }

    internal static void InitializeStatsTimer()
    {
        StatsTimer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
        StatsTimer.AutoReset = true;
        StatsTimer.Enabled = true;
    }

    public static void InitializeIdleTimeTimer()
    {
        SetIdleTimeTimerInterval(s_textLength);
    }

    public static void SetIdleTimeTimerInterval(int textLength)
    {
        s_textLength = textLength;
        double minReadingSpeedThreshold = CoreConfigManager.Instance.MinCharactersPerMinuteBeforeStoppingTimeTracking;
        if (minReadingSpeedThreshold > 0 && textLength > 0 && TimeStatStopWatch.IsRunning)
        {
            IdleTimeTimer.Interval = TimeSpan.FromMinutes(textLength / minReadingSpeedThreshold).TotalMilliseconds;
            IdleTimeTimer.Enabled = true;
        }
        else
        {
            IdleTimeTimer.Enabled = false;
        }
    }

    public static void StartTimeStatStopWatch()
    {
        TimeStatStopWatch.Start();

        // Restarts the timer
        // This is faster than setting the Enabled property to false and then true
        IdleTimeTimer.Interval = IdleTimeTimer.Interval;
        IdleTimeTimer.Enabled = true;
    }

    public static void StopTimeStatStopWatch()
    {
        TimeStatStopWatch.Stop();
        IdleTimeTimer.Enabled = false;
    }

    public static void StopIdleItemTimer()
    {
        IdleTimeTimer.Enabled = false;
    }

    private static void IdleTimeTimer_OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        if (TimeStatStopWatch.IsRunning)
        {
            IncrementStat(StatType.Time, TimeStatStopWatch.ElapsedTicks);
            TimeStatStopWatch.Reset();
        }
    }

    private static void StatsTimer_OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        IncrementStat(StatType.Time, TimeStatStopWatch.ElapsedTicks);

        if (TimeStatStopWatch.IsRunning)
        {
            TimeStatStopWatch.Restart();
        }

        else
        {
            TimeStatStopWatch.Reset();
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
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(StatType), nameof(StatsUtils), nameof(IncrementStat), type);
                Utils.Frontend.Alert(AlertLevel.Error, $"Invalid stat type: {type}");
                break;
        }
    }

    public static void ResetStats(SqliteConnection connection, StatsMode statsMode)
    {
        Stats stats = statsMode switch
        {
            StatsMode.Lifetime => LifetimeStats,
            StatsMode.Profile => ProfileLifetimeStats,
            StatsMode.Session => SessionStats,
            _ => SessionStats
        };

        stats.ResetStats();

        if (statsMode is StatsMode.Profile or StatsMode.Lifetime)
        {
            StatsDBUtils.ResetAllTermLookupCounts(connection, statsMode is StatsMode.Profile ? ProfileUtils.CurrentProfileId : ProfileUtils.GlobalProfileId);
        }
    }

    public static void IncrementTermLookupCount(string deconjugatedMatchedText)
    {
        SessionStats.IncrementLookupStat(deconjugatedMatchedText);
        ProfileLifetimeStats.IncrementLookupStat(deconjugatedMatchedText);
        LifetimeStats.IncrementLookupStat(deconjugatedMatchedText);
    }
}
