using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Utilities;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for EditAudioSourceWindow.xaml
/// </summary>
internal sealed partial class EditAudioSourceWindow
{
    private readonly string _uri;
    private readonly AudioSource _audioSource;

    public EditAudioSourceWindow(string uri, AudioSource audioSource)
    {
        _uri = uri;
        _audioSource = audioSource;
        InitializeComponent();

        string type = _audioSource.Type.GetDescription() ?? _audioSource.Type.ToString();
        _ = AudioSourceTypeComboBox.Items.Add(type);
        AudioSourceTypeComboBox.SelectedValue = type;

        switch (_audioSource.Type)
        {
            case AudioSourceType.Url:
            case AudioSourceType.UrlJson:
                PathType.Text = "URL";
                TextBlockUri.Text = _uri;
                TextBlockUri.Visibility = Visibility.Visible;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Collapsed;
                break;

            case AudioSourceType.LocalPath:
                PathType.Text = "Path";
                TextBlockUri.Text = _uri;
                TextBlockUri.Visibility = Visibility.Visible;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Collapsed;
                break;

            case AudioSourceType.TextToSpeech:
                PathType.Text = "Text to Speech Voice";

                if (SpeechSynthesisUtils.InstalledVoices is not null)
                {
                    TextToSpeechVoicesComboBox.ItemsSource = WindowsUtils.CloneComboBoxItems(SpeechSynthesisUtils.InstalledVoices);
                    TextToSpeechVoicesComboBox.SelectedIndex = Array.FindIndex(SpeechSynthesisUtils.InstalledVoices, iv => iv.Content.ToString() == _uri);

                    if (TextToSpeechVoicesComboBox.SelectedIndex < 0)
                    {
                        TextToSpeechVoicesComboBox.SelectedIndex = 0;
                    }
                }

                TextBlockUri.Visibility = Visibility.Collapsed;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Visible;
                break;

            default:
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(AudioSourceType), nameof(EditAudioSourceWindow), nameof(EditAudioSourceWindow), _audioSource.Type);
                Utils.Frontend.Alert(AlertLevel.Error, $"Invalid audio source type: {_audioSource.Type}");
                break;
        }
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
                    string relativePath = Path.GetRelativePath(Utils.ApplicationPath, fullPath);
                    uri = relativePath.StartsWith('.') ? fullPath : relativePath;

                    if (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri))
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
                uri = TextBlockUri.Text
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost", "://127.0.0.1", StringComparison.OrdinalIgnoreCase);
                if (string.IsNullOrEmpty(uri)
                    || !Uri.IsWellFormedUriString(uri.Replace("{Term}", "", StringComparison.OrdinalIgnoreCase).Replace("{Reading}", "", StringComparison.OrdinalIgnoreCase), UriKind.Absolute)
                    || (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri)))
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
                uri = ((ComboBoxItem?)TextToSpeechVoicesComboBox.SelectedItem)?.Content.ToString();
                if (string.IsNullOrWhiteSpace(uri)
                    || (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri)))
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
            if (_uri != uri)
            {
                AudioUtils.AudioSources.Add(uri, _audioSource);
                _ = AudioUtils.AudioSources.Remove(_uri);
            }

            Close();
        }
    }
}
