using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Point = System.Windows.Point;

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

            T? grandChild = child.GetChildOfType<T>();
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
                T? tChild = child.GetChildByName<T>(childName);
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

    public static Rect ToRect(this System.Drawing.Rectangle rectangle)
    {
        return new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    public static System.Drawing.Rectangle ToRectangle(this Rect rect)
    {
        return new System.Drawing.Rectangle(double.ConvertToIntegerNative<int>(rect.X), double.ConvertToIntegerNative<int>(rect.Y), double.ConvertToIntegerNative<int>(rect.Width), double.ConvertToIntegerNative<int>(rect.Height));
    }

    public static Interop.Point ToPoint(this Point point)
    {
        return new Interop.Point(double.ConvertToIntegerNative<int>(point.X), double.ConvertToIntegerNative<int>(point.Y));
    }

    public static Point ToPoint(this Interop.Point point)
    {
        return new Point(point.X, point.Y);
    }
}
