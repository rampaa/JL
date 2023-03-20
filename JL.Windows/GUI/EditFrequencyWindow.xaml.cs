using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for EditFrequencyWindow.xaml
/// </summary>
internal sealed partial class EditFrequencyWindow : Window
{
    private readonly Freq _freq;

    public EditFrequencyWindow(Freq freq)
    {
        _freq = freq;
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool isValid = true;

        string path = TextBlockPath.Text;
        if (_freq.Path != path)
        {
            if (string.IsNullOrEmpty(path)
                || (!Directory.Exists(path) && !File.Exists(path))
                || Storage.FreqDicts.Values.Any(dict => dict.Path == path))
            {
                TextBlockPath.BorderBrush = Brushes.Red;
                isValid = false;
            }

            else if (TextBlockPath.BorderBrush == Brushes.Red)
            {
                TextBlockPath.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
            }
        }

        string name = NameTextBox.Text;
        if (_freq.Name != name)
        {
            if (string.IsNullOrEmpty(name)
                || Storage.FreqDicts.Values.Any(dict => dict.Name == name))
            {
                NameTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }

            else if (NameTextBox.BorderBrush == Brushes.Red)
            {
                NameTextBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
            }
        }

        if (isValid)
        {
            if (_freq.Path != path)
            {
                _freq.Path = path;
                _freq.Contents.Clear();
            }

            _freq.Name = name;

            Storage.Frontend.InvalidateDisplayCache();

            Close();
        }
    }

    private void BrowseForFrequencyFile(string filter)
    {
        OpenFileDialog openFileDialog = new() { InitialDirectory = Storage.ApplicationPath, Filter = filter };

        if (openFileDialog.ShowDialog() is true)
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, openFileDialog.FileName);
            TextBlockPath.Text = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;
        }
    }

    private void BrowseForFrequencyFolder()
    {
        using var fbd = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = Storage.ApplicationPath };

        if (fbd.ShowDialog() is System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, fbd.SelectedPath);
            TextBlockPath.Text = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string type = _freq.Type.GetDescription() ?? _freq.Type.ToString();
        _ = FreqTypeComboBox.Items.Add(type);
        FreqTypeComboBox.SelectedValue = type;
        TextBlockPath.Text = _freq.Path;
        NameTextBox.Text = _freq.Name;
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = FreqTypeComboBox.SelectionBoxItem.ToString()!;
        FreqType selectedFreqType = typeString.GetEnum<FreqType>();

        switch (selectedFreqType)
        {
            // not providing a description for the filter causes the filename returned to be empty
            case FreqType.Yomichan:
            case FreqType.YomichanKanji:
                BrowseForFrequencyFolder();
                break;

            case FreqType.Nazeka:
                BrowseForFrequencyFile("Nazeka file|*.json");
                break;

            default:
                throw new ArgumentOutOfRangeException(null, "Invalid FreqType (Add)");
        }
    }
}
