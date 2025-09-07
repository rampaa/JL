namespace JL.Core;
public static class AppInfo
{
    public static readonly Version JLVersion = new(3, 8, 3);
    public static readonly string ApplicationPath = AppContext.BaseDirectory;
    public static readonly string ResourcesPath = Path.Join(ApplicationPath, "Resources");
    public static readonly string ConfigPath = Path.Join(ApplicationPath, "Config");
    public static readonly bool Is64BitProcess = Environment.Is64BitProcess;
}
