using System.Diagnostics;
using System.Windows;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;

namespace JL.Windows.GUI.UserControls;

internal sealed partial class FreqOptionsControl
{
    public FreqOptionsControl()
    {
        InitializeComponent();
    }

    public FreqOptions GetFreqOptions(FreqType type)
    {
        UseDBOption useDBOption;
        if (UseDBOption.ValidFreqTypes.Contains(type))
        {
            Debug.Assert(UseDBCheckBox.IsChecked is not null);
            useDBOption = new UseDBOption(UseDBCheckBox.IsChecked.Value);
        }
        else
        {
            useDBOption = new UseDBOption(false);
        }

        HigherValueMeansHigherFrequencyOption higherValueMeansHigherFrequencyOption;
        if (HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(type))
        {
            Debug.Assert(HigherValueMeansHigherFrequencyCheckBox.IsChecked is not null);
            higherValueMeansHigherFrequencyOption = new HigherValueMeansHigherFrequencyOption(HigherValueMeansHigherFrequencyCheckBox.IsChecked.Value);
        }
        else
        {
            higherValueMeansHigherFrequencyOption = new HigherValueMeansHigherFrequencyOption(false);
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
            UseDBCheckBox.IsChecked = freq.Options.UseDB.Value;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }

        if (HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(freq.Type))
        {
            HigherValueMeansHigherFrequencyCheckBox.IsChecked = freq.Options.HigherValueMeansHigherFrequency.Value;
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
