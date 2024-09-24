using System.ComponentModel;
using System.Diagnostics;
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

        Environment.CurrentDirectory = AppContext.BaseDirectory;

        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");

        if (IsSingleInstance())
        {
            StartupUri = new Uri("GUI/MainWindow.xaml", UriKind.Relative);
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }

    private bool IsSingleInstance()
    {
        Process currentProcess = Process.GetCurrentProcess();
        Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

        if (processes.Length > 1)
        {
            for (int i = 0; i < processes.Length; i++)
            {
                Process process = processes[i];
                if (currentProcess.Id != process.Id)
                {
                    try
                    {
                        if (process.MainModule?.FileName == Environment.ProcessPath)
                        {
                            WinApi.RestoreWindow(process.MainWindowHandle);
                            Shutdown();
                            return false;
                        }
                    }
                    catch (Win32Exception e)
                    {
                        Utils.Logger.Information(e, "Couldn't get the path of the process, probably because it has a higher integrity level than this instance of JL");
                    }
                }
            }
        }

        return true;
    }

    private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        Utils.Logger.Fatal((Exception)args.ExceptionObject, "Unhandled exception");
    }

    private static void LogUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Utils.Logger.Fatal(args.Exception, "Unobserved task exception");
    }
}
