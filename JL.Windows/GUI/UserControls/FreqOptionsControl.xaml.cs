using System.Windows;
using System.Windows.Controls;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.UserControls;

internal sealed partial class FreqOptionsControl : UserControl
{
    public FreqOptionsControl()
    {
        InitializeComponent();
    }

    private void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowColorPicker((Button)sender);
    }

    public FreqOptions GetFreqOptions(FreqType type)
    {
        UseDBOption? useDBOption = null;
        if (UseDBOption.ValidFreqTypes.Contains(type))
        {
            useDBOption = new UseDBOption(UseDBCheckBox.IsChecked!.Value);
        }

        FreqOptions options = new(useDBOption);

        return options;
    }

    public void GenerateFreqOptionsElements(Freq freq)
    {
        if (UseDBOption.ValidFreqTypes.Contains(freq.Type))
        {
            UseDBCheckBox.IsChecked = freq.Options?.UseDB?.Value ?? false;
            UseDBCheckBox.Visibility = Visibility.Visible;
        }

        if (UseDBCheckBox.Visibility is Visibility.Visible)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
