using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Config;
using JL.Core.Profile;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddProfileWindow.xaml
/// </summary>
internal sealed partial class AddProfileWindow : Window
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
                       && profileName.Length < 256
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

            ProfileDBUtils.InsertProfile(profileName);
            PreferencesWindow.Instance.ProfileComboBox.ItemsSource = ProfileDBUtils.GetProfileNames();

            _ = Directory.CreateDirectory(ProfileUtils.ProfileFolderPath);

            ConfigDBManager.CopyProfileSettings(ProfileUtils.CurrentProfileId, ProfileDBUtils.GetProfileId(profileName));
            await File.Create(ProfileUtils.GetProfileCustomNameDictPath(profileName)).DisposeAsync().ConfigureAwait(false);
            await File.Create(ProfileUtils.GetProfileCustomWordDictPath(profileName)).DisposeAsync().ConfigureAwait(false);

            Close();
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _ = ProfileNameTextBox.Focus();
    }
}
