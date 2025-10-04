using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Windows.Frontend;
using JL.Windows.Interop;

namespace JL.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>

// ReSharper disable once MemberCanBeFileLocal
internal sealed partial class App
{
    public App()
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        TaskScheduler.UnobservedTaskException += LogUnobservedTaskException;

        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        if (!HasModifyPermission(AppContext.BaseDirectory))
        {
            LoggerManager.Logger.Information(
                """
                JL is installed in a secure location that requires admin rights to modify files.
                If you'd rather not give admin rights to JL, consider installing it in a location where they're not needed (e.g., the desktop).
                """);

            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas"
            });

            Shutdown();
            return;
        }

        if (!IsSingleInstance(out nint windowHandleOfRunningInstance))
        {
            WinApi.RestoreWindow(windowHandleOfRunningInstance);
            Shutdown();
            return;
        }

        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableImplicitTouchKeyboardInvocation", true);
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        GUI.MainWindow mainWindow = new();
        FrontendManager.Frontend = new WindowsFrontend(mainWindow);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private static bool IsSingleInstance(out nint windowHandleOfRunningInstance)
    {
        Process currentProcess = Process.GetCurrentProcess();
        ReadOnlySpan<Process> processes = Process.GetProcessesByName(currentProcess.ProcessName);

        if (processes.Length > 1)
        {
            foreach (Process process in processes)
            {
                if (currentProcess.Id != process.Id)
                {
                    try
                    {
                        if (Environment.ProcessPath?.Equals(process.MainModule?.FileName, StringComparison.OrdinalIgnoreCase) ?? false)
                        {
                            windowHandleOfRunningInstance = process.MainWindowHandle;
                            return false;
                        }
                    }
                    catch (Win32Exception e)
                    {
                        LoggerManager.Logger.Information(e, "Couldn't get the path of the process, probably because it has a higher integrity level than this instance of JL");
                    }
                }
            }
        }

        windowHandleOfRunningInstance = 0;
        return true;
    }

    private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        LoggerManager.Logger.Fatal((Exception)args.ExceptionObject, "Unhandled exception");
    }

    private static void LogUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        LoggerManager.Logger.Fatal(args.Exception, "Unobserved task exception");
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        await ((GUI.MainWindow)MainWindow).HandleAppClosing().ConfigureAwait(false);
    }

    private static bool HasModifyPermission(string folderPath)
    {
        try
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                return true;
            }

            if (identity.User is null)
            {
                LoggerManager.Logger.Error("Current WindowsIdentity is null, cannot check modify permission for folder: {FolderPath}", folderPath);
                return false;
            }

            DirectoryInfo directoryInfo = new(folderPath);
            DirectorySecurity acl = directoryInfo.GetAccessControl();
            AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));

            bool allowModify = false;
            foreach (FileSystemAccessRule rule in rules.Cast<FileSystemAccessRule>())
            {
                if (rule.IdentityReference.Value == identity.User.Value
                    || principal.IsInRole((SecurityIdentifier)rule.IdentityReference))
                {
                    if (rule.FileSystemRights.HasFlag(FileSystemRights.Modify))
                    {
                        if (rule.AccessControlType is AccessControlType.Deny)
                        {
                            return false;
                        }

                        if (rule.AccessControlType is AccessControlType.Allow)
                        {
                            allowModify = true;
                        }
                    }
                }
            }

            return allowModify;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error checking modify permission for folder: {FolderPath}", folderPath);
            return false;
        }
    }

}
