using System.Diagnostics;
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

                Debug.Assert(s_ankiConfigDict is not null);
                int firstFieldChangedFlag = 0;
                await Parallel.ForEachAsync(s_ankiConfigDict.Values, async (ankiConfig, cancellationToken) =>
                {
                    Debug.Assert(ankiConfig.Fields.Count > 0);
                    string[]? fields = await AnkiUtils.GetFieldNames(ankiConfig.ModelName).ConfigureAwait(false);
                    if (fields is not null)
                    {
                        ReadOnlySpan<string> fieldsSpan = fields.AsReadOnlySpan();
                        if (ankiConfig.Fields.GetAt(0).Key != fieldsSpan[0])
                        {
                            _ = Interlocked.CompareExchange(ref firstFieldChangedFlag, 1, 0);

                            OrderedDictionary<string, JLField> upToDateFields = new(fieldsSpan.Length);
                            for (int i = 0; i < fieldsSpan.Length; i++)
                            {
                                string fieldName = fieldsSpan[i];
                                if (ankiConfig.Fields.TryGetValue(fieldName, out JLField field))
                                {
                                    upToDateFields.Add(fieldName, field);
                                }
                                else
                                {
                                    upToDateFields.Add(fieldName, JLField.Nothing);
                                }
                            }

                            ankiConfig.Fields = upToDateFields;
                        }
                    }
                }).ConfigureAwait(false);

                if (firstFieldChangedFlag is 1)
                {
                    await WriteAnkiConfig(s_ankiConfigDict).ConfigureAwait(false);
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
