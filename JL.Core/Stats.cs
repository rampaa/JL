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

    [JsonIgnore] public static Stats LifetimeStats { get; set; } = ReadLifetimeStats()!;

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

    public static async Task UpdateLifetimeStats()
    {
        await WriteLifetimeStats(LifetimeStats).ConfigureAwait(false);
    }

    private static async Task<bool> WriteLifetimeStats(Stats lifetimeStats)
    {
        try
        {
            Directory.CreateDirectory(Storage.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Storage.ConfigPath, "Stats.json"),
                    JsonSerializer.Serialize(lifetimeStats, new JsonSerializerOptions { WriteIndented = true }))
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write Stats");
            Utils.Logger.Error(ex, "Couldn't write Stats");
            return false;
        }
    }

    private static Stats? ReadLifetimeStats()
    {
        if (File.Exists(Path.Join(Storage.ConfigPath, "Stats.json")))
        {
            try
            {
                return JsonSerializer.Deserialize<Stats>(
                   File.ReadAllText(Path.Join(Storage.ConfigPath, "Stats.json")));
            }

            catch (Exception ex)
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't read Stats");
                Utils.Logger.Error(ex, "Couldn't read Stats");
                return null;
            }
        }
        else
        {
            Utils.Logger.Information("Stats.json doesn't exist, creating it");

            var lifetimeStats = new Stats();
            WriteLifetimeStats(lifetimeStats).Wait();
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
