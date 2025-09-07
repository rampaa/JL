namespace JL.Core.Utilities;
public static class PathUtils
{
    public static string GetPortablePath(string path)
    {
        string fullPath = Path.GetFullPath(path, AppInfo.ApplicationPath);
        return fullPath.StartsWith(AppInfo.ApplicationPath, StringComparison.Ordinal)
            ? Path.GetRelativePath(AppInfo.ApplicationPath, fullPath)
            : fullPath;
    }
}
