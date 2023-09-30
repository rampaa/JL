using System.Configuration;
using System.Windows;
using System.Windows.Media;

namespace JL.Windows.Utilities;
internal static class ExtensionMethods
{
    public static string? Get(this KeyValueConfigurationCollection configurationCollection, string key)
    {
        return configurationCollection[key]?.Value ?? null;
    }

    public static T? GetChildOfType<T>(this DependencyObject dependencyObject) where T : DependencyObject
    {
        int childrenCount = VisualTreeHelper.GetChildrenCount(dependencyObject);
        for (int i = 0; i < childrenCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
            if (child is T result)
            {
                return result;
            }

            T? grandChild = GetChildOfType<T>(child);
            if (grandChild is not null)
            {
                return grandChild;
            }
        }

        return null;
    }
}
