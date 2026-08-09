namespace JL.Core.Config;

public static class ProfileUtils
{
    public const int GlobalProfileId = 0;
    internal const string GlobalProfileName = "JLGlobal";
    internal const int DefaultProfileId = 1;
    internal const string DefaultProfileName = "Default";
    internal const string MpvProfileName = "mpv";
    internal const string TsukikageProfileName = "Tsukikage";
    internal const string CustomNames = "Custom_Names";
    internal const string CustomWords = "Custom_Words";

    public static readonly string ProfileFolderPath = Path.Join(AppInfo.ApplicationPath, "Profiles");
    public static int CurrentProfileId { get; set; } = DefaultProfileId;
    public static string CurrentProfileName { get; set; } = DefaultProfileName;
    public static DateTime CurrentProfileSessionStartTime { get; set; } = DateTime.Now;

    public static string GetProfileCustomNameDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, $"{profileName}_{CustomNames}.txt");
    }

    public static string GetProfileCustomWordDictPath(string profileName)
    {
        return Path.Join(ProfileFolderPath, $"{profileName}_{CustomWords}.txt");
    }
}
