using System.IO;
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

        if (type is AudioSourceType.LocalPath)
        {
            if (Path.IsPathFullyQualified(uri)
                && Directory.Exists(Path.GetDirectoryName(uri))
                && !string.IsNullOrEmpty(Path.GetFileName(uri)))
            {
                string relativePath = Path.GetRelativePath(Storage.ApplicationPath, uri);
                uri = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;

                if (Storage.AudioSources.ContainsKey(uri))
                {
                    TextBlockUri.BorderBrush = Brushes.Red;
                    isValid = false;
                }
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

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        const string audioSourceTypeInfo = @"1) Local files through ""Local Path"" type:
e.g. C:\Users\User\Desktop\jpod_files\{Reading} - {Term}.mp3

2) URLs returning an audio directly through ""URL"" type:
e.g. http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={Term}&kana={Reading}

3) URLs returning a JSON response in Custom Audio List format through ""URL (JSON)"" type:
e.g. http://127.0.0.1:5050/?sources=jpod,jpod_alternate,nhk16,forvo&term={Term}&reading={Reading}";

        InfoWindow infoWindow = new()
        {
            Owner = this,
            Title = "Audio Source Types",
            InfoTextBox = { Text = audioSourceTypeInfo },
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        _ = infoWindow.ShowDialog();
    }
}
