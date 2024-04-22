using System.IO;
using System.Windows;
using System.Windows.Media;
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
                       && profileName.Length < 256
                       && profileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                       && !ProfileUtils.Profiles.Contains(profileName, StringComparer.OrdinalIgnoreCase);

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

            ProfileUtils.Profiles.Add(profileName);
            PreferencesWindow.Instance.ProfileComboBox.ItemsSource = ProfileUtils.Profiles.ToList();

            _ = Directory.CreateDirectory(ProfileUtils.ProfileFolderPath);

            File.Copy(ProfileUtils.GetProfilePath(ProfileUtils.CurrentProfile), ProfileUtils.GetProfilePath(profileName));
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
