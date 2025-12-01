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

    public static string GetTempPath(string path)
    {
        return $"{path}.tmp";
    }

    public static void CreateFileIfNotExists(string path)
    {
        if (!File.Exists(path))
        {
            using FileStream fileStream = File.Create(path);
        }
    }

    public static void ReplaceFileAtomicallyOnSameVolume(string fileToBeReplaced, string tempFile)
    {
        if (OperatingSystem.IsWindows() && File.Exists(fileToBeReplaced))
        {
            File.Replace(fileToBeReplaced, tempFile, null, true);
        }
        else
        {
            File.Move(tempFile, fileToBeReplaced, true);
        }
    }
}
