using JL.Core.Config;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace JL.Windows;

internal static class ConfigMigrationManager
{
    private static readonly string s_profileFolderPath = Path.Join(Utils.ApplicationPath, "Profiles");
    private static readonly string s_defaultProfilePath = Path.Join(Utils.ApplicationPath, "JL.dll.config");
    private static readonly string s_profileConfigPath = Path.Join(Utils.ConfigPath, "Profiles.json");

    private static string GetStatsPath(string profileName)
    {
        return Path.Join(ProfileUtils.ProfileFolderPath, $"{profileName}_Stats.json");
    }

    private static readonly JsonSerializerOptions s_jsoWithIndentation = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

#pragma warning disable CA1812
    private sealed class Profile(string currentProfile, List<string> profiles)
    {
        public string CurrentProfile { get; } = currentProfile;
        public List<string> Profiles { get; } = profiles;
    }
#pragma warning restore CA1812

    private static string GetProfilePath(string profileName)
    {
        return profileName is "Default"
            ? s_defaultProfilePath
            : Path.Join(s_profileFolderPath, $"{profileName}.config");
    }

    private static async Task<(string currentProfileName, List<string> profileNames)?> GetProfilePathsFromOldConfig()
    {
        if (File.Exists(s_profileConfigPath))
        {
            FileStream fileStream = File.OpenRead(s_profileConfigPath);
            await using (fileStream.ConfigureAwait(false))
            {
                Profile? profileRecord = await JsonSerializer
                    .DeserializeAsync<Profile>(fileStream, s_jsoWithIndentation).ConfigureAwait(false);

                if (profileRecord is not null)
                {
                    return (profileRecord.CurrentProfile, profileRecord.Profiles);
                }
            }
        }

        return null;
    }

    private static void MigrateProfiles(SqliteConnection connection, string profileName)
    {
        if (profileName is not "Default")
        {
            ProfileDBUtils.InsertProfile(connection, profileName);
        }
    }

    private static void MigrateSettings(SqliteConnection connection, string profilePath, int profileId)
    {
        XmlReaderSettings xmlReaderSettings = new()
        {
            Async = true,
            DtdProcessing = DtdProcessing.Parse,
            IgnoreWhitespace = true
        };

        using XmlReader xmlReader = XmlReader.Create(profilePath, xmlReaderSettings);
        while (xmlReader.ReadToFollowing("add"))
        {
            string key = xmlReader.GetAttribute(nameof(key))!;
            string value = xmlReader.GetAttribute(nameof(value))!;
            ConfigDBManager.InsertSetting(connection, key, value, profileId);
        }
    }

    private static async Task MigrateStats(SqliteConnection connection, string statsPath, int profileId)
    {
        if (!File.Exists(statsPath))
        {
            return;
        }

        string stats = await File.ReadAllTextAsync(statsPath).ConfigureAwait(false);

        if (profileId is 1)
        {
            StatsDBUtils.UpdateStats(connection, stats, profileId);
        }
        else
        {
            StatsDBUtils.InsertStats(connection, stats, profileId);
        }
    }

    public static async Task MigrateConfig(SqliteConnection connection)
    {
        (string currentProfileName, List<string> profileNames)? profileInfo = await GetProfilePathsFromOldConfig().ConfigureAwait(false);

        if (profileInfo is null)
        {
            return;
        }

        List<string> profiles = profileInfo.Value.profileNames;
        int profileCount = profiles.Count;
        for (int i = 0; i < profileCount; i++)
        {
            string profileName = profiles[i];
            string profilePath = GetProfilePath(profileName);
            if (!File.Exists(profilePath))
            {
                continue;
            }

            MigrateProfiles(connection, profileName);

            int profileId = ProfileDBUtils.GetProfileId(connection, profileName);
            MigrateSettings(connection, profilePath, profileId);

            string statsPath = GetStatsPath(profileName);
            await MigrateStats(connection, statsPath, profileId).ConfigureAwait(false);

            File.Delete(profilePath);
            File.Delete(statsPath);
        }

        int currentProfileId = ProfileDBUtils.GetProfileId(connection, profileInfo.Value.currentProfileName);
        ConfigDBManager.UpdateSetting(connection, "CurrentProfileId", currentProfileId.ToString(CultureInfo.InvariantCulture), 1);

        File.Delete(s_profileConfigPath);
    }
}
