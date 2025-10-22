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
        OptionUtils.ChangeVisibilityOfCheckBox(UseDBOption.ValidFreqTypes.Contains(freqType), UseDBCheckBox, freqOptions?.UseDB.Value ?? true, ref showFreqOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(freqType), HigherValueMeansHigherFrequencyCheckBox, freqOptions?.HigherValueMeansHigherFrequency.Value ?? false, ref showFreqOptions);
        // OptionUtils.ChangeVisibilityOfNumericUpDown(AutoUpdateAfterNDaysOption.ValidFreqTypes.Contains(freqType), AutoUpdateAfterNDaysNumericUpDown, AutoUpdateAfterNDaysDockPanel, freqOptions?.AutoUpdateAfterNDays?.Value ?? 0, ref showFreqOptions);

        if (showFreqOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
