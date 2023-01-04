using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core;

public class Stats
{
    public long Characters { get; set; }

    public long Lines { get; set; }

    public TimeSpan Time { get; set; }

    public long CardsMined { get; set; }

    public long TimesPlayedAudio { get; set; }

    public long Imoutos { get; set; }

    [JsonIgnore] public static Stats SessionStats { get; set; } = new();

    [JsonIgnore] private static Stats? s_lifetimeStats;

    public static async Task<Stats> GetLifetimeStats()
    {
        return s_lifetimeStats ??= await ReadLifetimeStats().ConfigureAwait(false);
    }

    public static async Task IncrementStat(StatType type, long amount = 1)
    {
        Stats lifeTimeStats = await GetLifetimeStats().ConfigureAwait(false);
        switch (type)
        {
            case StatType.Characters:
                SessionStats.Characters += amount;
                lifeTimeStats.Characters += amount;
                break;
            case StatType.Lines:
                SessionStats.Lines += amount;
                lifeTimeStats.Lines += amount;
                break;
            case StatType.Time:
                SessionStats.Time = SessionStats.Time.Add(TimeSpan.FromTicks(amount));
                lifeTimeStats.Time = lifeTimeStats.Time.Add(TimeSpan.FromTicks(amount));
                break;
            case StatType.CardsMined:
                SessionStats.CardsMined += amount;
                lifeTimeStats.CardsMined += amount;
                break;
            case StatType.TimesPlayedAudio:
                SessionStats.TimesPlayedAudio += amount;
                lifeTimeStats.TimesPlayedAudio += amount;
                break;
            case StatType.Imoutos:
                SessionStats.Imoutos += amount;
                lifeTimeStats.Imoutos += amount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static async Task UpdateLifetimeStats()
    {
        Stats lifeTimeStats = await GetLifetimeStats().ConfigureAwait(false);
        await WriteLifetimeStats(lifeTimeStats).ConfigureAwait(false);
    }

    private static async Task WriteLifetimeStats(Stats lifetimeStats)
    {
        try
        {
            _ = Directory.CreateDirectory(Storage.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Storage.ConfigPath, "Stats.json"),
                    JsonSerializer.Serialize(lifetimeStats, new JsonSerializerOptions { WriteIndented = true }))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write Stats");
            Utils.Logger.Error(ex, "Couldn't write Stats");
        }
    }

    private static async Task<Stats> ReadLifetimeStats()
    {
        if (File.Exists(Path.Join(Storage.ConfigPath, "Stats.json")))
        {
            try
            {
                return JsonSerializer.Deserialize<Stats>(
                   File.ReadAllText(Path.Join(Storage.ConfigPath, "Stats.json"))) ?? new Stats();
            }

            catch (Exception ex)
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't read Stats");
                Utils.Logger.Error(ex, "Couldn't read Stats");
                return new Stats();
            }
        }
        else
        {
            Utils.Logger.Information("Stats.json doesn't exist, creating it");

            Stats lifetimeStats = new();
            await WriteLifetimeStats(lifetimeStats).ConfigureAwait(false);
            return lifetimeStats;
        }
    }
}

public enum StatType
{
    Characters,
    Lines,
    Time,
    CardsMined,
    TimesPlayedAudio,
    Imoutos
}
