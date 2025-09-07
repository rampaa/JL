using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using JL.Core.Config;
using JL.Core.Frontend;
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

    private void LoadTermLookupCounts(SqliteConnection connection)
    {
        _sessionLookupCountsForCurrentProfile = StatsUtils.SessionStats.TermLookupCountDict.ToArray();
        _termLookupCountsForCurrentProfile = StatsDBUtils.GetTermLookupCountsFromDB(connection, ProfileUtils.CurrentProfileId);
        _termLookupCountsForLifetime = StatsDBUtils.GetTermLookupCountsFromDB(connection, ProfileUtils.GlobalProfileId);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateStatsDisplay(StatsMode.Session);

        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        StatsDBUtils.UpdateProfileLifetimeStats(connection);
        StatsDBUtils.UpdateLifetimeStats(connection);
        LoadTermLookupCounts(connection);
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

        CharactersTextBlock.Text = stats.Characters.ToString("N0", CultureInfo.InvariantCulture);
        LinesTextBlock.Text = stats.Lines.ToString("N0", CultureInfo.InvariantCulture);
        TimeTextBlock.Text = stats.Time.ToString(@"d\.hh\:mm\:ss", CultureInfo.InvariantCulture);

        CharactersPerMinuteTextBlock.Text = stats.Time.TotalMinutes > 0
            ? Math.Round(stats.Characters / stats.Time.TotalMinutes).ToString("N0", CultureInfo.InvariantCulture)
            : stats.Characters is 0
                ? "0"
                : "∞";

        CardsMinedTextBlock.Text = stats.CardsMined.ToString("N0", CultureInfo.InvariantCulture);
        TimesPlayedAudioTextBlock.Text = stats.TimesPlayedAudio.ToString("N0", CultureInfo.InvariantCulture);
        NumberOfLookupsTextBlock.Text = stats.NumberOfLookups.ToString(CultureInfo.InvariantCulture);
        ImoutosTextBlock.Text = stats.Imoutos.ToString("N0", CultureInfo.InvariantCulture);
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
                    ButtonSwapStats.Content = nameof(StatsMode.Profile);
                    break;

                case StatsMode.Profile:
                    UpdateStatsDisplay(StatsMode.Lifetime);
                    ButtonSwapStats.Content = nameof(StatsMode.Lifetime);
                    break;

                case StatsMode.Lifetime:
                    UpdateStatsDisplay(StatsMode.Session);
                    ButtonSwapStats.Content = nameof(StatsMode.Session);
                    break;

                default:
                    LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(StatsMode), nameof(StatsWindow), nameof(ButtonSwapStats_OnClick), mode);
                    WindowsUtils.Alert(AlertLevel.Error, $"Invalid stats mode: {mode}");
                    break;
            }
        }

        else
        {
            LoggerManager.Logger.Error("Cannot parse {SwapButtonText} into a StatsMode enum", ButtonSwapStats.Content.ToString());
        }
    }

    private void ButtonResetStats_OnClick(object sender, RoutedEventArgs e)
    {
        string? statType = ButtonSwapStats.Content.ToString();
        Debug.Assert(statType is not null);

#pragma warning disable CA1308 // Normalize strings to uppercase
        statType = statType.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase

        if (!WindowsUtils.ShowYesNoDialog(
                $"Are you really sure that you want to reset the {statType} stats?",
                string.Create(CultureInfo.InvariantCulture, $"Reset {ButtonSwapStats.Content} Stats?"), this))
        {
            return;
        }


        if (Enum.TryParse(ButtonSwapStats.Content.ToString(), out StatsMode statsMode))
        {
            using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();

            StatsUtils.ResetStats(connection, statsMode);
            if (statsMode is StatsMode.Lifetime)
            {
                StatsDBUtils.UpdateLifetimeStats(connection);
            }

            else if (statsMode is StatsMode.Profile)
            {
                StatsDBUtils.UpdateProfileLifetimeStats(connection);
            }

            LoadTermLookupCounts(connection);

            UpdateStatsDisplay(statsMode);
        }

        else
        {
            LoggerManager.Logger.Error("Cannot parse {SwapButtonText} into a StatsMode enum", ButtonSwapStats.Content.ToString());
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
            LoggerManager.Logger.Error("Cannot parse {SwapButtonText} into a StatsMode enum", ButtonSwapStats.Content.ToString());
        }
    }
}
