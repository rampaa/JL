using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Utilities;
using JL.Windows.SpeechSynthesis;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddAudioSourceWindow.xaml
/// </summary>
internal sealed partial class AddAudioSourceWindow : Window
{
    private static readonly string[] s_audioSourceTypes = Enum.GetValues<AudioSourceType>().Select(static audioSourceType => audioSourceType.GetDescription() ?? audioSourceType.ToString()).ToArray();

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
        string? typeString = AudioSourceTypeComboBox.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            AudioSourceTypeComboBox.BorderBrush = Brushes.Red;
            return;
        }

        if (AudioSourceTypeComboBox.BorderBrush == Brushes.Red)
        {
            AudioSourceTypeComboBox.ClearValue(BorderBrushProperty);
        }

        AudioSourceType? type = null;
        if (!string.IsNullOrEmpty(typeString))
        {
            type = typeString.GetEnum<AudioSourceType>();
        }

        bool isValid = true;
        string? uri;

        switch (type)
        {
            case AudioSourceType.LocalPath:
                uri = TextBlockUri.Text;
                string fullPath = Path.GetFullPath(uri, Utils.ApplicationPath);

                if (!string.IsNullOrWhiteSpace(uri)
                    && Path.IsPathFullyQualified(fullPath)
                    && Directory.Exists(Path.GetDirectoryName(fullPath))
                    && !string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)))
                {
                    string relativePath = Path.GetRelativePath(Utils.ApplicationPath, uri);
                    uri = relativePath.StartsWith('.') ? fullPath : relativePath;

                    if (AudioUtils.AudioSources.ContainsKey(uri))
                    {
                        TextBlockUri.BorderBrush = Brushes.Red;
                        isValid = false;
                    }
                    else if (AudioSourceTypeComboBox.BorderBrush == Brushes.Red)
                    {
                        TextBlockUri.BorderBrush.ClearValue(BorderBrushProperty);
                    }
                }
                else
                {
                    TextBlockUri.BorderBrush = Brushes.Red;
                    isValid = false;
                }
                break;

            case AudioSourceType.Url:
            case AudioSourceType.UrlJson:
                uri = TextBlockUri.Text.Replace("://localhost", "://127.0.0.1", StringComparison.Ordinal);
                if (string.IsNullOrEmpty(uri)
                    || !Uri.IsWellFormedUriString(uri.Replace("{Term}", "", StringComparison.Ordinal).Replace("{Reading}", "", StringComparison.Ordinal), UriKind.Absolute)
                    || AudioUtils.AudioSources.ContainsKey(uri))
                {
                    TextBlockUri.BorderBrush = Brushes.Red;
                    isValid = false;
                }
                else if (AudioSourceTypeComboBox.BorderBrush == Brushes.Red)
                {
                    TextBlockUri.BorderBrush.ClearValue(BorderBrushProperty);
                }
                break;

            case AudioSourceType.TextToSpeech:
                uri = TextToSpeechVoicesComboBox.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(uri)
                    || AudioUtils.AudioSources.ContainsKey(uri))
                {
                    TextToSpeechVoicesComboBox.BorderBrush = Brushes.Red;
                    isValid = false;
                }
                else if (AudioSourceTypeComboBox.BorderBrush == Brushes.Red)
                {
                    TextToSpeechVoicesComboBox.BorderBrush.ClearValue(BorderBrushProperty);
                }
                break;

            default:
                isValid = false;
                uri = null;
                break;
        }

        if (isValid && !string.IsNullOrEmpty(uri))
        {
            AudioUtils.AudioSources.Add(uri,
                new AudioSource(type!.Value, true, AudioUtils.AudioSources.Count + 1));

            Close();
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AudioSourceTypeComboBox.ItemsSource = s_audioSourceTypes;
        TextToSpeechVoicesComboBox.ItemsSource = SpeechSynthesisUtils.InstalledVoices;
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        const string audioSourceTypeInfo = @"1) Local files through ""Local Path"" type:
e.g. C:\Users\User\Desktop\jpod_files\{Reading} - {Term}.mp3

2) URLs returning an audio directly through ""URL"" type:
e.g. http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={Term}&kana={Reading}

3) URLs returning a JSON response in Custom Audio List format through ""URL (JSON)"" type:
e.g. http://127.0.0.1:5050/?sources=jpod,jpod_alternate,nhk16,forvo&term={Term}&reading={Reading}

4) Windows Text to Speech:
e.g. Microsoft Haruka";

        InfoWindow infoWindow = new()
        {
            Owner = this,
            Title = "Audio Source Types",
            InfoTextBox = { Text = audioSourceTypeInfo },
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        _ = infoWindow.ShowDialog();
    }

    private void AudioSourceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AudioSourceType audioSourceType = ((ComboBox)sender).SelectedItem.ToString()!.GetEnum<AudioSourceType>();

        switch (audioSourceType)
        {
            case AudioSourceType.Url:
            case AudioSourceType.UrlJson:
                PathType.Text = "URL";
                TextBlockUri.Visibility = Visibility.Visible;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Collapsed;
                break;

            case AudioSourceType.LocalPath:
                PathType.Text = "Path";
                TextBlockUri.Visibility = Visibility.Visible;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Collapsed;
                break;

            case AudioSourceType.TextToSpeech:
                PathType.Text = "Text to Speech Voice";
                TextBlockUri.Visibility = Visibility.Collapsed;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Visible;
                break;
            default:
                throw new ArgumentOutOfRangeException(null, "Invalid AudioSourceType");
        }
    }
}
