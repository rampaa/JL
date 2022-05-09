using System.Windows;
using System.Windows.Controls;
using JL.Core;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for StatsWindow.xaml
/// </summary>
public partial class StatsWindow : Window
{
    private static StatsWindow? s_instance;

    public static StatsWindow Instance
    {
        get
        {
            if (s_instance is not { IsLoaded: true })
                s_instance = new StatsWindow();

            return s_instance;
        }
    }

    public StatsWindow()
    {
        InitializeComponent();
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
}

internal enum StatsMode
{
    Session,
    Lifetime
}
