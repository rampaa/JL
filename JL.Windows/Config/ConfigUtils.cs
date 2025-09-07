using System.Globalization;
using System.Windows.Media;
using JL.Core.Config;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.Config;
internal static class ConfigUtils
{
    public static Brush GetBrushFromConfig(SqliteConnection connection, Dictionary<string, string> configs, Brush solidColorBrush, string configKey)
    {
        if (configs.TryGetValue(configKey, out string? configValue))
        {
            return WindowsUtils.BrushFromHex(configValue);
        }

        ConfigDBManager.InsertSetting(connection, configKey, solidColorBrush.ToString(CultureInfo.InvariantCulture));

        return solidColorBrush.IsFrozen
            ? WindowsUtils.BrushFromHex(solidColorBrush.ToString(CultureInfo.InvariantCulture))
            : solidColorBrush;
    }

    public static Color GetColorFromConfig(SqliteConnection connection, Dictionary<string, string> configs, Color color, string configKey)
    {
        if (configs.TryGetValue(configKey, out string? configValue))
        {
            return WindowsUtils.ColorFromHex(configValue);
        }

        ConfigDBManager.InsertSetting(connection, configKey, color.ToString(CultureInfo.InvariantCulture));
        return WindowsUtils.ColorFromHex(color.ToString(CultureInfo.InvariantCulture));
    }

    public static Brush GetFrozenBrushFromConfig(SqliteConnection connection, Dictionary<string, string> configs, Brush solidColorBrush, string configKey)
    {
        Brush brush = GetBrushFromConfig(connection, configs, solidColorBrush, configKey);
        brush.Freeze();
        return brush;
    }
}
