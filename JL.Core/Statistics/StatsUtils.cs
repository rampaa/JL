using System.Diagnostics;
using System.Timers;
using JL.Core.Config;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;
using Timer = System.Timers.Timer;

namespace JL.Core.Statistics;

public static class StatsUtils
{
    public static readonly Stats SessionStats = new();
    public static Stats ProfileLifetimeStats { get; set; } = new();
    public static Stats LifetimeStats { get; internal set; } = new();

    internal static readonly Stopwatch s_timeStatStopWatch = new();
    public static readonly Lock TimeStatsLock = new();
    public static readonly Lock TermLookupCountsLock = new();
    private static readonly Timer s_statsTimer = new();
    private static readonly Timer s_idleTimeTimer = new()
    {
        AutoReset = false
    };

    private static long s_idleTimeDeadline; // = 0
    private static int s_textLength; // = 0

    static StatsUtils()
    {
        s_idleTimeTimer.Elapsed += IdleTimeTimer_OnTimedEvent;
        s_statsTimer.Elapsed += StatsTimer_OnTimedEvent;
    }

    internal static void InitializeStatsTimer()
    {
        s_statsTimer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
        s_statsTimer.AutoReset = true;
        s_statsTimer.Enabled = true;
    }

    public static void SetIdleTimeTimerInterval(int textLength)
    {
        lock (TimeStatsLock)
        {
            s_textLength = textLength;
            UpdateIdleTimeTimer();
        }
    }

    private static void UpdateIdleTimeTimer()
    {
        double minReadingSpeedThreshold = CoreConfigManager.Instance.MinCharactersPerMinuteBeforeStoppingTimeTracking;
        if (minReadingSpeedThreshold > 0 && s_textLength > 0 && s_timeStatStopWatch.IsRunning)
        {
            double interval = Math.Max(TimeSpan.FromMinutes(s_textLength / minReadingSpeedThreshold).TotalMilliseconds, 1500);
            s_idleTimeDeadline = Stopwatch.GetTimestamp() + (long)Math.Ceiling(interval * Stopwatch.Frequency / 1000);
            s_idleTimeTimer.Interval = interval;
            s_idleTimeTimer.Enabled = true;
        }
        else
        {
            s_idleTimeTimer.Enabled = false;
            s_idleTimeDeadline = 0;
        }
    }

    public static void StartTimeStatStopWatch(bool onlyIfStopped = false)
    {
        lock (TimeStatsLock)
        {
            if (onlyIfStopped && s_timeStatStopWatch.IsRunning)
            {
                return;
            }

            s_timeStatStopWatch.Start();
            UpdateIdleTimeTimer();
        }
    }

    public static void StopTimeStatStopWatch()
    {
        lock (TimeStatsLock)
        {
            s_timeStatStopWatch.Stop();
            s_idleTimeTimer.Enabled = false;
            s_idleTimeDeadline = 0;
        }
    }

    private static void IdleTimeTimer_OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        lock (TimeStatsLock)
        {
            if (!s_timeStatStopWatch.IsRunning || s_idleTimeDeadline is 0)
            {
                return;
            }

            // A queued callback can belong to an earlier timeout, before new text arrived.
            long remainingTicks = s_idleTimeDeadline - Stopwatch.GetTimestamp();
            if (remainingTicks > 0)
            {
                s_idleTimeTimer.Interval = Math.Max((double)remainingTicks * 1000 / Stopwatch.Frequency, 1);
                s_idleTimeTimer.Enabled = true;
                return;
            }

            IncrementTimeStats(s_timeStatStopWatch.ElapsedTicks);
            s_timeStatStopWatch.Reset();
            s_idleTimeTimer.Enabled = false;
            s_idleTimeDeadline = 0;
        }
    }

    public static void UpdateTimeStats(bool restartStopWatch)
    {
        lock (TimeStatsLock)
        {
            long elapsedTicks = s_timeStatStopWatch.ElapsedTicks;
            if (restartStopWatch && s_timeStatStopWatch.IsRunning)
            {
                s_timeStatStopWatch.Restart();
            }
            else
            {
                s_timeStatStopWatch.Reset();
                s_idleTimeTimer.Enabled = false;
                s_idleTimeDeadline = 0;
            }

            IncrementTimeStats(elapsedTicks);
        }
    }

    private static void IncrementTimeStats(long elapsedTicks)
    {
        if (elapsedTicks is 0)
        {
            return;
        }

        TimeSpan elapsed = TimeSpan.FromTicks((long)Math.Round((double)elapsedTicks * TimeSpan.TicksPerSecond / Stopwatch.Frequency));
        SessionStats.Time = SessionStats.Time.Add(elapsed);
        ProfileLifetimeStats.Time = ProfileLifetimeStats.Time.Add(elapsed);
        LifetimeStats.Time = LifetimeStats.Time.Add(elapsed);
    }

    private static void StatsTimer_OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        UpdateTimeStats(restartStopWatch: true);

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
                lock (TimeStatsLock)
                {
                    IncrementTimeStats(amount);
                }

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
                LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(StatType), nameof(StatsUtils), nameof(IncrementStat), type);
                break;
        }
    }

    public static void ResetStats(SqliteConnection connection, StatsMode statsMode)
    {
        lock (StatsDBUtils.s_statsDBLock)
        {
            int profileId;
            lock (TimeStatsLock)
            {
                Stats stats = statsMode switch
                {
                    StatsMode.Lifetime => LifetimeStats,
                    StatsMode.Profile => ProfileLifetimeStats,
                    StatsMode.Session => SessionStats,
                    _ => SessionStats
                };

                profileId = statsMode is StatsMode.Profile ? ProfileUtils.CurrentProfileId : ProfileUtils.GlobalProfileId;
                lock (TermLookupCountsLock)
                {
                    stats.ResetStats();
                }
            }

            if (statsMode is StatsMode.Profile or StatsMode.Lifetime)
            {
                StatsDBUtils.ResetAllTermLookupCounts(connection, profileId);
            }
        }
    }

    public static void IncrementTermLookupCount(string deconjugatedMatchedText)
    {
        lock (TermLookupCountsLock)
        {
            SessionStats.IncrementLookupStat(deconjugatedMatchedText);
            ProfileLifetimeStats.IncrementLookupStat(deconjugatedMatchedText);
            LifetimeStats.IncrementLookupStat(deconjugatedMatchedText);
        }
    }
}
