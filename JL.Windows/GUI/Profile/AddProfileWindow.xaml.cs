using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Config;
using JL.Core.Statistics;
using Microsoft.Data.Sqlite;

namespace JL.Windows.GUI.Profile;

/// <summary>
/// Interaction logic for AddProfileWindow.xaml
/// </summary>
internal sealed partial class AddProfileWindow
{
    public AddProfileWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ProfileNameTextBox.ClearValue(BorderBrushProperty);
        ProfileNameTextBox.ToolTip = "Profile name must be unique";

        string profileName = ProfileNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(profileName)
            || profileName.Length > 128
            || profileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ProfileNameTextBox.BorderBrush = Brushes.Red;
            ProfileNameTextBox.ToolTip = "Invalid profile name!";
            return;
        }

        if (ProfileDBUtils.ProfileExists(profileName))
        {
            ProfileNameTextBox.BorderBrush = Brushes.Red;
            ProfileNameTextBox.ToolTip = "Profile name must be unique!";
            return;
        }

        if (ProfileNameTextBox.BorderBrush == Brushes.Red)
        {
            ProfileNameTextBox.ClearValue(BorderBrushProperty);
        }

        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(true))
        {
            ProfileDBUtils.InsertProfile(connection, profileName);
            int currentProfileId = ProfileDBUtils.GetProfileId(connection, profileName);
            ConfigDBManager.CopyProfileSettings(connection, ProfileUtils.CurrentProfileId, currentProfileId);
            StatsDBUtils.InsertStats(connection, new Stats(), currentProfileId);
            PreferencesWindow.Instance.ProfileComboBox.ItemsSource = ProfileDBUtils.GetProfileNames(connection);
        }

        Close();

        _ = Directory.CreateDirectory(ProfileUtils.ProfileFolderPath);
        string customNamePath = ProfileUtils.GetProfileCustomNameDictPath(profileName);
        if (!File.Exists(customNamePath))
        {
            await File.Create(customNamePath).DisposeAsync().ConfigureAwait(false);
        }

        string customWordPath = ProfileUtils.GetProfileCustomWordDictPath(profileName);
        if (!File.Exists(customWordPath))
        {
            await File.Create(customWordPath).DisposeAsync().ConfigureAwait(false);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _ = ProfileNameTextBox.Focus();
    }
}
