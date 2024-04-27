using System.Diagnostics;
using System.Timers;
using JL.Core.Config;
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

        UpdateLifetimeStats();
        UpdateProfileLifetimeStats();
    }

    public static void UpdateLifetimeStats()
    {
        StatsDBUtils.UpdateStats(Stats.LifetimeStats, ProfileUtils.DefaultProfileId);
    }

    public static void UpdateProfileLifetimeStats()
    {
        StatsDBUtils.UpdateStats(Stats.ProfileLifetimeStats, ProfileUtils.CurrentProfileId);
    }

    public static void SetStatsFromConfig()
    {
        Stats.LifetimeStats = StatsDBUtils.GetStatsFromConfig(ProfileUtils.DefaultProfileId)!;
        Stats.ProfileLifetimeStats = StatsDBUtils.GetStatsFromConfig(ProfileUtils.CurrentProfileId)!;
    }
}
