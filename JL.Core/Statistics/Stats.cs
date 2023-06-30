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

    [JsonIgnore] private static Stats? s_lifetimeStats;

    public static async ValueTask<Stats> GetLifetimeStats()
    {
        return s_lifetimeStats ??= await ReadLifetimeStats().ConfigureAwait(false);
    }

    public static async ValueTask IncrementStat(StatType type, long amount = 1)
    {
        Stats lifetimeStats = await GetLifetimeStats().ConfigureAwait(false);
        switch (type)
        {
            case StatType.Characters:
                SessionStats.Characters += amount;
                lifetimeStats.Characters += amount;
                break;
            case StatType.Lines:
                SessionStats.Lines += amount;
                lifetimeStats.Lines += amount;
                break;
            case StatType.Time:
                SessionStats.Time = SessionStats.Time.Add(TimeSpan.FromTicks(amount));
                lifetimeStats.Time = lifetimeStats.Time.Add(TimeSpan.FromTicks(amount));
                break;
            case StatType.CardsMined:
                SessionStats.CardsMined += amount;
                lifetimeStats.CardsMined += amount;
                break;
            case StatType.TimesPlayedAudio:
                SessionStats.TimesPlayedAudio += amount;
                lifetimeStats.TimesPlayedAudio += amount;
                break;
            case StatType.Imoutos:
                SessionStats.Imoutos += amount;
                lifetimeStats.Imoutos += amount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static async ValueTask ResetStats(StatsMode statsMode)
    {
        switch (statsMode)
        {
            case StatsMode.Lifetime:
                Stats lifetimeStats = await GetLifetimeStats().ConfigureAwait(false);
                lifetimeStats.Characters = 0;
                lifetimeStats.Lines = 0;
                lifetimeStats.Time = TimeSpan.Zero;
                lifetimeStats.CardsMined = 0;
                lifetimeStats.TimesPlayedAudio = 0;
                lifetimeStats.Imoutos = 0;
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

    public static async Task UpdateLifetimeStats()
    {
        Stats lifetimeStats = await GetLifetimeStats().ConfigureAwait(false);
        await WriteLifetimeStats(lifetimeStats).ConfigureAwait(false);
    }

    private static async Task WriteLifetimeStats(Stats lifetimeStats)
    {
        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "Stats.json"),
                    JsonSerializer.Serialize(lifetimeStats, Utils.s_jsoWithIndendation))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't write Stats");
            Utils.Logger.Error(ex, "Couldn't write Stats");
        }
    }

    private static async Task<Stats> ReadLifetimeStats()
    {
        string filePath = Path.Join(Utils.ConfigPath, "Stats.json");
        if (File.Exists(filePath))
        {
            try
            {
                FileStream fileStream = File.OpenRead(filePath);
                await using (fileStream.ConfigureAwait(false))
                {
                    return await JsonSerializer.DeserializeAsync<Stats>(fileStream,
                        Utils.s_jsoWithEnumConverter).ConfigureAwait(false) ?? new Stats();
                }
            }

            catch (Exception ex)
            {
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't read Stats");
                Utils.Logger.Error(ex, "Couldn't read Stats");
                return new Stats();
            }
        }

        Utils.Logger.Information("Stats.json doesn't exist, creating it");
        Stats lifetimeStats = new();
        await WriteLifetimeStats(lifetimeStats).ConfigureAwait(false);
        return lifetimeStats;
    }
}
