using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.UserControls;

public partial class DictOptionsControl : UserControl
{
    public DictOptionsControl()
    {
        InitializeComponent();
    }

    private void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowColorPicker(sender, e);
    }

    public DictOptions GetDictOptions(DictType type)
    {
        NewlineBetweenDefinitionsOption? newlineOption = null;
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(type))
        {
            newlineOption = new NewlineBetweenDefinitionsOption { Value = NewlineCheckBox.IsChecked!.Value };
        }

        ExamplesOption? examplesOption = null;
        if (ExamplesOption.ValidDictTypes.Contains(type))
        {
            if (Enum.TryParse(ExamplesComboBox.SelectedValue?.ToString(), out ExamplesOptionValue eov))
                examplesOption = new ExamplesOption { Value = eov };
        }

        NoAllOption? noAllOption = null;
        if (NoAllOption.ValidDictTypes.Contains(type))
        {
            noAllOption = new NoAllOption { Value = NoAllCheckBox.IsChecked!.Value };
        }

        WordClassInfoOption? wordClassOption = null;
        if (WordClassInfoOption.ValidDictTypes.Contains(type))
        {
            wordClassOption = new WordClassInfoOption { Value = WordClassInfoCheckBox.IsChecked!.Value };
        }

        DialectInfoOption? dialectOption = null;
        if (DialectInfoOption.ValidDictTypes.Contains(type))
        {
            dialectOption = new DialectInfoOption { Value = DialectInfoCheckBox.IsChecked!.Value };
        }

        POrthographyInfoOption? pOrthographyInfoOption = null;
        if (POrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            pOrthographyInfoOption = new POrthographyInfoOption { Value = POrthographyInfoCheckBox.IsChecked!.Value };
        }

        POrthographyInfoColorOption? pOrthographyInfoColorOption = null;
        if (POrthographyInfoColorOption.ValidDictTypes.Contains(type))
        {
            pOrthographyInfoColorOption =
                new POrthographyInfoColorOption { Value = POrthographyInfoColorButton.Background.ToString() };
        }

        POrthographyInfoFontSizeOption? pOrthographyInfoFontSize = null;
        if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(type))
        {
            pOrthographyInfoFontSize =
                new POrthographyInfoFontSizeOption { Value = POrthographyInfoFontSizeNumericUpDown.Value };
        }

        AOrthographyInfoOption? aOrthographyInfoOption = null;
        if (AOrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            aOrthographyInfoOption = new AOrthographyInfoOption { Value = AOrthographyInfoCheckBox.IsChecked!.Value };
        }

        ROrthographyInfoOption? rOrthographyInfoOption = null;
        if (ROrthographyInfoOption.ValidDictTypes.Contains(type))
        {
            rOrthographyInfoOption = new ROrthographyInfoOption { Value = ROrthographyInfoCheckBox.IsChecked!.Value };
        }

        WordTypeInfoOption? wordTypeOption = null;
        if (WordTypeInfoOption.ValidDictTypes.Contains(type))
        {
            wordTypeOption = new WordTypeInfoOption { Value = WordTypeInfoCheckBox.IsChecked!.Value };
        }

        SpellingRestrictionInfoOption? spellingRestrictionInfo = null;
        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(type))
        {
            spellingRestrictionInfo =
                new SpellingRestrictionInfoOption { Value = SpellingRestrictionInfoCheckBox.IsChecked!.Value };
        }

        ExtraDefinitionInfoOption? extraDefinitionInfo = null;
        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(type))
        {
            extraDefinitionInfo =
                new ExtraDefinitionInfoOption { Value = ExtraDefinitionInfoCheckBox.IsChecked!.Value };
        }

        MiscInfoOption? miscInfoOption = null;
        if (MiscInfoOption.ValidDictTypes.Contains(type))
        {
            miscInfoOption = new MiscInfoOption { Value = MiscInfoCheckBox.IsChecked!.Value };
        }

        var options =
            new DictOptions(
                newlineOption,
                examplesOption,
                noAllOption,
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
                miscInfoOption
            );

        return options;
    }

    public void GenerateDictOptionsElements(Dict dict)
    {
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dict.Type))
        {
            bool isEpwing = Storage.YomichanDictTypes.Concat(Storage.NazekaDictTypes).All(dt => dt != dict.Type);
            NewlineCheckBox.IsChecked = dict.Options?.NewlineBetweenDefinitions?.Value ?? isEpwing;
            NewlineCheckBox.Visibility = Visibility.Visible;
        }

        if (ExamplesOption.ValidDictTypes.Contains(dict.Type))
        {
            ExamplesComboBox.ItemsSource = Enum.GetValues<ExamplesOptionValue>().ToArray();
            ExamplesComboBox.SelectedValue = dict.Options?.Examples?.Value ?? ExamplesOptionValue.All;

            ExamplesDockPanel.Visibility = Visibility.Visible;
        }

        if (NoAllOption.ValidDictTypes.Contains(dict.Type))
        {
            NoAllCheckBox.IsChecked = dict.Options?.NoAll?.Value ?? false;
            NoAllCheckBox.Visibility = Visibility.Visible;
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
            POrthographyInfoColorButton.Background = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(
                    dict.Options?.POrthographyInfoColor?.Value ?? ConfigManager.PrimarySpellingColor.ToString())!;

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

        if (NewlineCheckBox.Visibility == Visibility.Visible
            || ExamplesDockPanel.Visibility == Visibility.Visible
            || NoAllCheckBox.Visibility == Visibility.Visible
            || WordClassInfoCheckBox.Visibility == Visibility.Visible
           //|| DialectInfoCheckBox.Visibility == Visibility.Visible
           //|| POrthographyInfoCheckBox.Visibility == Visibility.Visible
           //|| POrthographyInfoColorDockPanel.Visibility == Visibility.Visible
           //|| POrthographyInfoFontSizeNumericUpDown.Visibility == Visibility.Visible
           //|| AOrthographyInfoCheckBox.Visibility == Visibility.Visible
           //|| ROrthographyInfoCheckBox.Visibility == Visibility.Visible
           //|| WordTypeInfoCheckBox.Visibility == Visibility.Visible
           //|| SpellingRestrictionInfoCheckBox.Visibility == Visibility.Visible
           //|| ExtraDefinitionInfoCheckBox.Visibility == Visibility.Visible
           //|| MiscInfoCheckBox.Visibility == Visibility.Visible
           )
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }
}
