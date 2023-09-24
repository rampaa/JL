using System.Configuration;

namespace JL.Windows.Utilities;
internal static class ExtensionMethods
{
    public static string? Get(this KeyValueConfigurationCollection configurationCollection, string key)
    {
        return configurationCollection[key]?.Value ?? null;
    }

    //public static T? GetChildOfType<T>(this DependencyObject dependencyObject) where T : DependencyObject
    //{
    //    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
    //    {
    //        DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);

    //        if (child is T result)
    //        {
    //            return result;
    //        }

    //        if (child is not null)
    //        {
    //            T? grandChild = GetChildOfType<T>(child);

    //            if (grandChild is not null)
    //            {
    //                return grandChild;
    //            }
    //        }
    //    }

    //    return null;
    //}
}
