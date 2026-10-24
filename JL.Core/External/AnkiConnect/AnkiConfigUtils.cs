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
    private static readonly SemaphoreSlim s_configSemaphoreSlim = new(1, 1);

    public static async Task WriteAnkiConfig(Dictionary<MineType, AnkiConfig> ankiConfig)
    {
        await s_configSemaphoreSlim.WaitAsync().ConfigureAwait(false);
        try
        {
            if (await WriteAnkiConfigFile(ankiConfig).ConfigureAwait(false))
            {
                PrepareMiningConfigs(ankiConfig);
                Volatile.Write(ref s_ankiConfigDict, ankiConfig);
            }
        }
        finally
        {
            _ = s_configSemaphoreSlim.Release();
        }
    }

    private static async Task<bool> WriteAnkiConfigFile(Dictionary<MineType, AnkiConfig> ankiConfig)
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
            return true;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't write AnkiConfig");
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't write AnkiConfig. Check the logs for more details.");
            return false;
        }
    }

    public static async ValueTask<Dictionary<MineType, AnkiConfig>?> ReadAnkiConfig(CancellationToken cancellationToken)
    {
        Dictionary<MineType, AnkiConfig>? ankiConfigDict = Volatile.Read(ref s_ankiConfigDict);
        if (ankiConfigDict is not null)
        {
            return ankiConfigDict;
        }

        await s_configSemaphoreSlim.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            ankiConfigDict = s_ankiConfigDict;
            if (ankiConfigDict is not null)
            {
                return ankiConfigDict;
            }

            if (!File.Exists(s_configFilePath))
            {
                LoggerManager.Logger.Warning("AnkiConfig.json doesn't exist");
                return null;
            }

            FileStream ankiConfigStream = new(s_configFilePath, FileStreamOptionsPresets.s_asyncReadFso);
            await using (ankiConfigStream.ConfigureAwait(false))
            {
                ankiConfigDict = await JsonSerializer.DeserializeAsync<Dictionary<MineType, AnkiConfig>>(ankiConfigStream, JsonOptions.s_jsoWithEnumConverter, CancellationToken.None).ConfigureAwait(false);
            }

            Debug.Assert(ankiConfigDict is not null);
            AtomicBool fieldsChanged = new(false);
            await Parallel.ForEachAsync(ankiConfigDict.Values, CancellationToken.None, async (ankiConfig, _) =>
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
                _ = await WriteAnkiConfigFile(ankiConfigDict).ConfigureAwait(false);
            }

            PrepareMiningConfigs(ankiConfigDict);
            Volatile.Write(ref s_ankiConfigDict, ankiConfigDict);
            return ankiConfigDict;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't read AnkiConfig");
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't read AnkiConfig. Check the logs for more details.");
            return null;
        }
        finally
        {
            _ = s_configSemaphoreSlim.Release();
        }
    }

    internal static void PrepareMiningConfigs(Dictionary<MineType, AnkiConfig> ankiConfigs)
    {
        foreach ((MineType mineType, AnkiConfig config) in ankiConfigs)
        {
            config.PrepareMiningFields();
            config.MineType = mineType;
            foreach ((MineType previousMineType, AnkiConfig previousConfig) in ankiConfigs)
            {
                if (previousMineType == mineType)
                {
                    break;
                }

                if (config.DeckName == previousConfig.DeckName && config.ModelName == previousConfig.ModelName)
                {
                    config.MineType = previousConfig.MineType;
                    break;
                }
            }
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
