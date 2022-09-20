using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JL.Windows.GUI.Converter;

// https://stackoverflow.com/a/5182660
public class BooleanConverter<T> : IValueConverter
{
    public BooleanConverter(T trueValue, T falseValue)
    {
        True = trueValue;
        False = falseValue;
    }

    public T True { get; set; }
    public T False { get; set; }

    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool)
        {
            return (value is true ? True : False)!;
        }
        else
        {
            return ((int)value > 0 ? True : False)!;
        }
    }

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
    }
}

public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
{
    public BooleanToVisibilityConverter() : base(Visibility.Visible, Visibility.Collapsed)
    {
    }
}
