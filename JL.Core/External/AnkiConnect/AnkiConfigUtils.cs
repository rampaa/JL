using System.Diagnostics;
using System.Text.Json;
using JL.Core.Frontend;
using JL.Core.Mining;
using JL.Core.Utilities;
using JL.Core.Utilities.Bool;

namespace JL.Core.External.AnkiConnect;

public static class AnkiConfigUtils
{
    private static Dictionary<MineType, AnkiConfig>? s_ankiConfigDict;

    public static async Task WriteAnkiConfig(Dictionary<MineType, AnkiConfig> ankiConfig)
    {
        try
        {
            _ = Directory.CreateDirectory(AppInfo.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(AppInfo.ConfigPath, "AnkiConfig.json"),
                JsonSerializer.Serialize(ankiConfig, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation)).ConfigureAwait(false);

            s_ankiConfigDict = ankiConfig;
        }

        catch (Exception ex)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't write AnkiConfig");
            LoggerManager.Logger.Error(ex, "Couldn't write AnkiConfig");
        }
    }

    public static async ValueTask<Dictionary<MineType, AnkiConfig>?> ReadAnkiConfig()
    {
        if (s_ankiConfigDict is not null)
        {
            return s_ankiConfigDict;
        }

        string filePath = Path.Join(AppInfo.ConfigPath, "AnkiConfig.json");
        if (File.Exists(filePath))
        {
            try
            {
                FileStream ankiConfigStream = File.OpenRead(filePath);
                await using (ankiConfigStream.ConfigureAwait(false))
                {
                    s_ankiConfigDict = await JsonSerializer.DeserializeAsync<Dictionary<MineType, AnkiConfig>>(ankiConfigStream,
                        JsonOptions.s_jsoWithEnumConverter).ConfigureAwait(false);
                }

                Debug.Assert(s_ankiConfigDict is not null);
                AtomicBool firstFieldChanged = new(false);
                await Parallel.ForEachAsync(s_ankiConfigDict.Values, async (ankiConfig, cancellationToken) =>
                {
                    Debug.Assert(ankiConfig.Fields.Count > 0);
                    string[]? fields = await AnkiConnectUtils.GetFieldNames(ankiConfig.ModelName).ConfigureAwait(false);
                    if (fields is not null)
                    {
                        ReadOnlySpan<string> fieldsSpan = fields.AsReadOnlySpan();
                        if (ankiConfig.Fields.GetAt(0).Key != fieldsSpan[0])
                        {
                            firstFieldChanged.SetTrue();

                            OrderedDictionary<string, JLField> upToDateFields = new(fieldsSpan.Length);
                            for (int i = 0; i < fieldsSpan.Length; i++)
                            {
                                string fieldName = fieldsSpan[i];
                                upToDateFields.Add(fieldName, ankiConfig.Fields.GetValueOrDefault(fieldName, JLField.Nothing));
                            }

                            ankiConfig.Fields = upToDateFields;
                        }
                    }
                }).ConfigureAwait(false);

                if (firstFieldChanged)
                {
                    await WriteAnkiConfig(s_ankiConfigDict).ConfigureAwait(false);
                }

                return s_ankiConfigDict;
            }

            catch (Exception ex)
            {
                FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't read AnkiConfig");
                LoggerManager.Logger.Error(ex, "Couldn't read AnkiConfig");
                return null;
            }
        }

        // FrontendManager.Frontend.Alert(AlertLevel.Error, "AnkiConfig.json doesn't exist");
        LoggerManager.Logger.Warning("AnkiConfig.json doesn't exist");
        return null;
    }
}
