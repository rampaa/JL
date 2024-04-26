using System.Globalization;
using JL.Core.Config;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Profile;

public static class ProfileUtils
{
    public const int DefaultProfileId = 1;
    public const string DefaultProfileName = "Default";
    public static readonly string ProfileFolderPath = Path.Join(Utils.ApplicationPath, "Profiles");
    public static int CurrentProfileId { get; set; } = DefaultProfileId;
    public static string CurrentProfileName { get; set; } = DefaultProfileName;

    public static string GetProfileCustomNameDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, $"{profileName}_Custom_Names.txt");
    }

    public static string GetProfileCustomWordDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, $"{profileName}_Custom_Words.txt");
    }

    public static void UpdateCurrentProfile()
    {
        using SqliteConnection command = ConfigDBManager.CreateDBConnection();
        ConfigDBManager.UpdateSetting(command, nameof(CurrentProfileId), CurrentProfileId.ToString(CultureInfo.InvariantCulture), 1);
    }
}
