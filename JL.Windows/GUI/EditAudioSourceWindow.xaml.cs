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
/// Interaction logic for EditAudioSourceWindow.xaml
/// </summary>
internal sealed partial class EditAudioSourceWindow
{
    private readonly string _uri;
    private readonly AudioSource _audioSource;

#pragma warning disable CA1054 // URI-like parameters should not be strings
    public EditAudioSourceWindow(string uri, AudioSource audioSource)
#pragma warning restore CA1054 // URI-like parameters should not be strings
    {
        _uri = uri;
        _audioSource = audioSource;
        InitializeComponent();

        string type = _audioSource.Type.GetDescription();
        _ = AudioSourceTypeComboBox.Items.Add(type);
        AudioSourceTypeComboBox.SelectedValue = type;

        switch (_audioSource.Type)
        {
            case AudioSourceType.Url:
            case AudioSourceType.UrlJson:
                PathType.Text = "URL";
                UriTextBox.Text = _uri;
                UriTextBox.Visibility = Visibility.Visible;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Collapsed;
                break;

            case AudioSourceType.LocalPath:
                PathType.Text = "Path";
                UriTextBox.Text = _uri;
                UriTextBox.Visibility = Visibility.Visible;
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

                UriTextBox.Visibility = Visibility.Collapsed;
                TextToSpeechVoicesComboBox.Visibility = Visibility.Visible;
                break;

            default:
                LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(AudioSourceType), nameof(EditAudioSourceWindow), nameof(EditAudioSourceWindow), _audioSource.Type);
                WindowsUtils.Alert(AlertLevel.Error, $"Invalid audio source type: {_audioSource.Type}");
                break;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        UriTextBox.ClearValue(BorderBrushProperty);
        UriTextBox.ClearValue(CursorProperty);
        UriTextBox.ClearValue(ToolTipProperty);
        TextToSpeechVoicesComboBox.ClearValue(BorderBrushProperty);
        TextToSpeechVoicesComboBox.ClearValue(CursorProperty);
        TextToSpeechVoicesComboBox.ClearValue(ToolTipProperty);

        string? audioSourceTypeStr = AudioSourceTypeComboBox.SelectionBoxItem.ToString();
        Debug.Assert(audioSourceTypeStr is not null);

        AudioSourceType type = audioSourceTypeStr.GetEnum<AudioSourceType>();
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

                if (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri))
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

                if (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri))
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

                if (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri))
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
            if (_uri != uri)
            {
                AudioUtils.AudioSources.Add(uri, _audioSource);
                _ = AudioUtils.AudioSources.Remove(_uri);
            }

            Close();
        }
    }
}
