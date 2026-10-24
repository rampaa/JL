using System.Globalization;
using System.Windows.Media;
using JL.Core.Config;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.Config;

internal static class ConfigUtils
{
    public static Color GetColorFromConfig(SqliteConnection connection, Dictionary<string, string> configs, Color color, string configKey)
    {
        if (configs.TryGetValue(configKey, out string? configValue))
        {
            return WindowsUtils.ColorFromHex(configValue);
        }

        ConfigDBManager.InsertSetting(connection, configKey, color.ToString(CultureInfo.InvariantCulture));
        return color;
    }

    public static Brush GetBrushFromConfig(SqliteConnection connection, Dictionary<string, string> configs, Brush solidColorBrush, string configKey)
    {
        if (configs.TryGetValue(configKey, out string? configValue))
        {
            return WindowsUtils.BrushFromHex(configValue);
        }

        ConfigDBManager.InsertSetting(connection, configKey, solidColorBrush.ToString(CultureInfo.InvariantCulture));
        return solidColorBrush;
    }

    public static Brush GetFrozenBrushFromConfig(SqliteConnection connection, Dictionary<string, string> configs, Brush solidColorBrush, string configKey)
    {
        return GetFrozenBrushFromConfig(connection, configs, solidColorBrush, 1, configKey);
    }

    public static Brush GetFrozenBrushFromConfig(SqliteConnection connection, Dictionary<string, string> configs, Brush solidColorBrush, double brushOpacity, string configKey)
    {
        if (configs.TryGetValue(configKey, out string? configValue))
        {
            Color color = WindowsUtils.ColorFromHex(configValue);
            if (solidColorBrush is SolidColorBrush { IsFrozen: true } currentBrush
                && currentBrush.Color == color
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                && currentBrush.Opacity == brushOpacity)
            {
                return currentBrush;
            }

            SolidColorBrush brush = new(color)
            {
                Opacity = brushOpacity
            };

            brush.Freeze();
            return brush;
        }

        ConfigDBManager.InsertSetting(connection, configKey, solidColorBrush.ToString(CultureInfo.InvariantCulture));
        return solidColorBrush;
    }
}
