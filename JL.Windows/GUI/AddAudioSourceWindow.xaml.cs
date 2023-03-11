using System.Windows;
using System.Windows.Media;
using JL.Core;
using JL.Core.Audio;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Path = System.IO.Path;

namespace JL.Windows.GUI;
/// <summary>
/// Interaction logic for AddAudioSourceWindow.xaml
/// </summary>
internal sealed partial class AddAudioSourceWindow : Window
{
    public AddAudioSourceWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool isValid = true;

        string? typeString = AudioSourceTypeComboBox.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            AudioSourceTypeComboBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (AudioSourceTypeComboBox.BorderBrush == Brushes.Red)
        {
            AudioSourceTypeComboBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        AudioSourceType? type = null;
        if (!string.IsNullOrEmpty(typeString))
        {
            type = typeString.GetEnum<AudioSourceType>();
        }

        string uri = TextBlockUri.Text.Replace("://localhost", "://127.0.0.1");

        if (type == AudioSourceType.LocalPath)
        {
            if (Path.IsPathFullyQualified(uri))
            {
                string relativePath = Path.GetRelativePath(Storage.ApplicationPath, uri);
                uri = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;
            }
            else
            {
                TextBlockUri.BorderBrush = Brushes.Red;
                isValid = false;
            }
        }

        else if (string.IsNullOrEmpty(uri)
            || !Uri.IsWellFormedUriString(uri.Replace("{Term}", "").Replace("{Reading}", ""), UriKind.Absolute)
            || Storage.AudioSources.ContainsKey(uri))
        {
            TextBlockUri.BorderBrush = Brushes.Red;
            isValid = false;
        }

        if (isValid)
        {
            Storage.AudioSources.Add(uri,
                new AudioSource(type!.Value, true, Storage.AudioSources.Count + 1));

            Close();
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AudioSourceTypeComboBox.ItemsSource = Enum.GetValues<AudioSourceType>().Select(static audioSourceType => audioSourceType.GetDescription() ?? audioSourceType.ToString());
    }
}
