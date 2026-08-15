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

        GenerateMazegakiVariantsOption? generateMazegakiVariantsOption = null;
        if (GenerateMazegakiVariantsOption.ValidFreqTypes.Contains(type))
        {
            Debug.Assert(GenerateMazegakiVariantsCheckBox.IsChecked is not null);
            generateMazegakiVariantsOption = new GenerateMazegakiVariantsOption(GenerateMazegakiVariantsCheckBox.IsChecked.Value);
        }

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = null;
        if (GenerateFusejiVariantsOption.ValidFreqTypes.Contains(type))
        {
            Debug.Assert(GenerateFusejiVariantsCheckBox.IsChecked is not null);
            generateFusejiVariantsOption = new GenerateFusejiVariantsOption(GenerateFusejiVariantsCheckBox.IsChecked.Value);
        }

        MaxSearchKeyLengthForFusejiGenerationOption? maxSearchKeyLengthForFusejiGenerationOption = null;
        if (MaxSearchKeyLengthForFusejiGenerationOption.ValidFreqTypes.Contains(type))
        {
            maxSearchKeyLengthForFusejiGenerationOption = new MaxSearchKeyLengthForFusejiGenerationOption(double.ConvertToIntegerNative<int>(MaxSearchKeyLengthForFusejiGenerationNumericUpDown.Value));
        }

        MaxTotalFusejiCountOption? maxTotalFusejiCountOption = null;
        if (MaxTotalFusejiCountOption.ValidFreqTypes.Contains(type))
        {
            maxTotalFusejiCountOption = new MaxTotalFusejiCountOption(double.ConvertToIntegerNative<int>(MaxTotalFusejiCountNumericUpDown.Value));
        }

        FreqOptions options = new(useDBOption,
            higherValueMeansHigherFrequencyOption,
            autoUpdateAfterNDaysOption,
            generateMazegakiVariantsOption,
            generateFusejiVariantsOption,
            maxSearchKeyLengthForFusejiGenerationOption,
            maxTotalFusejiCountOption);

        return options;
    }

    public void GenerateFreqOptionsElements(FreqType freqType, FreqOptions? freqOptions)
    {
        bool showFreqOptions = false;
        OptionUtils.ChangeVisibilityOfCheckBox(UseDBOption.ValidFreqTypes.Contains(freqType), UseDBCheckBox, freqOptions?.UseDB.Value ?? true, ref showFreqOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(freqType), HigherValueMeansHigherFrequencyCheckBox, freqOptions?.HigherValueMeansHigherFrequency.Value ?? false, ref showFreqOptions);
        // OptionUtils.ChangeVisibilityOfNumericUpDown(AutoUpdateAfterNDaysOption.ValidFreqTypes.Contains(freqType), AutoUpdateAfterNDaysNumericUpDown, AutoUpdateAfterNDaysDockPanel, freqOptions?.AutoUpdateAfterNDays?.Value ?? 0, ref showFreqOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(HigherValueMeansHigherFrequencyOption.ValidFreqTypes.Contains(freqType), HigherValueMeansHigherFrequencyCheckBox, freqOptions?.HigherValueMeansHigherFrequency.Value ?? false, ref showFreqOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(GenerateMazegakiVariantsOption.ValidFreqTypes.Contains(freqType), GenerateMazegakiVariantsCheckBox, freqOptions?.GenerateMazegakiVariants?.Value ?? false, ref showFreqOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(GenerateFusejiVariantsOption.ValidFreqTypes.Contains(freqType), GenerateFusejiVariantsCheckBox, freqOptions?.GenerateFusejiVariants?.Value ?? false, ref showFreqOptions);
        OptionUtils.ChangeVisibilityOfNumericUpDown(MaxSearchKeyLengthForFusejiGenerationOption.ValidFreqTypes.Contains(freqType), MaxSearchKeyLengthForFusejiGenerationNumericUpDown, MaxSearchKeyLengthForFusejiGenerationDockPanel, freqOptions?.MaxSearchKeyLengthForFusejiGeneration?.Value ?? 9, ref showFreqOptions);
        OptionUtils.ChangeVisibilityOfNumericUpDown(MaxTotalFusejiCountOption.ValidFreqTypes.Contains(freqType), MaxTotalFusejiCountNumericUpDown, MaxTotalFusejiDockPanel, freqOptions?.MaxTotalFusejiCount?.Value ?? 1, ref showFreqOptions);

        if (showFreqOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
