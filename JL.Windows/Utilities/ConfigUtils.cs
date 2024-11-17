using System.Globalization;
using System.Windows.Media;
using JL.Core.Config;
using Microsoft.Data.Sqlite;

namespace JL.Windows.Utilities;
internal static class ConfigUtils
{
    public static Brush GetBrushFromConfig(SqliteConnection connection, Brush solidColorBrush, string configKey)
    {
        string? configValue = ConfigDBManager.GetSettingValue(connection, configKey);
        if (configValue is not null)
        {
            return WindowsUtils.BrushFromHex(configValue);
        }

        ConfigDBManager.InsertSetting(connection, configKey, solidColorBrush.ToString(CultureInfo.InvariantCulture));

        return solidColorBrush.IsFrozen
            ? WindowsUtils.BrushFromHex(solidColorBrush.ToString(CultureInfo.InvariantCulture))
            : solidColorBrush;
    }

    public static Color GetColorFromConfig(SqliteConnection connection, Color color, string configKey)
    {
        string? configValue = ConfigDBManager.GetSettingValue(connection, configKey);
        if (configValue is not null)
        {
            return WindowsUtils.ColorFromHex(configValue);
        }

        ConfigDBManager.InsertSetting(connection, configKey, color.ToString(CultureInfo.InvariantCulture));
        return WindowsUtils.ColorFromHex(color.ToString(CultureInfo.InvariantCulture));
    }

    public static Brush GetFrozenBrushFromConfig(SqliteConnection connection, Brush solidColorBrush, string configKey)
    {
        Brush brush = GetBrushFromConfig(connection, solidColorBrush, configKey);
        brush.Freeze();
        return brush;
    }
}
