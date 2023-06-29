using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Utilities;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for EditAudioSourceWindow.xaml
/// </summary>
internal sealed partial class EditAudioSourceWindow : Window
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
        TextBlockUri.Text = _uri;
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
            AudioSourceTypeComboBox.ClearValue(BorderBrushProperty);
        }

        AudioSourceType? type = null;
        if (!string.IsNullOrEmpty(typeString))
        {
            type = typeString.GetEnum<AudioSourceType>();
        }

        string uri = TextBlockUri.Text.Replace("://localhost", "://127.0.0.1", StringComparison.Ordinal);

        if (type is AudioSourceType.LocalPath)
        {
            if (Path.IsPathFullyQualified(uri)
                && Directory.Exists(Path.GetDirectoryName(uri))
                && !string.IsNullOrEmpty(Path.GetFileName(uri)))
            {
                string relativePath = Path.GetRelativePath(Utils.ApplicationPath, uri);
                uri = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;

                if (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri))
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
                 || !Uri.IsWellFormedUriString(uri.Replace("{Term}", "", StringComparison.Ordinal).Replace("{Reading}", "", StringComparison.Ordinal), UriKind.Absolute)
                 || (_uri != uri && AudioUtils.AudioSources.ContainsKey(uri)))
        {
            TextBlockUri.BorderBrush = Brushes.Red;
            isValid = false;
        }

        if (isValid)
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
