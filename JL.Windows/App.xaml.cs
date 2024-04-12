using System.Runtime;
using System.Windows;
using JL.Core.Utilities;

namespace JL.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>

// ReSharper disable once MemberCanBeFileLocal
internal sealed partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        TaskScheduler.UnobservedTaskException += LogUnobservedTaskException;

        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");
    }

    private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        Exception ex = (Exception)args.ExceptionObject;
        Utils.Logger.Fatal(ex, "Unhandled exception");
    }

    private static void LogUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Utils.Logger.Fatal(args.Exception, "Unobserved task exception");
    }
}
