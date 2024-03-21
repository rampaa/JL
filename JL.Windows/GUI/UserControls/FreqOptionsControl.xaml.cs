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

        HigherValueMeansHigherFrequencyOption? higherValueMeansHigherFrequencyOption = null;
        if (HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(type))
        {
            higherValueMeansHigherFrequencyOption = new HigherValueMeansHigherFrequencyOption(HigherValueMeansHigherFrequencyCheckBox.IsChecked!.Value);
        }

        FreqOptions options = new(useDBOption, higherValueMeansHigherFrequencyOption);

        return options;
    }

    public void GenerateFreqOptionsElements(FreqType freqType)
    {
        bool showFreqOptions = false;
        if (UseDBOption.ValidFreqTypes.Contains(freqType))
        {
            UseDBCheckBox.IsChecked = true;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }

        if (HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(freqType))
        {
            HigherValueMeansHigherFrequencyCheckBox.IsChecked = false;
            HigherValueMeansHigherFrequencyCheckBox.Visibility = Visibility.Visible;
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
            UseDBCheckBox.IsChecked = freq.Options?.UseDB?.Value ?? true;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }

        if (HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(freq.Type))
        {
            HigherValueMeansHigherFrequencyCheckBox.IsChecked = freq.Options?.HigherValueMeansHigherFrequency?.Value ?? false;
            HigherValueMeansHigherFrequencyCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }

        if (showFreqOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
