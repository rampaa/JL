using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.UserControls;

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

    public DictOptions GetDictOptions(DictType type)
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
        if (AutoUpdateAfterNDaysOption.ValidDictTypes.Contains(type))
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
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dictType))
        {
            NewlineCheckBox.IsChecked = dictOptions?.NewlineBetweenDefinitions?.Value ?? true;
            NewlineCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            NewlineCheckBox.Visibility = Visibility.Collapsed;
        }

        if (NoAllOption.ValidDictTypes.Contains(dictType))
        {
            NoAllCheckBox.IsChecked = dictOptions?.NoAll.Value ?? false;
            NoAllCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            NoAllCheckBox.Visibility = Visibility.Collapsed;
        }

        if (PitchAccentMarkerColorOption.ValidDictTypes.Contains(dictType))
        {
            Brush pitchAccentMarkerBrush = dictOptions?.PitchAccentMarkerColor?.Value is not null
                ? WindowsUtils.FrozenBrushFromHex(dictOptions.PitchAccentMarkerColor.Value)
                : DictOptionManager.PitchAccentMarkerColor;

            PitchAccentMarkerColorButton.Background = pitchAccentMarkerBrush;
            PitchAccentMarkerColorDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            PitchAccentMarkerColorDockPanel.Visibility = Visibility.Collapsed;
        }

        if (WordClassInfoOption.ValidDictTypes.Contains(dictType))
        {
            WordClassInfoCheckBox.IsChecked = dictOptions?.WordClassInfo?.Value ?? true;
            WordClassInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            WordClassInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (DialectInfoOption.ValidDictTypes.Contains(dictType))
        {
            DialectInfoCheckBox.IsChecked = dictOptions?.DialectInfo?.Value ?? true;
            DialectInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            DialectInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (POrthographyInfoOption.ValidDictTypes.Contains(dictType))
        {
            POrthographyInfoCheckBox.IsChecked = dictOptions?.POrthographyInfo?.Value ?? true;
            POrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            POrthographyInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (POrthographyInfoColorOption.ValidDictTypes.Contains(dictType))
        {
            Brush pOrthographyInfoBrush = dictOptions?.POrthographyInfoColor?.Value is not null
                ? WindowsUtils.FrozenBrushFromHex(dictOptions.POrthographyInfoColor.Value)
                : DictOptionManager.POrthographyInfoColor;

            POrthographyInfoColorButton.Background = pOrthographyInfoBrush;
            POrthographyInfoColorDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            POrthographyInfoColorDockPanel.Visibility = Visibility.Collapsed;
        }

        if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(dictType))
        {
            POrthographyInfoFontSizeNumericUpDown.Value = dictOptions?.POrthographyInfoFontSize?.Value ?? 15;
            POrthographyInfoFontSizeDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            POrthographyInfoFontSizeDockPanel.Visibility = Visibility.Collapsed;
        }

        if (AOrthographyInfoOption.ValidDictTypes.Contains(dictType))
        {
            AOrthographyInfoCheckBox.IsChecked = dictOptions?.AOrthographyInfo?.Value ?? true;
            AOrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            AOrthographyInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (ROrthographyInfoOption.ValidDictTypes.Contains(dictType))
        {
            ROrthographyInfoCheckBox.IsChecked = dictOptions?.ROrthographyInfo?.Value ?? true;
            ROrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            ROrthographyInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (WordTypeInfoOption.ValidDictTypes.Contains(dictType))
        {
            WordTypeInfoCheckBox.IsChecked = dictOptions?.WordTypeInfo?.Value ?? true;
            WordTypeInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            WordTypeInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(dictType))
        {
            SpellingRestrictionInfoCheckBox.IsChecked = dictOptions?.SpellingRestrictionInfo?.Value ?? true;
            SpellingRestrictionInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            SpellingRestrictionInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(dictType))
        {
            ExtraDefinitionInfoCheckBox.IsChecked = dictOptions?.ExtraDefinitionInfo?.Value ?? true;
            ExtraDefinitionInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            ExtraDefinitionInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (MiscInfoOption.ValidDictTypes.Contains(dictType))
        {
            MiscInfoCheckBox.IsChecked = dictOptions?.MiscInfo?.Value ?? true;
            MiscInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            MiscInfoCheckBox.Visibility = Visibility.Collapsed;
        }

        if (LoanwordEtymologyOption.ValidDictTypes.Contains(dictType))
        {
            LoanwordEtymologyCheckBox.IsChecked = dictOptions?.LoanwordEtymology?.Value ?? true;
            LoanwordEtymologyCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            LoanwordEtymologyCheckBox.Visibility = Visibility.Collapsed;
        }

        if (RelatedTermOption.ValidDictTypes.Contains(dictType))
        {
            RelatedTermCheckBox.IsChecked = dictOptions?.RelatedTerm?.Value ?? false;
            RelatedTermCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            RelatedTermCheckBox.Visibility = Visibility.Collapsed;
        }

        if (AntonymOption.ValidDictTypes.Contains(dictType))
        {
            AntonymCheckBox.IsChecked = dictOptions?.Antonym?.Value ?? false;
            AntonymCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            AntonymCheckBox.Visibility = Visibility.Collapsed;
        }

        if (UseDBOption.ValidDictTypes.Contains(dictType))
        {
            UseDBCheckBox.IsChecked = dictOptions?.UseDB.Value ?? true;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            UseDBCheckBox.Visibility = Visibility.Collapsed;
        }

        if (ShowPitchAccentWithDottedLinesOption.ValidDictTypes.Contains(dictType))
        {
            ShowPitchAccentWithDottedLinesCheckBox.IsChecked = dictOptions?.ShowPitchAccentWithDottedLines?.Value ?? true;
            ShowPitchAccentWithDottedLinesCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            ShowPitchAccentWithDottedLinesCheckBox.Visibility = Visibility.Collapsed;
        }

        if (AutoUpdateAfterNDaysOption.ValidDictTypes.Contains(dictType))
        {
            AutoUpdateAfterNDaysNumericUpDown.Value = dictOptions?.AutoUpdateAfterNDays?.Value ?? 0;
            AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Collapsed;
        }

        if (ShowImagesOption.ValidDictTypes.Contains(dictType))
        {
            ShowImagesCheckBox.IsChecked = dictOptions?.ShowImagesOption?.Value ?? true;
            ShowImagesCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }
        else
        {
            ShowImagesCheckBox.Visibility = Visibility.Collapsed;
        }

        if (showDictOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
