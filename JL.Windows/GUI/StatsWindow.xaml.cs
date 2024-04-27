using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using JL.Core.Config;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for StatsWindow.xaml
/// </summary>
internal sealed partial class StatsWindow : Window
{
    private static StatsWindow? s_instance;
    private nint _windowHandle;

    public static StatsWindow Instance => s_instance ??= new StatsWindow();

    private StatsWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
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

        using SqliteConnection connection = ConfigDBManager.CreateDBConnection();
        StatsDBUtils.UpdateProfileLifetimeStats(connection);
        StatsDBUtils.UpdateLifetimeStats(connection);
    }

    private void UpdateStatsDisplay(StatsMode mode)
    {
        Stats stats = mode switch
        {
            StatsMode.Session => Stats.SessionStats,
            StatsMode.Profile => Stats.ProfileLifetimeStats,
            StatsMode.Lifetime => Stats.LifetimeStats,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        TextBlockCharacters.Text = stats.Characters.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockLines.Text = stats.Lines.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockTime.Text = stats.Time.ToString(@"d\.hh\:mm\:ss", CultureInfo.InvariantCulture);
        TextBlockCharactersPerMinute.Text = Math.Round(stats.Characters / stats.Time.TotalMinutes).ToString("N0", CultureInfo.InvariantCulture);
        TextBlockCardsMined.Text = stats.CardsMined.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockTimesPlayedAudio.Text = stats.TimesPlayedAudio.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockImoutos.Text = stats.Imoutos.ToString("N0", CultureInfo.InvariantCulture);
    }

    private void ButtonSwapStats_OnClick(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        if (Enum.TryParse(button.Content.ToString(), out StatsMode mode))
        {
            switch (mode)
            {
                case StatsMode.Session:
                    UpdateStatsDisplay(StatsMode.Profile);
                    button.Content = StatsMode.Profile.ToString();
                    break;
                case StatsMode.Profile:
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

    private void ButtonResetStats_OnClick(object sender, RoutedEventArgs e)
    {
#pragma warning disable CA1308 // Normalize strings to uppercase
        if (WindowsUtils.ShowYesNoDialog(
                $"Are you really sure that you want to reset the {ButtonSwapStats.Content.ToString()!.ToLowerInvariant()} stats?",
                string.Create(CultureInfo.InvariantCulture, $"Reset {ButtonSwapStats.Content} Stats?")))
        {
            if (Enum.TryParse(ButtonSwapStats.Content.ToString(), out StatsMode statsMode))
            {
                Stats.ResetStats(statsMode);

                if (statsMode is StatsMode.Lifetime)
                {
                    StatsDBUtils.UpdateLifetimeStats();
                }

                else if (statsMode is StatsMode.Profile)
                {
                    StatsDBUtils.UpdateProfileLifetimeStats();
                }

                UpdateStatsDisplay(statsMode);
            }

            else
            {
                Utils.Logger.Error("Cannot parse {SwapButtonText} into a StatsMode enum", ButtonSwapStats.Content.ToString());
            }
        }
#pragma warning restore CA1308 // Normalize strings to uppercase
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        s_instance = null;
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
    }
}
