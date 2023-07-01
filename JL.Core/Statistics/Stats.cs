using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Statistics;

public sealed class Stats
{
    public long Characters { get; set; }

    public long Lines { get; set; }

    public TimeSpan Time { get; set; }

    public long CardsMined { get; set; }

    public long TimesPlayedAudio { get; set; }

    public long Imoutos { get; set; }

    [JsonIgnore] public static Stats SessionStats { get; set; } = new();

    [JsonIgnore] public static Stats LifetimeStats { get; set; } = new();

    public static void IncrementStat(StatType type, long amount = 1)
    {
        switch (type)
        {
            case StatType.Characters:
                SessionStats.Characters += amount;
                LifetimeStats.Characters += amount;
                break;
            case StatType.Lines:
                SessionStats.Lines += amount;
                LifetimeStats.Lines += amount;
                break;
            case StatType.Time:
                SessionStats.Time = SessionStats.Time.Add(TimeSpan.FromTicks(amount));
                LifetimeStats.Time = LifetimeStats.Time.Add(TimeSpan.FromTicks(amount));
                break;
            case StatType.CardsMined:
                SessionStats.CardsMined += amount;
                LifetimeStats.CardsMined += amount;
                break;
            case StatType.TimesPlayedAudio:
                SessionStats.TimesPlayedAudio += amount;
                LifetimeStats.TimesPlayedAudio += amount;
                break;
            case StatType.Imoutos:
                SessionStats.Imoutos += amount;
                LifetimeStats.Imoutos += amount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static void ResetStats(StatsMode statsMode)
    {
        switch (statsMode)
        {
            case StatsMode.Lifetime:
                LifetimeStats.Characters = 0;
                LifetimeStats.Lines = 0;
                LifetimeStats.Time = TimeSpan.Zero;
                LifetimeStats.CardsMined = 0;
                LifetimeStats.TimesPlayedAudio = 0;
                LifetimeStats.Imoutos = 0;
                break;

            case StatsMode.Session:
                SessionStats.Characters = 0;
                SessionStats.Lines = 0;
                SessionStats.Time = TimeSpan.Zero;
                SessionStats.CardsMined = 0;
                SessionStats.TimesPlayedAudio = 0;
                SessionStats.Imoutos = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(statsMode), statsMode, null);
        }
    }

    public static async Task SerializeLifetimeStats()
    {
        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "Stats.json"),
                    JsonSerializer.Serialize(LifetimeStats, Utils.s_jsoWithIndentation))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't write Stats");
            Utils.Logger.Error(ex, "Couldn't write Stats");
        }
    }
}
