using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddWordWindow.xaml
/// </summary>
internal sealed partial class AddWordWindow
{
    private static readonly string[] s_allVerbTypes =
        [
            "v-unspec",
            "v1",
            "v1-s",
            "v2a-s",
            "v2b-k",
            "v2b-s",
            "v2d-k",
            "v2d-s",
            "v2g-k",
            "v2g-s",
            "v2h-k",
            "v2h-s",
            "v2k-k",
            "v2k-s",
            "v2m-k",
            "v2m-s",
            "v2n-s",
            "v2r-k",
            "v2r-s",
            "v2s-s",
            "v2t-k",
            "v2t-s",
            "v2w-s",
            "v2y-k",
            "v2y-s",
            "v2z-s",
            "v4b",
            "v4g",
            "v4h",
            "v4k",
            "v4m",
            "v4n",
            "v4r",
            "v4s",
            "v4t",
            "v5aru",
            "v5b",
            "v5g",
            "v5k",
            "v5k-s",
            "v5m",
            "v5n",
            "v5r",
            "v5r-i",
            "v5s",
            "v5t",
            "v5u",
            "v5u-s",
            "v5uru",
            "vi",
            "vk",
            "vn",
            "vr",
            "vs",
            "vs-c",
            "vs-i",
            "vs-s",
            "vt",
            "vz"
        ];

    private static readonly string[] s_allAdjectiveTypes =
        [
            "adj-f",
            "adj-i",
            "adj-ix",
            "adj-kari",
            "adj-ku",
            "adj-na",
            "adj-nari",
            "adj-no",
            "adj-pn",
            "adj-shiku",
            "adj-t"
        ];

    private static AddWordWindow? s_instance;

    public static AddWordWindow Instance => s_instance ??= new AddWordWindow();

    private AddWordWindow()
    {
        InitializeComponent();
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void SaveButton_Click(object? sender, RoutedEventArgs? e)
    {
        await HandleSaveButtonClick().ConfigureAwait(false);
    }

    private Task HandleSaveButtonClick()
    {
        DefinitionsTextBox.ClearValue(BorderBrushProperty);
        DefinitionsTextBox.ClearValue(CursorProperty);
        DefinitionsTextBox.ClearValue(ToolTipProperty);
        WordClassTextBox.ClearValue(BorderBrushProperty);
        WordClassTextBox.ClearValue(CursorProperty);
        WordClassTextBox.ClearValue(ToolTipProperty);

        string rawSpellings = SpellingsTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);
        string[] spellings = rawSpellings.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (spellings.Length is 0)
        {
            SpellingsTextBox.BorderBrush = Brushes.Red;
            SpellingsTextBox.Cursor = Cursors.Help;
            SpellingsTextBox.ToolTip = "Spellings field cannot be left empty!";
            return Task.CompletedTask;
        }

        string rawDefinitions = DefinitionsTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);
        string[] definitions = rawDefinitions.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (definitions.Length is 0)
        {
            DefinitionsTextBox.BorderBrush = Brushes.Red;
            DefinitionsTextBox.Cursor = Cursors.Help;
            DefinitionsTextBox.ToolTip = "Definitions field cannot be left empty!";
            return Task.CompletedTask;
        }

        string rawWordClasses = WordClassTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);
        string[]? wordClasses = rawWordClasses.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        bool noWordClass = wordClasses.Length is 0;

        string? rawPartOfSpeech = PartOfSpeechStackPanel.Children.OfType<RadioButton>()
            .First(static r => r.IsChecked.HasValue && r.IsChecked.Value).Content.ToString();
        Debug.Assert(rawPartOfSpeech is not null);

        if (WordClassStackPanel.Visibility is Visibility.Visible)
        {
            if (noWordClass)
            {
                WordClassTextBox.BorderBrush = Brushes.Red;
                WordClassTextBox.Cursor = Cursors.Help;
                WordClassTextBox.ToolTip = "Word Classes field cannot be left empty!";
                return Task.CompletedTask;
            }

            string[] validWordClasses = rawPartOfSpeech is "Verb"
                ? s_allVerbTypes
                : s_allAdjectiveTypes;

            foreach (string wordClass in wordClasses)
            {
                if (!validWordClasses.AsReadOnlySpan().Contains(wordClass))
                {
                    WordClassTextBox.BorderBrush = Brushes.Red;
                    WordClassTextBox.Cursor = Cursors.Help;
                    WordClassTextBox.ToolTip = "Invalid word class! Press the info button to see the list of valid word classes.";
                    return Task.CompletedTask;
                }
            }
        }

        if (noWordClass)
        {
            wordClasses = null;
        }

        string rawReadings = ReadingsTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);
        string[]? readings = rawReadings.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (readings.Length is 0
            || (spellings.Length is 1 && readings.Length is 1 && spellings[0] == readings[0]))
        {
            readings = null;
        }

        DictType dictType = ComboBoxDictType.SelectedValue.ToString() is "Global"
            ? DictType.CustomWordDictionary
            : DictType.ProfileCustomWordDictionary;

        Dict dict = DictUtils.SingleDictTypeDicts[dictType];
        if (dict.Active)
        {
            CustomWordLoader.AddToDictionary(spellings, readings, definitions, rawPartOfSpeech, wordClasses, dict.Contents);
        }

        PopupWindowUtils.HidePopups(0);
        Close();

        string line = string.IsNullOrWhiteSpace(rawWordClasses)
            ? $"{rawSpellings}\t{rawReadings}\t{rawDefinitions.ReplaceLineEndings("\\n")}\t{rawPartOfSpeech}\n"
            : $"{rawSpellings}\t{rawReadings}\t{rawDefinitions.ReplaceLineEndings("\\n")}\t{rawPartOfSpeech}\t{rawWordClasses}\n";

        string path = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        return File.AppendAllTextAsync(path, line);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        s_instance = null;
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
    }

    private void VerbOrAdjectiveRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        WordClassStackPanel.Visibility = Visibility.Visible;
    }

    private void OtherRadioButtons_Checked(object sender, RoutedEventArgs e)
    {
        WordClassTextBox.Text = "";
        WordClassStackPanel.Visibility = Visibility.Collapsed;
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        bool showAllowedVerbTypeInfo = PartOfSpeechStackPanel.Children.OfType<RadioButton>()
            .First(static r => r.IsChecked.HasValue && r.IsChecked.Value).Content.ToString() is "Verb";

        string[] keys = showAllowedVerbTypeInfo ? s_allVerbTypes : s_allAdjectiveTypes;
        StringBuilder sb = new(keys.Length);
        foreach (string key in keys)
        {
            if (DictUtils.JmdictEntities.TryGetValue(key, out string? value))
            {
                _ = sb.Append(CultureInfo.InvariantCulture, $"{key}: {value}\n");
            }
        }

        InfoWindow infoWindow = new()
        {
            Owner = this,
            Title = "Supported Word Classes",
            InfoTextBox =
            {
                Text = sb.ToString(0, sb.Length - 1)
            },
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        _ = infoWindow.ShowDialog();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _ = Activate();
        if (string.IsNullOrEmpty(SpellingsTextBox.Text))
        {
            _ = SpellingsTextBox.Focus();
        }
        else // if (string.IsNullOrEmpty(ReadingsTextBox.Text))
        {
            _ = ReadingsTextBox.Focus();
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter && InputMethod.Current?.ImeState is not InputMethodState.On && !DefinitionsTextBox.IsFocused)
        {
            e.Handled = true;
            await HandleSaveButtonClick().ConfigureAwait(false);
        }
    }
}
