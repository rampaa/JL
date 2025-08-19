using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using JL.Core.Utilities;

namespace JL.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>

// ReSharper disable once MemberCanBeFileLocal
internal sealed partial class App
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
        TaskScheduler.UnobservedTaskException += LogUnobservedTaskException;

        Environment.CurrentDirectory = AppContext.BaseDirectory;

        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");

        if (!HasModifyPermission(AppContext.BaseDirectory))
        {
            Utils.Logger.Information(
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

        if (IsSingleInstance())
        {
            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableImplicitTouchKeyboardInvocation", true);

            StartupUri = new Uri("GUI/MainWindow.xaml", UriKind.Relative);
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }

    private bool IsSingleInstance()
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

    // ReSharper disable once AsyncVoidMethod
    private async void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        await GUI.MainWindow.Instance.HandleAppClosing().ConfigureAwait(false);
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
                Utils.Logger.Error("Current WindowsIdentity is null, cannot check modify permission for folder: {FolderPath}", folderPath);
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
            Utils.Logger.Error(ex, "Error checking modify permission for folder: {FolderPath}", folderPath);
            return false;
        }
    }

}
