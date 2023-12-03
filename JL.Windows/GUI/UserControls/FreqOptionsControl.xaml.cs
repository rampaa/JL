using System.Windows;
using System.Windows.Controls;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;

namespace JL.Windows.GUI.UserControls;

internal sealed partial class FreqOptionsControl : UserControl
{
    public FreqOptionsControl()
    {
        InitializeComponent();
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

    public void GenerateFreqOptionsElements(FreqType freqType)
    {
        bool showFreqOptions = false;
        if (UseDBOption.ValidFreqTypes.Contains(freqType))
        {
            UseDBCheckBox.IsChecked = false;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }

        if (showFreqOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }

    public void GenerateFreqOptionsElements(Freq freq)
    {
        bool showFreqOptions = false;
        if (UseDBOption.ValidFreqTypes.Contains(freq.Type))
        {
            UseDBCheckBox.IsChecked = freq.Options?.UseDB?.Value ?? false;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }

        if (showFreqOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
