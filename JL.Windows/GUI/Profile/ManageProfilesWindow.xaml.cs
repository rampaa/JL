using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Config;
using JL.Core.Utilities;
using JL.Windows.Config;
using JL.Windows.Interop;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.GUI.Profile;

/// <summary>
/// Interaction logic for ManageProfilesWindow.xaml
/// </summary>
internal sealed partial class ManageProfilesWindow
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

        if (ConfigManager.Instance.Focusable)
        {
            WinApi.AllowActivation(_windowHandle);
        }
        else
        {
            WinApi.PreventActivation(_windowHandle);
        }
    }

    private void UpdateProfilesDisplay()
    {
        ReadOnlySpan<string> profileNames = ProfileDBUtils.GetProfileNames();
        DockPanel[] resultDockPanels = new DockPanel[profileNames.Length];
        for (int i = 0; i < profileNames.Length; i++)
        {
            ref readonly string profileName = ref profileNames[i];

            DockPanel dockPanel = new();

            TextBlock profileNameTextBlock = new()
            {
                Width = 350,
                Text = profileName,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };

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
                Visibility = profileName == ProfileUtils.CurrentProfileName
                    ? Visibility.Collapsed
                    : Visibility.Visible,
                Tag = profileName
            };
            removeButton.Click += RemoveButton_Click;

            resultDockPanels[i] = dockPanel;

            _ = dockPanel.Children.Add(profileNameTextBlock);
            _ = dockPanel.Children.Add(removeButton);
        }

        ProfileListBox.ItemsSource = resultDockPanels;
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!WindowsUtils.ShowYesNoDialog("Do you really want to remove this profile?", "Confirmation", this))
        {
            return;
        }

        string profile = (string)((Button)sender).Tag;
        using (SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection())
        {
            ProfileDBUtils.DeleteProfile(connection, profile);
            PreferencesWindow.Instance.ProfileComboBox.ItemsSource = ProfileDBUtils.GetProfileNames(connection);
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

        string profileRegexReplacementFilePath = RegexReplacerUtils.GetProfileSpecificFilePath();
        if (File.Exists(profileRegexReplacementFilePath))
        {
            File.Delete(profileRegexReplacementFilePath);
        }

        UpdateProfilesDisplay();
    }

    private void AddProfileButton_Click(object sender, RoutedEventArgs e)
    {
        _ = new AddProfileWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();

        UpdateProfilesDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
