using System.Configuration;

namespace JL.Windows.Utilities;
internal static class ExtensionMethods
{
    public static string? Get(this KeyValueConfigurationCollection configurationCollection, string key)
    {
        return configurationCollection[key]?.Value ?? null;
    }
}
