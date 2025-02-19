using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Mining.Anki;

public static class AnkiConfigUtils
{
    private static Dictionary<MineType, AnkiConfig>? s_ankiConfigDict;

    public static async Task WriteAnkiConfig(Dictionary<MineType, AnkiConfig> ankiConfig)
    {
        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "AnkiConfig.json"),
                JsonSerializer.Serialize(ankiConfig, Utils.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation)).ConfigureAwait(false);

            s_ankiConfigDict = ankiConfig;
        }

        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't write AnkiConfig");
            Utils.Logger.Error(ex, "Couldn't write AnkiConfig");
        }
    }

    public static async ValueTask<Dictionary<MineType, AnkiConfig>?> ReadAnkiConfig()
    {
        if (s_ankiConfigDict is not null)
        {
            return s_ankiConfigDict;
        }

        string filePath = Path.Join(Utils.ConfigPath, "AnkiConfig.json");
        if (File.Exists(filePath))
        {
            try
            {
                FileStream ankiConfigStream = File.OpenRead(filePath);
                await using (ankiConfigStream.ConfigureAwait(false))
                {
                    s_ankiConfigDict = await JsonSerializer.DeserializeAsync<Dictionary<MineType, AnkiConfig>>(ankiConfigStream,
                        Utils.s_jsoWithEnumConverter).ConfigureAwait(false);
                }

                return s_ankiConfigDict;
            }

            catch (Exception ex)
            {
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't read AnkiConfig");
                Utils.Logger.Error(ex, "Couldn't read AnkiConfig");
                return null;
            }
        }

        // Utils.Frontend.Alert(AlertLevel.Error, "AnkiConfig.json doesn't exist");
        Utils.Logger.Warning("AnkiConfig.json doesn't exist");
        return null;
    }
}
