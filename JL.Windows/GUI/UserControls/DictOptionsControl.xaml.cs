using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.UserControls;

internal sealed partial class DictOptionsControl : UserControl
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
            newlineOption = new NewlineBetweenDefinitionsOption(NewlineCheckBox.IsChecked!.Value);
        }

        ExamplesOption? examplesOption = null;
        if (ExamplesOption.ValidDictTypes.Contains(type))
        {
            if (Enum.TryParse(ExamplesComboBox.SelectedValue?.ToString(), out ExamplesOptionValue eov))
            {
                examplesOption = new ExamplesOption(eov);
            }
        }

        NoAllOption? noAllOption = null;
        if (NoAllOption.ValidDictTypes.Contains(type))
        {
            noAllOption = new NoAllOption(NoAllCheckBox.IsChecked!.Value);
        }

        PitchAccentMarkerColorOption? pitchAccentMarkerColorOption = null;
        if (PitchAccentMarkerColorOption.ValidDictTypes.Contains(type))
        {
            pitchAccentMarkerColorOption = new PitchAccentMarkerColorOption(PitchAccentMarkerColorButton.Background.ToString(CultureInfo.InvariantCulture));
            DictOptionManager.PitchAccentMarkerColor = WindowsUtils.FrozenBrushFromHex(pitchAccentMarkerColorOption.Value.Value)!;
        }

        WordClassInfoOption? wordClassOption = null;
        if (WordClassInfoOption.ValidDictTypes.Contains(type))
        {
            wordClassOption = new WordClassInfoOption(WordClassInfoCheckBox.IsChecked!.Value);
        }

        DialectInfoOption? dialectOption = null;
        if (DialectInfoOption.ValidDictTypes.Contains(type))
        {
            dialectOption = new DialectInfoOption(DialectInfoCheckBox.IsChecked!.Value);
        }

        POrthographyInfoOption? pOrthographyInfoOption = null;
        if (POrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            pOrthographyInfoOption = new POrthographyInfoOption(POrthographyInfoCheckBox.IsChecked!.Value);
        }

        POrthographyInfoColorOption? pOrthographyInfoColorOption = null;
        if (POrthographyInfoColorOption.ValidDictTypes.Contains(type))
        {
            pOrthographyInfoColorOption =
                new POrthographyInfoColorOption(POrthographyInfoColorButton.Background.ToString(CultureInfo.InvariantCulture));

            DictOptionManager.POrthographyInfoColor = WindowsUtils.FrozenBrushFromHex(pOrthographyInfoColorOption.Value.Value)!;
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
            aOrthographyInfoOption = new AOrthographyInfoOption(AOrthographyInfoCheckBox.IsChecked!.Value);
        }

        ROrthographyInfoOption? rOrthographyInfoOption = null;
        if (ROrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            rOrthographyInfoOption = new ROrthographyInfoOption(ROrthographyInfoCheckBox.IsChecked!.Value);
        }

        WordTypeInfoOption? wordTypeOption = null;
        if (WordTypeInfoOption.ValidDictTypes.Contains(type))
        {
            wordTypeOption = new WordTypeInfoOption(WordTypeInfoCheckBox.IsChecked!.Value);
        }

        SpellingRestrictionInfoOption? spellingRestrictionInfo = null;
        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(type))
        {
            spellingRestrictionInfo =
                new SpellingRestrictionInfoOption(SpellingRestrictionInfoCheckBox.IsChecked!.Value);
        }

        ExtraDefinitionInfoOption? extraDefinitionInfo = null;
        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(type))
        {
            extraDefinitionInfo =
                new ExtraDefinitionInfoOption(ExtraDefinitionInfoCheckBox.IsChecked!.Value);
        }

        MiscInfoOption? miscInfoOption = null;
        if (MiscInfoOption.ValidDictTypes.Contains(type))
        {
            miscInfoOption = new MiscInfoOption(MiscInfoCheckBox.IsChecked!.Value);
        }

        LoanwordEtymologyOption? loanwordEtymology = null;
        if (LoanwordEtymologyOption.ValidDictTypes.Contains(type))
        {
            loanwordEtymology = new LoanwordEtymologyOption(LoanwordEtymologyCheckBox.IsChecked!.Value);
        }

        RelatedTermOption? relatedTermOption = null;
        if (RelatedTermOption.ValidDictTypes.Contains(type))
        {
            relatedTermOption = new RelatedTermOption(RelatedTermCheckBox.IsChecked!.Value);
        }

        AntonymOption? antonymOption = null;
        if (AntonymOption.ValidDictTypes.Contains(type))
        {
            antonymOption = new AntonymOption(AntonymCheckBox.IsChecked!.Value);
        }

        DictOptions options = new(
            newlineOption,
            examplesOption,
            noAllOption,
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
            antonymOption);

        return options;
    }

    public void GenerateDictOptionsElements(Dict dict)
    {
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dict.Type))
        {
            bool isEpwing = DictUtils.YomichanDictTypes.Concat(DictUtils.NazekaDictTypes).All(dt => dt != dict.Type);
            NewlineCheckBox.IsChecked = dict.Options?.NewlineBetweenDefinitions?.Value ?? isEpwing;
            NewlineCheckBox.Visibility = Visibility.Visible;
        }

        if (ExamplesOption.ValidDictTypes.Contains(dict.Type))
        {
            ExamplesComboBox.ItemsSource = Enum.GetValues<ExamplesOptionValue>().ToArray();
            ExamplesComboBox.SelectedValue = dict.Options?.Examples?.Value ?? ExamplesOptionValue.None;

            ExamplesDockPanel.Visibility = Visibility.Visible;
        }

        if (NoAllOption.ValidDictTypes.Contains(dict.Type))
        {
            NoAllCheckBox.IsChecked = dict.Options?.NoAll?.Value ?? false;
            NoAllCheckBox.Visibility = Visibility.Visible;
        }

        if (PitchAccentMarkerColorOption.ValidDictTypes.Contains(dict.Type))
        {
            PitchAccentMarkerColorButton.Background = DictOptionManager.PitchAccentMarkerColor;

            PitchAccentMarkerColorDockPanel.Visibility = Visibility.Visible;
        }

        if (WordClassInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            WordClassInfoCheckBox.IsChecked = dict.Options?.WordClassInfo?.Value ?? true;
            WordClassInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (DialectInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            DialectInfoCheckBox.IsChecked = dict.Options?.DialectInfo?.Value ?? true;
            DialectInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (POrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            POrthographyInfoCheckBox.IsChecked = dict.Options?.POrthographyInfo?.Value ?? true;
            POrthographyInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (POrthographyInfoColorOption.ValidDictTypes.Contains(dict.Type))
        {
            POrthographyInfoColorButton.Background = DictOptionManager.POrthographyInfoColor;

            POrthographyInfoColorDockPanel.Visibility = Visibility.Visible;
        }

        if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(dict.Type))
        {
            POrthographyInfoFontSizeNumericUpDown.Value = dict.Options?.POrthographyInfoFontSize?.Value ?? 15;
            POrthographyInfoFontSizeDockPanel.Visibility = Visibility.Visible;
        }

        if (AOrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            AOrthographyInfoCheckBox.IsChecked = dict.Options?.AOrthographyInfo?.Value ?? true;
            AOrthographyInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (ROrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            ROrthographyInfoCheckBox.IsChecked = dict.Options?.ROrthographyInfo?.Value ?? true;
            ROrthographyInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (WordTypeInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            WordTypeInfoCheckBox.IsChecked = dict.Options?.WordTypeInfo?.Value ?? true;
            WordTypeInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            SpellingRestrictionInfoCheckBox.IsChecked = dict.Options?.SpellingRestrictionInfo?.Value ?? true;
            SpellingRestrictionInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            ExtraDefinitionInfoCheckBox.IsChecked = dict.Options?.ExtraDefinitionInfo?.Value ?? true;
            ExtraDefinitionInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (MiscInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            MiscInfoCheckBox.IsChecked = dict.Options?.MiscInfo?.Value ?? true;
            MiscInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (LoanwordEtymologyOption.ValidDictTypes.Contains(dict.Type))
        {
            LoanwordEtymologyCheckBox.IsChecked = dict.Options?.LoanwordEtymology?.Value ?? true;
            LoanwordEtymologyCheckBox.Visibility = Visibility.Visible;
        }

        if (RelatedTermOption.ValidDictTypes.Contains(dict.Type))
        {
            RelatedTermCheckBox.IsChecked = dict.Options?.RelatedTerm?.Value ?? false;
            RelatedTermCheckBox.Visibility = Visibility.Visible;
        }

        if (AntonymOption.ValidDictTypes.Contains(dict.Type))
        {
            AntonymCheckBox.IsChecked = dict.Options?.Antonym?.Value ?? false;
            AntonymCheckBox.Visibility = Visibility.Visible;
        }

        if (NoAllCheckBox.Visibility is Visibility.Visible
            || PitchAccentMarkerColorDockPanel.Visibility is Visibility.Visible)
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
