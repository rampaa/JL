using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core;
using JL.Core.Audio;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddAudioSourceWindow.xaml
/// </summary>
internal sealed partial class AddAudioSourceWindow
{
    private static readonly string[] s_audioSourceTypes = Enum.GetValues<AudioSourceType>().Select(static audioSourceType => audioSourceType.GetDescription()).ToArray();

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
        AudioSourceTypeComboBox.ClearValue(BorderBrushProperty);
        UriTextBox.ClearValue(BorderBrushProperty);
        UriTextBox.ClearValue(CursorProperty);
        UriTextBox.ClearValue(ToolTipProperty);
        TextToSpeechVoicesComboBox.ClearValue(BorderBrushProperty);
        TextToSpeechVoicesComboBox.ClearValue(CursorProperty);
        TextToSpeechVoicesComboBox.ClearValue(ToolTipProperty);

        string? typeString = AudioSourceTypeComboBox.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            AudioSourceTypeComboBox.BorderBrush = Brushes.Red;
            return;
        }

        AudioSourceType type = typeString.GetEnum<AudioSourceType>();

        string? uri;
        switch (type)
        {
            case AudioSourceType.LocalPath:
                uri = UriTextBox.Text;
                string fullPath = Path.GetFullPath(uri, AppInfo.ApplicationPath);

                if (string.IsNullOrWhiteSpace(uri)
                    || !Path.IsPathFullyQualified(fullPath)
                    || !Directory.Exists(Path.GetDirectoryName(fullPath))
                    || string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)))
                {
                    UriTextBox.BorderBrush = Brushes.Red;
                    UriTextBox.Cursor = Cursors.Help;
                    UriTextBox.ToolTip = "Invalid URI!";
                    return;
                }

                string relativePath = Path.GetRelativePath(AppInfo.ApplicationPath, fullPath);
                uri = relativePath[0] is '.'
                    ? fullPath
                    : relativePath;

                if (AudioUtils.AudioSources.ContainsKey(uri))
                {
                    UriTextBox.BorderBrush = Brushes.Red;
                    UriTextBox.Cursor = Cursors.Help;
                    UriTextBox.ToolTip = "URI must be unique!";
                    return;
                }
                break;

            case AudioSourceType.Url:
            case AudioSourceType.UrlJson:
                uri = UriTextBox.Text
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost", "://127.0.0.1", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(uri) || !Uri.IsWellFormedUriString(uri.Replace("{Term}", "", StringComparison.OrdinalIgnoreCase).Replace("{Reading}", "", StringComparison.OrdinalIgnoreCase), UriKind.Absolute))
                {
                    UriTextBox.BorderBrush = Brushes.Red;
                    UriTextBox.Cursor = Cursors.Help;
                    UriTextBox.ToolTip = "Invalid URI!";
                    return;
                }

                if (AudioUtils.AudioSources.ContainsKey(uri))
                {
                    UriTextBox.BorderBrush = Brushes.Red;
                    UriTextBox.Cursor = Cursors.Help;
                    UriTextBox.ToolTip = "URI must be unique!";
                    return;
                }
                break;

            case AudioSourceType.TextToSpeech:
                uri = ((ComboBoxItem?)TextToSpeechVoicesComboBox.SelectedItem)?.Content.ToString();
                if (string.IsNullOrWhiteSpace(uri))
                {
                    TextToSpeechVoicesComboBox.BorderBrush = Brushes.Red;
                    TextToSpeechVoicesComboBox.Cursor = Cursors.Help;
                    TextToSpeechVoicesComboBox.ToolTip = "Invalid URI!";
                    return;
                }

                if (AudioUtils.AudioSources.ContainsKey(uri))
                {
                    TextToSpeechVoicesComboBox.BorderBrush = Brushes.Red;
                    TextToSpeechVoicesComboBox.Cursor = Cursors.Help;
                    TextToSpeechVoicesComboBox.ToolTip = "URI must be unique!";
                    return;
                }
                break;

            default:
                uri = null;
                break;
        }

        if (!string.IsNullOrEmpty(uri))
        {
            AudioUtils.AudioSources.Add(uri, new AudioSource(type, true, AudioUtils.AudioSources.Count + 1));
            Close();
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AudioSourceTypeComboBox.ItemsSource = s_audioSourceTypes;
        if (SpeechSynthesisUtils.InstalledVoices is not null)
        {
            TextToSpeechVoicesComboBox.ItemsSource = WindowsUtils.CloneComboBoxItems(SpeechSynthesisUtils.InstalledVoices);
        }
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        const string audioSourceTypeInfo = """
                                           1) Local files through "Local Path" type:
                                           e.g. C:\Users\User\Desktop\jpod_files\{Reading} - {Term}.mp3

                                           2) URLs returning an audio directly through "URL" type:
                                           e.g. http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={Term}&kana={Reading}

                                           3) URLs returning a JSON response in Custom Audio List format through "URL (JSON)" type:
                                           e.g. http://127.0.0.1:5050/?sources=jpod,jpod_alternate,nhk16,forvo&term={Term}&reading={Reading}

                                           4) Windows Text to Speech:
                                           e.g. Microsoft Haruka
                                           """;

        InfoWindow infoWindow = new(audioSourceTypeInfo)
        {
            Owner = this,
            Title = "Audio Source Types",
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        _ = infoWindow.ShowDialog();
    }

    private void AudioSourceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string? audioSourceTypeStr = ((ComboBox)sender).SelectedItem.ToString();
        Debug.Assert(audioSourceTypeStr is not null);

        AudioSourceType audioSourceType = audioSourceTypeStr.GetEnum<AudioSourceType>();

        switch (audioSourceType)
        {
            case AudioSourceType.Url:
            case AudioSourceType.UrlJson:
                PathType.Text = "URL";
                UriTextBox.Visibility = Visibility.Visible;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Collapsed;
                break;

            case AudioSourceType.LocalPath:
                PathType.Text = "Path";
                UriTextBox.Visibility = Visibility.Visible;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Collapsed;
                break;

            case AudioSourceType.TextToSpeech:
                PathType.Text = "Text to Speech Voice";
                UriTextBox.Visibility = Visibility.Collapsed;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Visible;
                break;

            default:
                LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(AudioSourceType), nameof(AddAudioSourceWindow), nameof(AudioSourceTypeComboBox_SelectionChanged), audioSourceType);
                WindowsUtils.Alert(AlertLevel.Error, $"Invalid audio source type: {audioSourceType}");
                break;
        }
    }
}
