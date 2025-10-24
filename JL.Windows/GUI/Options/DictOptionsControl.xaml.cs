using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Windows.Config;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.Options;

internal sealed partial class DictOptionsControl
{
    public DictOptionsControl()
    {
        InitializeComponent();
    }

    private void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowColorPicker((Button)sender);
    }

    public DictOptions GetDictOptions(DictType type, bool autoUpdatable)
    {
        NewlineBetweenDefinitionsOption? newlineOption = null;
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(NewlineCheckBox.IsChecked is not null);
            newlineOption = new NewlineBetweenDefinitionsOption(NewlineCheckBox.IsChecked.Value);
        }

        NoAllOption noAllOption;
        if (NoAllOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(NoAllCheckBox.IsChecked is not null);
            noAllOption = new NoAllOption(NoAllCheckBox.IsChecked.Value);
        }
        else
        {
            noAllOption = new NoAllOption(false);
        }

        PitchAccentMarkerColorOption? pitchAccentMarkerColorOption = null;
        if (PitchAccentMarkerColorOption.ValidDictTypes.Contains(type))
        {
            pitchAccentMarkerColorOption = new PitchAccentMarkerColorOption(PitchAccentMarkerColorButton.Background.ToString(CultureInfo.InvariantCulture));
            DictOptionManager.PitchAccentMarkerColor = WindowsUtils.FrozenBrushFromHex(pitchAccentMarkerColorOption.Value);
        }

        WordClassInfoOption? wordClassOption = null;
        if (WordClassInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(WordClassInfoCheckBox.IsChecked is not null);
            wordClassOption = new WordClassInfoOption(WordClassInfoCheckBox.IsChecked.Value);
        }

        DialectInfoOption? dialectOption = null;
        if (DialectInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(DialectInfoCheckBox.IsChecked is not null);
            dialectOption = new DialectInfoOption(DialectInfoCheckBox.IsChecked.Value);
        }

        POrthographyInfoOption? pOrthographyInfoOption = null;
        if (POrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(POrthographyInfoCheckBox.IsChecked is not null);
            pOrthographyInfoOption = new POrthographyInfoOption(POrthographyInfoCheckBox.IsChecked.Value);
        }

        POrthographyInfoColorOption? pOrthographyInfoColorOption = null;
        if (POrthographyInfoColorOption.ValidDictTypes.Contains(type))
        {
            pOrthographyInfoColorOption =
                new POrthographyInfoColorOption(POrthographyInfoColorButton.Background.ToString(CultureInfo.InvariantCulture));

            DictOptionManager.POrthographyInfoColor = WindowsUtils.FrozenBrushFromHex(pOrthographyInfoColorOption.Value);
        }

        POrthographyInfoFontSizeOption? pOrthographyInfoFontSize = null;
        if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(type))
        {
            pOrthographyInfoFontSize =
                new POrthographyInfoFontSizeOption(POrthographyInfoFontSizeNumericUpDown.Value);
        }

        AOrthographyInfoOption? aOrthographyInfoOption = null;
        if (AOrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(AOrthographyInfoCheckBox.IsChecked is not null);
            aOrthographyInfoOption = new AOrthographyInfoOption(AOrthographyInfoCheckBox.IsChecked.Value);
        }

        ROrthographyInfoOption? rOrthographyInfoOption = null;
        if (ROrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(ROrthographyInfoCheckBox.IsChecked is not null);
            rOrthographyInfoOption = new ROrthographyInfoOption(ROrthographyInfoCheckBox.IsChecked.Value);
        }

        WordTypeInfoOption? wordTypeOption = null;
        if (WordTypeInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(WordTypeInfoCheckBox.IsChecked is not null);
            wordTypeOption = new WordTypeInfoOption(WordTypeInfoCheckBox.IsChecked.Value);
        }

        SpellingRestrictionInfoOption? spellingRestrictionInfo = null;
        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(SpellingRestrictionInfoCheckBox.IsChecked is not null);
            spellingRestrictionInfo = new SpellingRestrictionInfoOption(SpellingRestrictionInfoCheckBox.IsChecked.Value);
        }

        ExtraDefinitionInfoOption? extraDefinitionInfo = null;
        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(ExtraDefinitionInfoCheckBox.IsChecked is not null);
            extraDefinitionInfo = new ExtraDefinitionInfoOption(ExtraDefinitionInfoCheckBox.IsChecked.Value);
        }

        MiscInfoOption? miscInfoOption = null;
        if (MiscInfoOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(MiscInfoCheckBox.IsChecked is not null);
            miscInfoOption = new MiscInfoOption(MiscInfoCheckBox.IsChecked.Value);
        }

        LoanwordEtymologyOption? loanwordEtymology = null;
        if (LoanwordEtymologyOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(LoanwordEtymologyCheckBox.IsChecked is not null);
            loanwordEtymology = new LoanwordEtymologyOption(LoanwordEtymologyCheckBox.IsChecked.Value);
        }

        RelatedTermOption? relatedTermOption = null;
        if (RelatedTermOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(RelatedTermCheckBox.IsChecked is not null);
            relatedTermOption = new RelatedTermOption(RelatedTermCheckBox.IsChecked.Value);
        }

        AntonymOption? antonymOption = null;
        if (AntonymOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(AntonymCheckBox.IsChecked is not null);
            antonymOption = new AntonymOption(AntonymCheckBox.IsChecked.Value);
        }

        UseDBOption useDBOption;
        if (UseDBOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(UseDBCheckBox.IsChecked is not null);
            useDBOption = new UseDBOption(UseDBCheckBox.IsChecked.Value);
        }
        else
        {
            useDBOption = new UseDBOption(false);
        }

        ShowPitchAccentWithDottedLinesOption? showPitchAccentWithDottedLines = null;
        if (ShowPitchAccentWithDottedLinesOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(ShowPitchAccentWithDottedLinesCheckBox.IsChecked is not null);
            showPitchAccentWithDottedLines = new ShowPitchAccentWithDottedLinesOption(ShowPitchAccentWithDottedLinesCheckBox.IsChecked.Value);
        }

        AutoUpdateAfterNDaysOption? autoUpdateAfterNDaysOption = null;
        if (autoUpdatable && AutoUpdateAfterNDaysOption.ValidDictTypes.Contains(type))
        {
            autoUpdateAfterNDaysOption = new AutoUpdateAfterNDaysOption(double.ConvertToIntegerNative<int>(AutoUpdateAfterNDaysNumericUpDown.Value));
        }

        ShowImagesOption? showImagesOption = null;
        if (ShowImagesOption.ValidDictTypes.Contains(type))
        {
            Debug.Assert(ShowImagesCheckBox.IsChecked is not null);
            showImagesOption = new ShowImagesOption(ShowImagesCheckBox.IsChecked.Value);
        }

        DictOptions options = new(
            useDBOption,
            noAllOption,
            newlineOption,
            pitchAccentMarkerColorOption,
            wordClassOption,
            dialectOption,
            pOrthographyInfoOption,
            pOrthographyInfoColorOption,
            pOrthographyInfoFontSize,
            aOrthographyInfoOption,
            rOrthographyInfoOption,
            wordTypeOption,
            spellingRestrictionInfo,
            extraDefinitionInfo,
            miscInfoOption,
            loanwordEtymology,
            relatedTermOption,
            antonymOption,
            showPitchAccentWithDottedLines,
            autoUpdateAfterNDaysOption,
            showImagesOption);

        return options;
    }

    public void GenerateDictOptionsElements(DictType dictType, DictOptions? dictOptions)
    {
        bool showDictOptions = false;
        OptionUtils.ChangeVisibilityOfCheckBox(NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dictType), NewlineCheckBox, dictOptions?.NewlineBetweenDefinitions?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(NoAllOption.ValidDictTypes.Contains(dictType), NoAllCheckBox, dictOptions?.NoAll.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(WordClassInfoOption.ValidDictTypes.Contains(dictType), WordClassInfoCheckBox, dictOptions?.WordClassInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(DialectInfoOption.ValidDictTypes.Contains(dictType), DialectInfoCheckBox, dictOptions?.DialectInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(POrthographyInfoOption.ValidDictTypes.Contains(dictType), POrthographyInfoCheckBox, dictOptions?.POrthographyInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(AOrthographyInfoOption.ValidDictTypes.Contains(dictType), AOrthographyInfoCheckBox, dictOptions?.AOrthographyInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(ROrthographyInfoOption.ValidDictTypes.Contains(dictType), ROrthographyInfoCheckBox, dictOptions?.ROrthographyInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(WordTypeInfoOption.ValidDictTypes.Contains(dictType), WordTypeInfoCheckBox, dictOptions?.WordTypeInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(SpellingRestrictionInfoOption.ValidDictTypes.Contains(dictType), SpellingRestrictionInfoCheckBox, dictOptions?.SpellingRestrictionInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(ExtraDefinitionInfoOption.ValidDictTypes.Contains(dictType), ExtraDefinitionInfoCheckBox, dictOptions?.ExtraDefinitionInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(MiscInfoOption.ValidDictTypes.Contains(dictType), MiscInfoCheckBox, dictOptions?.MiscInfo?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(LoanwordEtymologyOption.ValidDictTypes.Contains(dictType), LoanwordEtymologyCheckBox, dictOptions?.LoanwordEtymology?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(RelatedTermOption.ValidDictTypes.Contains(dictType), RelatedTermCheckBox, dictOptions?.RelatedTerm?.Value ?? false, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(AntonymOption.ValidDictTypes.Contains(dictType), AntonymCheckBox, dictOptions?.Antonym?.Value ?? false, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(UseDBOption.ValidDictTypes.Contains(dictType), UseDBCheckBox, dictOptions?.UseDB.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(ShowPitchAccentWithDottedLinesOption.ValidDictTypes.Contains(dictType), ShowPitchAccentWithDottedLinesCheckBox, dictOptions?.ShowPitchAccentWithDottedLines?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfCheckBox(ShowImagesOption.ValidDictTypes.Contains(dictType), ShowImagesCheckBox, dictOptions?.ShowImages?.Value ?? true, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfColorButton(PitchAccentMarkerColorOption.ValidDictTypes.Contains(dictType), PitchAccentMarkerColorButton, PitchAccentMarkerColorDockPanel, dictOptions?.PitchAccentMarkerColor?.Value, DictOptionManager.PitchAccentMarkerColor, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfColorButton(POrthographyInfoColorOption.ValidDictTypes.Contains(dictType), POrthographyInfoColorButton, POrthographyInfoColorDockPanel, dictOptions?.POrthographyInfoColor?.Value, DictOptionManager.POrthographyInfoColor, ref showDictOptions);
        OptionUtils.ChangeVisibilityOfNumericUpDown(POrthographyInfoFontSizeOption.ValidDictTypes.Contains(dictType), POrthographyInfoFontSizeNumericUpDown, POrthographyInfoFontSizeDockPanel, dictOptions?.POrthographyInfoFontSize?.Value ?? 15, ref showDictOptions);
        // OptionUtils.ChangeVisibilityOfNumericUpDown(AutoUpdateAfterNDaysOption.ValidDictTypes.Contains(dictType), AutoUpdateAfterNDaysNumericUpDown, AutoUpdateAfterNDaysDockPanel, dictOptions?.AutoUpdateAfterNDays?.Value ?? 0, ref showOptions);

        if (showDictOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
