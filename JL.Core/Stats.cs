using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core;

public class Stats
{
    public long Characters { get; set; }

    public long Lines { get; set; }

    public int CardsMined { get; set; }

    public int TimesPlayedAudio { get; set; }

    public int Imoutos { get; set; }

    [JsonIgnore] public static Stats SessionStats { get; set; } = new();

    [JsonIgnore] public static Stats LifetimeStats { get; set; } = ReadLifetimeStats().Result!;

    public static void IncrementStat(StatType type, int amount = 1)
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
        await WriteLifetimeStats(LifetimeStats);
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
        catch (Exception e)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write Stats");
            Utils.Logger.Error(e, "Couldn't write Stats");
            return false;
        }
    }

    private static async Task<Stats?> ReadLifetimeStats()
    {
        if (File.Exists(Path.Join(Storage.ConfigPath, "Stats.json")))
        {
            try
            {
                return JsonSerializer.Deserialize<Stats>(
                    await File.ReadAllTextAsync(Path.Join(Storage.ConfigPath, "Stats.json")).ConfigureAwait(false));
            }

            catch
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't read Stats");
                Utils.Logger.Error("Couldn't read Stats");
                return null;
            }
        }
        else
        {
            Utils.Logger.Warning("Stats.json doesn't exist, creating it");

            var lifetimeStats = new Stats();
            await WriteLifetimeStats(lifetimeStats);
            return lifetimeStats;
        }
    }
}

public enum StatType
{
    Characters,
    Lines,
    CardsMined,
    TimesPlayedAudio,
    Imoutos
}
