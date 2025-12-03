namespace JL.Core;

public static class AppInfo
{
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(ApplicationPath, "Resources");
    public static readonly string ConfigPath = Path.Join(ApplicationPath, "Config");
    public static readonly bool Is64BitProcess = Environment.Is64BitProcess;
}
