using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyControl.Controls;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.Options;
internal static class OptionUtils
{
    public static void ChangeVisibilityOfCheckBox(bool valid, CheckBox checkBox, bool value, ref bool showOptions)
    {
        if (valid)
        {
            checkBox.IsChecked = value;
            checkBox.Visibility = Visibility.Visible;
            showOptions = true;
        }
        else
        {
            checkBox.Visibility = Visibility.Collapsed;
        }
    }

    public static void ChangeVisibilityOfColorButton(bool valid, Button button, DockPanel dockPanel, string? colorStr, Brush defaultBrush, ref bool showOptions)
    {
        if (valid)
        {
            Brush pitchAccentMarkerBrush = colorStr is not null
                ? WindowsUtils.FrozenBrushFromHex(colorStr)
                : defaultBrush;

            button.Background = pitchAccentMarkerBrush;
            dockPanel.Visibility = Visibility.Visible;
            showOptions = true;
        }
        else
        {
            dockPanel.Visibility = Visibility.Collapsed;
        }
    }

    public static void ChangeVisibilityOfNumericUpDown(bool valid, NumericUpDown numericUpDown, DockPanel dockPanel, double value, ref bool showOptions)
    {
        if (valid)
        {
            numericUpDown.Value = value;
            dockPanel.Visibility = Visibility.Visible;
            showOptions = true;
        }
        else
        {
            dockPanel.Visibility = Visibility.Collapsed;
        }
    }
}
