using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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

    public void GenerateDictOptionsElements(DictType dictType)
    {
        bool showDictOptions = false;
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dictType))
        {
            NewlineCheckBox.IsChecked = true;
            NewlineCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (NoAllOption.ValidDictTypes.Contains(dictType))
        {
            NoAllCheckBox.IsChecked = false;
            NoAllCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (PitchAccentMarkerColorOption.ValidDictTypes.Contains(dictType))
        {
            PitchAccentMarkerColorButton.Background = DictOptionManager.PitchAccentMarkerColor;
            PitchAccentMarkerColorDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (WordClassInfoOption.ValidDictTypes.Contains(dictType))
        {
            WordClassInfoCheckBox.IsChecked = true;
            WordClassInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (DialectInfoOption.ValidDictTypes.Contains(dictType))
        {
            DialectInfoCheckBox.IsChecked = true;
            DialectInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (POrthographyInfoOption.ValidDictTypes.Contains(dictType))
        {
            POrthographyInfoCheckBox.IsChecked = true;
            POrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (POrthographyInfoColorOption.ValidDictTypes.Contains(dictType))
        {
            POrthographyInfoColorButton.Background = DictOptionManager.POrthographyInfoColor;
            POrthographyInfoColorDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(dictType))
        {
            POrthographyInfoFontSizeNumericUpDown.Value = 15;
            POrthographyInfoFontSizeDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (AOrthographyInfoOption.ValidDictTypes.Contains(dictType))
        {
            AOrthographyInfoCheckBox.IsChecked = true;
            AOrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ROrthographyInfoOption.ValidDictTypes.Contains(dictType))
        {
            ROrthographyInfoCheckBox.IsChecked = true;
            ROrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (WordTypeInfoOption.ValidDictTypes.Contains(dictType))
        {
            WordTypeInfoCheckBox.IsChecked = true;
            WordTypeInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(dictType))
        {
            SpellingRestrictionInfoCheckBox.IsChecked = true;
            SpellingRestrictionInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(dictType))
        {
            ExtraDefinitionInfoCheckBox.IsChecked = true;
            ExtraDefinitionInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (MiscInfoOption.ValidDictTypes.Contains(dictType))
        {
            MiscInfoCheckBox.IsChecked = true;
            MiscInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (LoanwordEtymologyOption.ValidDictTypes.Contains(dictType))
        {
            LoanwordEtymologyCheckBox.IsChecked = true;
            LoanwordEtymologyCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (RelatedTermOption.ValidDictTypes.Contains(dictType))
        {
            RelatedTermCheckBox.IsChecked = false;
            RelatedTermCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (AntonymOption.ValidDictTypes.Contains(dictType))
        {
            AntonymCheckBox.IsChecked = false;
            AntonymCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (UseDBOption.ValidDictTypes.Contains(dictType))
        {
            UseDBCheckBox.IsChecked = true;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ShowPitchAccentWithDottedLinesOption.ValidDictTypes.Contains(dictType))
        {
            ShowPitchAccentWithDottedLinesCheckBox.IsChecked = true;
            ShowPitchAccentWithDottedLinesCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (AutoUpdateAfterNDaysOption.ValidDictTypes.Contains(dictType))
        {
            AutoUpdateAfterNDaysNumericUpDown.Value = 0;
            AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ShowImagesOption.ValidDictTypes.Contains(dictType))
        {
            ShowImagesCheckBox.IsChecked = true;
            ShowImagesCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (showDictOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }

    public void GenerateDictOptionsElements(Dict dict)
    {
        bool showDictOptions = false;
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.NewlineBetweenDefinitions is not null);
            NewlineCheckBox.IsChecked = dict.Options.NewlineBetweenDefinitions.Value;
            NewlineCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (NoAllOption.ValidDictTypes.Contains(dict.Type))
        {
            NoAllCheckBox.IsChecked = dict.Options.NoAll.Value;
            NoAllCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (PitchAccentMarkerColorOption.ValidDictTypes.Contains(dict.Type))
        {
            PitchAccentMarkerColorButton.Background = DictOptionManager.PitchAccentMarkerColor;
            PitchAccentMarkerColorDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (WordClassInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.WordClassInfo is not null);
            WordClassInfoCheckBox.IsChecked = dict.Options.WordClassInfo.Value;
            WordClassInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (DialectInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.DialectInfo is not null);
            DialectInfoCheckBox.IsChecked = dict.Options.DialectInfo.Value;
            DialectInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (POrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.POrthographyInfo is not null);
            POrthographyInfoCheckBox.IsChecked = dict.Options.POrthographyInfo.Value;
            POrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (POrthographyInfoColorOption.ValidDictTypes.Contains(dict.Type))
        {
            POrthographyInfoColorButton.Background = DictOptionManager.POrthographyInfoColor;
            POrthographyInfoColorDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.POrthographyInfoFontSize is not null);
            POrthographyInfoFontSizeNumericUpDown.Value = dict.Options.POrthographyInfoFontSize.Value;
            POrthographyInfoFontSizeDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (AOrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.AOrthographyInfo is not null);
            AOrthographyInfoCheckBox.IsChecked = dict.Options.AOrthographyInfo.Value;
            AOrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ROrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.ROrthographyInfo is not null);
            ROrthographyInfoCheckBox.IsChecked = dict.Options.ROrthographyInfo.Value;
            ROrthographyInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (WordTypeInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.WordTypeInfo is not null);
            WordTypeInfoCheckBox.IsChecked = dict.Options.WordTypeInfo.Value;
            WordTypeInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.SpellingRestrictionInfo is not null);
            SpellingRestrictionInfoCheckBox.IsChecked = dict.Options.SpellingRestrictionInfo.Value;
            SpellingRestrictionInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.ExtraDefinitionInfo is not null);
            ExtraDefinitionInfoCheckBox.IsChecked = dict.Options.ExtraDefinitionInfo.Value;
            ExtraDefinitionInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (MiscInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.MiscInfo is not null);
            MiscInfoCheckBox.IsChecked = dict.Options.MiscInfo.Value;
            MiscInfoCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (LoanwordEtymologyOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.LoanwordEtymology is not null);
            LoanwordEtymologyCheckBox.IsChecked = dict.Options.LoanwordEtymology.Value;
            LoanwordEtymologyCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (RelatedTermOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.RelatedTerm is not null);
            RelatedTermCheckBox.IsChecked = dict.Options.RelatedTerm.Value;
            RelatedTermCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (AntonymOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.Antonym is not null);
            AntonymCheckBox.IsChecked = dict.Options.Antonym.Value;
            AntonymCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (UseDBOption.ValidDictTypes.Contains(dict.Type))
        {
            UseDBCheckBox.IsChecked = dict.Options.UseDB.Value;
            UseDBCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ShowPitchAccentWithDottedLinesOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.ShowPitchAccentWithDottedLines is not null);
            ShowPitchAccentWithDottedLinesCheckBox.IsChecked = dict.Options.ShowPitchAccentWithDottedLines.Value;
            ShowPitchAccentWithDottedLinesCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (AutoUpdateAfterNDaysOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.AutoUpdateAfterNDays is not null);
            AutoUpdateAfterNDaysNumericUpDown.Value = dict.Options.AutoUpdateAfterNDays.Value;
            AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (ShowImagesOption.ValidDictTypes.Contains(dict.Type))
        {
            Debug.Assert(dict.Options.ShowImagesOption is not null);
            ShowImagesCheckBox.IsChecked = dict.Options.ShowImagesOption.Value;
            ShowImagesCheckBox.Visibility = Visibility.Visible;
            showDictOptions = true;
        }

        if (showDictOptions)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
