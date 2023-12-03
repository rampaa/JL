using System.Runtime;
using System.Windows;
using JL.Core.Utilities;

namespace JL.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
internal sealed partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += static (_, eventArgs) =>
        {
            Exception ex = (Exception)eventArgs.ExceptionObject;
            Utils.Logger.Fatal(ex, "Unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += static (_, eventArgs) => Utils.Logger.Fatal(eventArgs.Exception, "Unobserved task exception");

        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");
    }
}
