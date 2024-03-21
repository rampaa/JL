using System.Runtime.Serialization;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Profile;
public static class ProfileUtils
{
    public static readonly List<string> DefaultProfiles = ["Default"];
    public static readonly string ProfileFolderPath = Path.Join(Utils.ApplicationPath, "Profiles");
    public static readonly string DefaultProfilePath = Path.Join(Utils.ApplicationPath, "JL.dll.config");
    public static string CurrentProfile { get; set; } = "Default";
    public static List<string> Profiles { get; private set; } = ["Default"];

    public static async Task SerializeProfiles()
    {
        _ = Directory.CreateDirectory(Utils.ConfigPath);
        await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "Profiles.json"),
            JsonSerializer.Serialize(new Profile(CurrentProfile, Profiles), Utils.s_jsoWithIndentation)).ConfigureAwait(false);
    }

    public static async Task DeserializeProfiles()
    {
        string profileConfigPath = Path.Join(Utils.ConfigPath, "Profiles.json");
        if (File.Exists(profileConfigPath))
        {
            FileStream fileStream = File.OpenRead(profileConfigPath);
            await using (fileStream.ConfigureAwait(false))
            {
                Profile? profileRecord = await JsonSerializer
                    .DeserializeAsync<Profile>(fileStream, Utils.s_jsoWithIndentation).ConfigureAwait(false);

                if (profileRecord is not null)
                {
                    CurrentProfile = profileRecord.CurrentProfile;
                    Profiles = profileRecord.Profiles;
                }
                else
                {
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/Profiles.json");
                    throw new SerializationException("Couldn't load Config/Profiles.json");
                }
            }
        }

        else
        {
            Utils.Logger.Information("Profiles.json doesn't exist, creating it");
            await SerializeProfiles().ConfigureAwait(false);
        }
    }

    public static string GetProfilePath(string profileName)
    {
        return profileName is "Default"
            ? DefaultProfilePath
            : Path.Join(ProfileFolderPath, $"{profileName}.config");
    }

    public static string GetProfileCustomNameDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, $"{profileName}_Custom_Names.txt");
    }

    public static string GetProfileCustomWordDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, $"{profileName}_Custom_Words.txt");
    }
}
