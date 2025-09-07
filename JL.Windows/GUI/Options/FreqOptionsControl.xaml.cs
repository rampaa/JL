using System.Diagnostics;
using System.Windows;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;

namespace JL.Windows.GUI.Options;

internal sealed partial class FreqOptionsControl
{
    public FreqOptionsControl()
    {
        InitializeComponent();
    }

    public FreqOptions GetFreqOptions(FreqType type, bool autoUpdatable)
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

        AutoUpdateAfterNDaysOption? autoUpdateAfterNDaysOption = null;
        if (autoUpdatable && AutoUpdateAfterNDaysOption.ValidFreqTypes.Contains(type))
        {
            autoUpdateAfterNDaysOption = new AutoUpdateAfterNDaysOption(double.ConvertToIntegerNative<int>(AutoUpdateAfterNDaysNumericUpDown.Value));
        }

        FreqOptions options = new(useDBOption, higherValueMeansHigherFrequencyOption, autoUpdateAfterNDaysOption);

        return options;
    }

    public void GenerateFreqOptionsElements(FreqType freqType, FreqOptions? freqOptions)
    {
        bool showFreqOptions = false;
        if (UseDBOption.ValidFreqTypes.Contains(freqType))
        {
            UseDBCheckBox.IsChecked = freqOptions?.UseDB.Value ?? true;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }
        else
        {
            UseDBCheckBox.Visibility = Visibility.Collapsed;
        }

        if (HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(freqType))
        {
            HigherValueMeansHigherFrequencyCheckBox.IsChecked = freqOptions?.HigherValueMeansHigherFrequency.Value ?? false;
            HigherValueMeansHigherFrequencyCheckBox.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }
        else
        {
            HigherValueMeansHigherFrequencyCheckBox.Visibility = Visibility.Collapsed;
        }

        if (AutoUpdateAfterNDaysOption.ValidFreqTypes.Contains(freqType))
        {
            AutoUpdateAfterNDaysNumericUpDown.Value = freqOptions?.AutoUpdateAfterNDays?.Value ?? 0;
            AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Visible;
            showFreqOptions = true;
        }
        else
        {
            AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Collapsed;
        }

        if (showFreqOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
