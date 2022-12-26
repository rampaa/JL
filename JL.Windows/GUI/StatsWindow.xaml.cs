using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using JL.Core;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for StatsWindow.xaml
/// </summary>
public partial class StatsWindow : Window
{
    private static StatsWindow? s_instance;
    private IntPtr _windowHandle;

    public static StatsWindow Instance
    {
        get { return s_instance ??= new(); }
    }

    public StatsWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        WinApi.BringToFront(_windowHandle);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (ConfigManager.Focusable)
        {
            WinApi.AllowActivation(_windowHandle);
        }
        else
        {
            WinApi.PreventActivation(_windowHandle);
        }
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateStatsDisplay(StatsMode.Session);
    }

    private void UpdateStatsDisplay(StatsMode mode)
    {
        Stats stats = mode switch
        {
            StatsMode.Session => Stats.SessionStats,
            StatsMode.Lifetime => Stats.LifetimeStats,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        TextBlockCharacters.Text = stats.Characters.ToString();
        TextBlockLines.Text = stats.Lines.ToString();
        TextBlockTime.Text = stats.Time.ToString(@"d\.hh\:mm\:ss");
        TextBlockCardsMined.Text = stats.CardsMined.ToString();
        TextBlockTimesPlayedAudio.Text = stats.TimesPlayedAudio.ToString();
        TextBlockImoutos.Text = stats.Imoutos.ToString();
    }

    private async void ButtonSwapStats_OnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (Enum.TryParse(button.Content.ToString(), out StatsMode mode))
        {
            switch (mode)
            {
                case StatsMode.Session:
                    await Stats.UpdateLifetimeStats();
                    UpdateStatsDisplay(StatsMode.Lifetime);
                    button.Content = StatsMode.Lifetime.ToString();
                    break;
                case StatsMode.Lifetime:
                    UpdateStatsDisplay(StatsMode.Session);
                    button.Content = StatsMode.Session.ToString();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(null, "StatsMode out of range");
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        WindowsUtils.UpdateMainWindowVisibility();
        MainWindow.Instance.Focus();
        s_instance = null;
    }
}

internal enum StatsMode
{
    Session,
    Lifetime
}
