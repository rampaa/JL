using System.Diagnostics;
using System.Text.Json;
using System.Timers;
using JL.Core.Config;
using JL.Core.Profile;
using JL.Core.Utilities;
using Timer = System.Timers.Timer;

namespace JL.Core.Statistics;

public static class StatsUtils
{
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
        Stats.IncrementStat(StatType.Time, StatsStopWatch.ElapsedTicks);

        if (StatsStopWatch.IsRunning)
        {
            StatsStopWatch.Restart();
        }

        else
        {
            StatsStopWatch.Reset();
        }

        InsertOrUpdateStats(Stats.LifetimeStats, ProfileUtils.DefaultProfileId);
        InsertOrUpdateStats(Stats.ProfileLifetimeStats, ProfileUtils.CurrentProfileId);
    }

    private static void InsertOrUpdateStats(Stats stats, int profileId)
    {
        string lifetimeStats = JsonSerializer.Serialize(stats, Utils.s_jsoWithIndentation);

        bool statsExists = StatsDBUtils.StatsExists(profileId);
        if (statsExists)
        {
            StatsDBUtils.UpdateStats(lifetimeStats, profileId);
        }
        else
        {
            StatsDBUtils.InsertStats(lifetimeStats, profileId);
        }
    }

    public static void UpdateLifetimeStats()
    {
        string lifetimeStats = JsonSerializer.Serialize(Stats.LifetimeStats, Utils.s_jsoWithIndentation);
        StatsDBUtils.UpdateStats(lifetimeStats, ProfileUtils.DefaultProfileId);
    }

    public static void UpdateProfileLifetimeStats()
    {
        string profileLifetimeStats = JsonSerializer.Serialize(Stats.ProfileLifetimeStats, Utils.s_jsoWithIndentation);
        StatsDBUtils.UpdateStats(profileLifetimeStats, ProfileUtils.CurrentProfileId);
    }
}
