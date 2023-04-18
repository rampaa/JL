using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace JL.Core.Statistics;

public static class StatsUtils
{
    public static Stopwatch StatsStopWatch { get; } = new();
    internal static Timer StatsTimer { get; } = new();

    internal static void SetTimer()
    {
        StatsTimer.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds;
        StatsTimer.Elapsed += OnTimedEvent;
        StatsTimer.AutoReset = true;
        StatsTimer.Enabled = true;
    }

    private static async void OnTimedEvent(object? sender, ElapsedEventArgs e)
    {
        await Stats.IncrementStat(StatType.Time, StatsStopWatch.ElapsedTicks).ConfigureAwait(false);

        if (StatsStopWatch.IsRunning)
        {
            StatsStopWatch.Restart();
        }

        else
        {
            StatsStopWatch.Reset();
        }

        await Stats.UpdateLifetimeStats().ConfigureAwait(false);
    }
}
