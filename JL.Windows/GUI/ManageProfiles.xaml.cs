using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Profile;
using JL.Core.Statistics;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageProfilesWindow.xaml
/// </summary>
internal sealed partial class ManageProfilesWindow : Window
{
    private nint _windowHandle;
    public ManageProfilesWindow()
    {
        InitializeComponent();
        UpdateProfilesDisplay();
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

    private async void Window_Closed(object sender, EventArgs e)
    {
        await ProfileUtils.SerializeProfiles().ConfigureAwait(false);
    }

    private void UpdateProfilesDisplay()
    {
        List<DockPanel> resultDockPanels = [];

        foreach (string profile in ProfileUtils.Profiles)
        {
            DockPanel dockPanel = new();

            TextBlock profileNameTextBlock = new()
            {
                Width = 350,
                Text = profile,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };

            bool defaultOrCurrentProfile = ProfileUtils.DefaultProfiles.Contains(profile) || profile == ProfileUtils.CurrentProfile;

            Button removeButton = new()
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1),
                Visibility = defaultOrCurrentProfile
                    ? Visibility.Collapsed
                    : Visibility.Visible,
                Tag = profile
            };
            removeButton.Click += RemoveButton_Click;

            resultDockPanels.Add(dockPanel);

            _ = dockPanel.Children.Add(profileNameTextBlock);
            _ = dockPanel.Children.Add(removeButton);
        }

        ProfileListBox.ItemsSource = resultDockPanels;
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowsUtils.ShowYesNoDialog("Do you really want to remove this profile?", "Confirmation"))
        {
            string profile = (string)((Button)sender).Tag;

            _ = ProfileUtils.Profiles.Remove(profile);
            PreferencesWindow.Instance.ProfileComboBox.ItemsSource = ProfileUtils.Profiles.ToList();

            string profilePath = ProfileUtils.GetProfilePath(profile);
            if (File.Exists(profilePath))
            {
                File.Delete(profilePath);
            }

            string profileCustomNamesPath = ProfileUtils.GetProfileCustomNameDictPath(profile);
            if (File.Exists(profileCustomNamesPath))
            {
                File.Delete(profileCustomNamesPath);
            }

            string profileCustomWordsPath = ProfileUtils.GetProfileCustomWordDictPath(profile);
            if (File.Exists(profileCustomWordsPath))
            {
                File.Delete(profileCustomWordsPath);
            }

            string statsPath = StatsUtils.GetStatsPath(profile);
            if (File.Exists(statsPath))
            {
                File.Delete(statsPath);
            }

            UpdateProfilesDisplay();
        }
    }

    private void AddProfileButton_Click(object sender, RoutedEventArgs e)
    {
        _ = new AddProfileWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
        UpdateProfilesDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
