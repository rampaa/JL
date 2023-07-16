using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for StatsWindow.xaml
/// </summary>
internal sealed partial class StatsWindow : Window
{
    private static StatsWindow? s_instance;
    private IntPtr _windowHandle;

    public static StatsWindow Instance => s_instance ??= new StatsWindow();

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

        TextBlockCharacters.Text = stats.Characters.ToString(CultureInfo.InvariantCulture);
        TextBlockLines.Text = stats.Lines.ToString(CultureInfo.InvariantCulture);
        TextBlockTime.Text = stats.Time.ToString(@"d\.hh\:mm\:ss", CultureInfo.InvariantCulture);
        TextBlockCharactersPerMinute.Text = Math.Round(stats.Characters / stats.Time.TotalMinutes).ToString(CultureInfo.InvariantCulture);
        TextBlockCardsMined.Text = stats.CardsMined.ToString(CultureInfo.InvariantCulture);
        TextBlockTimesPlayedAudio.Text = stats.TimesPlayedAudio.ToString(CultureInfo.InvariantCulture);
        TextBlockImoutos.Text = stats.Imoutos.ToString(CultureInfo.InvariantCulture);
    }

    private async void ButtonSwapStats_OnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (Enum.TryParse(button.Content.ToString(), out StatsMode mode))
        {
            switch (mode)
            {
                case StatsMode.Session:
                    await Stats.SerializeLifetimeStats().ConfigureAwait(true);
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

        else
        {
            Utils.Logger.Error("Cannot parse {SwapButtonText} into a StatsMode enum", ButtonSwapStats.Content.ToString());
        }
    }

#pragma warning disable CA1308
    private async void ButtonResetStats_OnClick(object sender, RoutedEventArgs e)
    {
        if (Utils.Frontend.ShowYesNoDialog(
                string.Create(CultureInfo.InvariantCulture, $"Are you really sure that you want to reset the {ButtonSwapStats.Content.ToString()!.ToLowerInvariant()} stats?"),
                string.Create(CultureInfo.InvariantCulture, $"Reset {ButtonSwapStats.Content} Stats?")))
        {
            if (Enum.TryParse(ButtonSwapStats.Content.ToString(), out StatsMode statsMode))
            {
                Stats.ResetStats(statsMode);

                if (statsMode is StatsMode.Lifetime)
                {
                    await Stats.SerializeLifetimeStats().ConfigureAwait(true);
                }

                UpdateStatsDisplay(statsMode);
            }

            else
            {
                Utils.Logger.Error("Cannot parse {SwapButtonText} into a StatsMode enum", ButtonSwapStats.Content.ToString());
            }
        }
    }
#pragma warning restore CA1308

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
        s_instance = null;
    }
}
