using System.Globalization;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace JL.Core.Utilities;
public static class LoggerManager
{
    internal static readonly LoggingLevelSwitch s_loggingLevelSwitch = new()
    {
        MinimumLevel = LogEventLevel.Error
    };

    public static readonly Logger Logger = new LoggerConfiguration()
        .MinimumLevel.ControlledBy(s_loggingLevelSwitch)
        .WriteTo.File(Path.Join(AppInfo.ApplicationPath, "Logs", "log.txt"),
            formatProvider: CultureInfo.InvariantCulture,
            rollingInterval: RollingInterval.Day,
            retainedFileTimeLimit: TimeSpan.FromDays(30),
            shared: true)
        .CreateLogger();
}
