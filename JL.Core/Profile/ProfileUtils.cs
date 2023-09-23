using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Profile;
public static class ProfileUtils
{
    public static readonly List<string> DefaultProfiles = new(1) { "Default" };
    public static readonly string ProfileFolderPath = Path.Join(Utils.ApplicationPath, "Profiles");
    public static readonly string DefaultProfilePath = Path.Join(Utils.ApplicationPath, "JL.dll.config");
    public static string CurrentProfile { get; set; } = "Default";
    public static List<string> Profiles { get; private set; } = new() { "Default" };

    public static async Task SerializeProfiles()
    {
        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "Profiles.json"),
                JsonSerializer.Serialize(new Profile(CurrentProfile, Profiles), Utils.s_jsoWithIndentation)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "SerializeProfiles failed");
            throw;
        }
    }


    public static async Task DeserializeProfiles()
    {
        try
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
                        Utils.Logger.Fatal("Couldn't load Config/Profiles.json");
                    }
                }
            }

            else
            {
                Utils.Logger.Information("Profiles.json doesn't exist, creating it");
                await SerializeProfiles().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "DeserializeProfiles failed");
            throw;
        }
    }

    public static string GetProfilePath(string profileName)
    {
        return profileName is "Default"
            ? DefaultProfilePath
            : Path.Join(ProfileFolderPath, string.Create(CultureInfo.InvariantCulture, $"{profileName}.config"));
    }

    public static string GetProfileCustomNameDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, string.Create(CultureInfo.InvariantCulture, $"{profileName}_Custom_Names.txt"));
    }

    public static string GetProfileCustomWordDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, string.Create(CultureInfo.InvariantCulture, $"{profileName}_Custom_Words.txt"));
    }
}
