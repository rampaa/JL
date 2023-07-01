using System.Diagnostics;
using System.Text.Json;
using System.Timers;
using JL.Core.Utilities;
using Timer = System.Timers.Timer;

namespace JL.Core.Statistics;

public static class StatsUtils
{
    public static Stopwatch StatsStopWatch { get; } = new();
    private static Timer StatsTimer { get; } = new();

    public static async Task DeserializeLifetimeStats()
    {
        string filePath = Path.Join(Utils.ConfigPath, "Stats.json");
        if (File.Exists(filePath))
        {
            try
            {
                FileStream fileStream = File.OpenRead(filePath);
                await using (fileStream.ConfigureAwait(false))
                {
                    Stats.LifetimeStats = await JsonSerializer.DeserializeAsync<Stats>(fileStream,
                        Utils.s_jsoWithEnumConverter).ConfigureAwait(false) ?? Stats.LifetimeStats;
                }
            }

            catch (Exception ex)
            {
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't read Stats");
                Utils.Logger.Error(ex, "Couldn't read Stats");
            }
        }

        else
        {
            Utils.Logger.Information("Stats.json doesn't exist, creating it");
            await Stats.SerializeLifetimeStats().ConfigureAwait(false);
        }
    }

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

    private static async void OnTimedEvent(object? sender, ElapsedEventArgs e)
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

        await Stats.SerializeLifetimeStats().ConfigureAwait(false);
    }
}
