using System.Runtime;
using System.Windows;

namespace JL.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
internal sealed partial class App : Application
{
    public App()
    {
        ProfileOptimization.SetProfileRoot(AppContext.BaseDirectory);
        ProfileOptimization.StartProfile("Startup.Profile");
    }
}
