using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Frontend;
using JL.Core.Mining;
using JL.Core.Utilities;
using JL.Core.Utilities.Bool;

namespace JL.Core.External.AnkiConnect;

public static class AnkiConfigUtils
{
    private static readonly string s_configFilePath = Path.Join(AppInfo.ConfigPath, "AnkiConfig.json");

    private static Dictionary<MineType, AnkiConfig>? s_ankiConfigDict;

    public static async Task WriteAnkiConfig(Dictionary<MineType, AnkiConfig> ankiConfig)
    {
        try
        {
            _ = Directory.CreateDirectory(AppInfo.ConfigPath);

            string tempConfigFilePath = PathUtils.GetTempPath(s_configFilePath);
            FileStream fileStream = new(tempConfigFilePath, FileStreamOptionsPresets.s_asyncCreateFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(fileStream, ankiConfig, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation).ConfigureAwait(false);
            }

            PathUtils.ReplaceFileAtomicallyOnSameVolume(s_configFilePath, tempConfigFilePath);
            s_ankiConfigDict = ankiConfig;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't write AnkiConfig");
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't write AnkiConfig. Check the logs for more details.");
        }
    }

    public static async ValueTask<Dictionary<MineType, AnkiConfig>?> ReadAnkiConfig(CancellationToken cancellationToken)
    {
        if (s_ankiConfigDict is not null)
        {
            return s_ankiConfigDict;
        }

        if (!File.Exists(s_configFilePath))
        {
            LoggerManager.Logger.Warning("AnkiConfig.json doesn't exist");
            return null;
        }

        try
        {
            FileStream ankiConfigStream = new(s_configFilePath, FileStreamOptionsPresets.s_asyncReadFso);
            await using (ankiConfigStream.ConfigureAwait(false))
            {
                s_ankiConfigDict = await JsonSerializer.DeserializeAsync<Dictionary<MineType, AnkiConfig>>(ankiConfigStream, JsonOptions.s_jsoWithEnumConverter, CancellationToken.None).ConfigureAwait(false);
            }

            Debug.Assert(s_ankiConfigDict is not null);
            AtomicBool fieldsChanged = new(false);
            await Parallel.ForEachAsync(s_ankiConfigDict.Values, CancellationToken.None, async (ankiConfig, _) =>
            {
                if (ankiConfig.Fields.Count > 0)
                {
                    string[]? fields = await AnkiConnectUtils.GetFieldNames(ankiConfig.ModelName, cancellationToken).ConfigureAwait(false);
                    if (fields?.Length > 0)
                    {
                        if (UpdateFields(ankiConfig, fields))
                        {
                            fieldsChanged.SetTrue();
                        }
                    }
                }
            }).ConfigureAwait(false);

            if (fieldsChanged.Read())
            {
                await WriteAnkiConfig(s_ankiConfigDict).ConfigureAwait(false);
            }

            return s_ankiConfigDict;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't read AnkiConfig");
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't read AnkiConfig. Check the logs for more details.");
            return null;
        }
    }

    internal static bool UpdateFields(AnkiConfig ankiConfig, ReadOnlySpan<string> fieldNames)
    {
        bool fieldsMatch = ankiConfig.Fields.Count == fieldNames.Length;
        if (fieldsMatch)
        {
            for (int i = 0; i < fieldNames.Length; i++)
            {
                if (ankiConfig.Fields.GetAt(i).Key != fieldNames[i])
                {
                    fieldsMatch = false;
                    break;
                }
            }
        }

        if (fieldsMatch)
        {
            return false;
        }

        OrderedDictionary<string, JLField> fields = new(fieldNames.Length, StringComparer.Ordinal);
        foreach (string fieldName in fieldNames)
        {
            fields.Add(fieldName, ankiConfig.Fields.GetValueOrDefault(fieldName, JLField.Nothing));
        }

        ankiConfig.Fields = fields;
        ankiConfig.UsedJLFields = fields.Values.Where(static field => field is not JLField.Nothing).ToFrozenSet();
        return true;
    }
}
