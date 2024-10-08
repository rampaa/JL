using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Config;
using JL.Core.Statistics;
using Microsoft.Data.Sqlite;

namespace JL.Windows.GUI;

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
        string profileName = ProfileNameTextBox.Text.Trim();
        bool isValid = !string.IsNullOrWhiteSpace(profileName)
                       && profileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                       && profileName.Length < 128
                       && !ProfileDBUtils.ProfileExists(profileName);

        if (!isValid)
        {
            ProfileNameTextBox.BorderBrush = Brushes.Red;
        }

        else
        {
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
            await File.Create(ProfileUtils.GetProfileCustomNameDictPath(profileName)).DisposeAsync().ConfigureAwait(false);
            await File.Create(ProfileUtils.GetProfileCustomWordDictPath(profileName)).DisposeAsync().ConfigureAwait(false);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _ = ProfileNameTextBox.Focus();
    }
}
