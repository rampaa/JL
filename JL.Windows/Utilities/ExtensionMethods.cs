using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JL.Windows.Utilities;

internal static class ExtensionMethods
{
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

    public static T? GetChildByName<T>(this DependencyObject parent, string childName) where T : DependencyObject
    {
        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
            {
                if (child is FrameworkElement frameworkElement)
                {
                    if (frameworkElement.Name == childName)
                    {
                        return t;
                    }
                }
            }
            else
            {
                T? tChild = GetChildByName<T>(child, childName);
                if (tChild is not null)
                {
                    return tChild;
                }
            }
        }

        return null;
    }

    public static void SetIsReadOnly(this TextBox textBox, bool isReadOnly)
    {
        textBox.IsReadOnly = isReadOnly;
        textBox.IsUndoEnabled = !isReadOnly;
        textBox.AcceptsReturn = !isReadOnly;
        textBox.AcceptsTab = !isReadOnly;
        textBox.UndoLimit = isReadOnly ? 0 : -1;
    }
}
