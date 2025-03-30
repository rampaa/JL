using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
internal sealed partial class StatsWindow
{
    private static StatsWindow? s_instance;
    private nint _windowHandle;

    private KeyValuePair<string, int>[]? _sessionLookupCountsForCurrentProfile;
    private List<KeyValuePair<string, int>>? _termLookupCountsForCurrentProfile;
    private List<KeyValuePair<string, int>>? _termLookupCountsForLifetime;

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

        if (ConfigManager.Instance.Focusable)
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

        _sessionLookupCountsForCurrentProfile = StatsUtils.SessionStats.TermLookupCountDict.ToArray();

        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        StatsDBUtils.UpdateProfileLifetimeStats(connection);
        StatsDBUtils.UpdateLifetimeStats(connection);

        _termLookupCountsForCurrentProfile = StatsDBUtils.GetTermLookupCountsFromDB(connection, ProfileUtils.CurrentProfileId);
        _termLookupCountsForLifetime = StatsDBUtils.GetTermLookupCountsFromDB(connection, ProfileUtils.GlobalProfileId);
    }

    private void UpdateStatsDisplay(StatsMode mode)
    {
        Stats stats = mode switch
        {
            StatsMode.Session => StatsUtils.SessionStats,
            StatsMode.Profile => StatsUtils.ProfileLifetimeStats,
            StatsMode.Lifetime => StatsUtils.LifetimeStats,
            _ => StatsUtils.SessionStats
        };

        TextBlockCharacters.Text = stats.Characters.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockLines.Text = stats.Lines.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockTime.Text = stats.Time.ToString(@"d\.hh\:mm\:ss", CultureInfo.InvariantCulture);

        TextBlockCharactersPerMinute.Text = stats.Time.TotalMinutes > 0
            ? Math.Round(stats.Characters / stats.Time.TotalMinutes).ToString("N0", CultureInfo.InvariantCulture)
            : stats.Characters is 0
                ? "0"
                : "∞";

        TextBlockCardsMined.Text = stats.CardsMined.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockTimesPlayedAudio.Text = stats.TimesPlayedAudio.ToString("N0", CultureInfo.InvariantCulture);
        TextBlockNumberOfLookups.Text = stats.NumberOfLookups.ToString(CultureInfo.InvariantCulture);
        TextBlockImoutos.Text = stats.Imoutos.ToString("N0", CultureInfo.InvariantCulture);
        ShowTermLookupCountsButton.IsEnabled = CoreConfigManager.Instance.TrackTermLookupCounts;
    }

    private void ButtonSwapStats_OnClick(object sender, RoutedEventArgs e)
    {
        if (Enum.TryParse(ButtonSwapStats.Content.ToString(), out StatsMode mode))
        {
            switch (mode)
            {
                case StatsMode.Session:
                    UpdateStatsDisplay(StatsMode.Profile);
                    ButtonSwapStats.Content = StatsMode.Profile.ToString();
                    break;

                case StatsMode.Profile:
                    UpdateStatsDisplay(StatsMode.Lifetime);
                    ButtonSwapStats.Content = StatsMode.Lifetime.ToString();
                    break;

                case StatsMode.Lifetime:
                    UpdateStatsDisplay(StatsMode.Session);
                    ButtonSwapStats.Content = StatsMode.Session.ToString();
                    break;

                default:
                    Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(StatsMode), nameof(StatsWindow), nameof(ButtonSwapStats_OnClick), mode);
                    Utils.Frontend.Alert(AlertLevel.Error, $"Invalid stats mode: {mode}");
                    break;
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
        if (!WindowsUtils.ShowYesNoDialog(
                $"Are you really sure that you want to reset the {ButtonSwapStats.Content.ToString()!.ToLowerInvariant()} stats?",
                string.Create(CultureInfo.InvariantCulture, $"Reset {ButtonSwapStats.Content} Stats?")))
        {
            return;
        }
#pragma warning restore CA1308 // Normalize strings to uppercase

        if (Enum.TryParse(ButtonSwapStats.Content.ToString(), out StatsMode statsMode))
        {
            StatsUtils.ResetStats(statsMode);

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

    private void Window_Closed(object sender, EventArgs e)
    {
        s_instance = null;
        _sessionLookupCountsForCurrentProfile = null;
        _termLookupCountsForCurrentProfile = null;
        _termLookupCountsForLifetime = null;

        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
    }

    private void ShowTermLookupCountsButton_Click(object sender, RoutedEventArgs e)
    {
        InfoDataGridWindow infoDataGridWindow = new()
        {
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        DataGridTextColumn termColumn = new()
        {
            Header = "Term",
            Binding = new Binding("Key")
        };

        DataGridTextColumn frequencyColumn = new()
        {
            Header = "Count",
            Binding = new Binding("Value")
        };

        infoDataGridWindow.InfoDataGrid.Columns.Add(termColumn);
        infoDataGridWindow.InfoDataGrid.Columns.Add(frequencyColumn);

        if (Enum.TryParse(ButtonSwapStats.Content.ToString(), out StatsMode mode))
        {
            IList<KeyValuePair<string, int>>? termLookupCounts = mode switch
            {
                StatsMode.Session => _sessionLookupCountsForCurrentProfile,
                StatsMode.Profile => _termLookupCountsForCurrentProfile,
                StatsMode.Lifetime => _termLookupCountsForLifetime,
                _ => _sessionLookupCountsForCurrentProfile
            };

            infoDataGridWindow.InfoDataGrid.ItemsSource = termLookupCounts;

            _ = infoDataGridWindow.ShowDialog();
        }

        else
        {
            Utils.Logger.Error("Cannot parse {SwapButtonText} into a StatsMode enum", ButtonSwapStats.Content.ToString());
        }
    }
}
