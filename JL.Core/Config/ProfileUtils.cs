using JL.Core.Utilities;

namespace JL.Core.Config;

public static class ProfileUtils
{
    public const int GlobalProfileId = 0;
    internal const string GlobalProfileName = "JLGlobal";
    internal const int DefaultProfileId = 1;
    internal const string DefaultProfileName = "Default";
    public static readonly string ProfileFolderPath = Path.Join(AppInfo.ApplicationPath, "Profiles");
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
}
