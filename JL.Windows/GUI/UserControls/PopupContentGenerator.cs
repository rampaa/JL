using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.UserControls;

#pragma warning disable CA1812 // Internal class that is apparently never instantiated
internal sealed class PopupContentGenerator : Decorator
#pragma warning restore CA1812 // Internal class that is apparently never instantiated
{
    static PopupContentGenerator()
    {
        DataContextProperty.OverrideMetadata(typeof(PopupContentGenerator), new FrameworkPropertyMetadata(null, OnDataContextChangedCallback));
    }

    private static void OnDataContextChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        PopupContentGenerator generator = (PopupContentGenerator)d;
        generator.Child = e.NewValue is LookupDisplayResult displayItem
            ? PrepareResultStackPanel(displayItem)
            : null;
    }

    private static StackPanel PrepareResultStackPanel(LookupDisplayResult lookupDisplayResult)
    {
        // top
        WrapPanel top = new()
        {
            Tag = lookupDisplayResult.Index
        };

        ConfigManager configManager = ConfigManager.Instance;

        FrameworkElement primarySpellingFrameworkElement;
        PopupWindow ownerWindow = lookupDisplayResult.OwnerWindow;
        LookupResult result = lookupDisplayResult.LookupResult;
        if (ownerWindow.MiningMode)
        {
            primarySpellingFrameworkElement = PopupWindowUtils.CreateTextBox(nameof(result.PrimarySpelling),
            result.PrimarySpelling,
            configManager.PrimarySpellingColor,
            configManager.PrimarySpellingFontSize,
            VerticalAlignment.Center,
            new Thickness(),
            ownerWindow.PopupContextMenu);

            ownerWindow.AddEventHandlersToPrimarySpellingTextBox(primarySpellingFrameworkElement);
        }
        else
        {
            primarySpellingFrameworkElement = PopupWindowUtils.CreateTextBlock(nameof(result.PrimarySpelling),
            result.PrimarySpelling,
            configManager.PrimarySpellingColor,
            configManager.PrimarySpellingFontSize,
            VerticalAlignment.Center,
            new Thickness(2, 0, 0, 0));
        }

        bool pitchPositionsExist = result.PitchPositions is not null;

        if (result.Readings is null && pitchPositionsExist)
        {
            Debug.Assert(result.PitchPositions is not null);

            PitchAccentDecorator pitchAccentDecorator = new(primarySpellingFrameworkElement, [result.PrimarySpelling],
                        [result.PrimarySpelling],
                        result.PitchPositions,
                        PopupWindowUtils.PitchAccentMarkerPen);

            _ = top.Children.Add(pitchAccentDecorator);
        }
        else
        {
            _ = top.Children.Add(primarySpellingFrameworkElement);
        }

        JmdictLookupResult? jmdictLookupResult = result.JmdictLookupResult;
        bool jmdictLookupResultExist = jmdictLookupResult is not null;


        bool showPOrthographyInfo = false;
        bool showROrthographyInfo = false;
        bool showAOrthographyInfo = false;
        double pOrthographyInfoFontSize = 0;

        if (jmdictLookupResultExist)
        {
            Dict jmdict = result.Dict;
            Debug.Assert(jmdict.Options.POrthographyInfo is not null);
            showPOrthographyInfo = jmdict.Options.POrthographyInfo.Value;

            Debug.Assert(jmdict.Options.ROrthographyInfo is not null);
            showROrthographyInfo = jmdict.Options.ROrthographyInfo.Value;

            Debug.Assert(jmdict.Options.AOrthographyInfo is not null);
            showAOrthographyInfo = jmdict.Options.AOrthographyInfo.Value;

            Debug.Assert(jmdict.Options.POrthographyInfoFontSize is not null);
            pOrthographyInfoFontSize = jmdict.Options.POrthographyInfoFontSize.Value;
        }

        if (showPOrthographyInfo && jmdictLookupResultExist)
        {
            Debug.Assert(jmdictLookupResult is not null);
            if (jmdictLookupResult.PrimarySpellingOrthographyInfoList is not null)
            {
                TextBlock textBlockPOrthographyInfo = PopupWindowUtils.CreateTextBlock(nameof(jmdictLookupResult.PrimarySpellingOrthographyInfoList),
                    $"[{string.Join(", ", jmdictLookupResult.PrimarySpellingOrthographyInfoList)}]",
                    DictOptionManager.POrthographyInfoColor,
                    pOrthographyInfoFontSize,
                    VerticalAlignment.Center,
                    new Thickness(3, 0, 0, 0));

                _ = top.Children.Add(textBlockPOrthographyInfo);
            }
        }

        if (result.Readings is not null && configManager.ReadingsFontSize > 0
                                        && (pitchPositionsExist || result.KanjiLookupResult is null || (result.KanjiLookupResult.KunReadings is null && result.KanjiLookupResult.OnReadings is null)))
        {
            string readingsText = showROrthographyInfo && jmdictLookupResultExist && jmdictLookupResult!.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(result.Readings, jmdictLookupResult.ReadingsOrthographyInfoList)
                : string.Join('、', result.Readings);

            if (ownerWindow.MiningMode)
            {
                TextBox readingTextBox = PopupWindowUtils.CreateTextBox(nameof(result.Readings),
                    readingsText, configManager.ReadingsColor,
                    configManager.ReadingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(5, 0, 0, 0),
                    ownerWindow.PopupContextMenu);

                if (pitchPositionsExist)
                {
                    Debug.Assert(result.PitchPositions is not null);

                    PitchAccentDecorator pitchAccentDecorator = new(readingTextBox, result.Readings,
                        readingTextBox.Text.Split('、'),
                        result.PitchPositions,
                        PopupWindowUtils.PitchAccentMarkerPen);

                    _ = top.Children.Add(pitchAccentDecorator);
                }

                else
                {
                    _ = top.Children.Add(readingTextBox);
                }

                ownerWindow.AddEventHandlersToTextBox(readingTextBox);
            }

            else
            {
                TextBlock readingTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Readings),
                    readingsText,
                    configManager.ReadingsColor,
                    configManager.ReadingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(7, 0, 0, 0));

                if (pitchPositionsExist)
                {
                    Debug.Assert(result.PitchPositions is not null);

                    PitchAccentDecorator pitchAccentDecorator = new(readingTextBlock, result.Readings,
                        readingTextBlock.Text.Split('、'),
                        result.PitchPositions,
                        PopupWindowUtils.PitchAccentMarkerPen);

                    _ = top.Children.Add(pitchAccentDecorator);
                }

                else
                {
                    _ = top.Children.Add(readingTextBlock);
                }
            }
        }

        if (ownerWindow.MiningMode && configManager.AudioButtonFontSize > 0)
        {
            Button audioButton = new()
            {
                Name = "AudioButton",
                Content = "🔊",
                Foreground = configManager.AudioButtonColor,
                VerticalAlignment = VerticalAlignment.Top,
                VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Cursor = Cursors.Arrow,
                BorderThickness = new Thickness(),
                Padding = new Thickness(),
                FontSize = configManager.AudioButtonFontSize,
                Focusable = false,
                Height = double.NaN,
                Width = double.NaN
            };

            audioButton.PreviewMouseUp += ownerWindow.AudioButton_Click;

            _ = top.Children.Add(audioButton);
        }

        if (result.AlternativeSpellings is not null && configManager.AlternativeSpellingsFontSize > 0)
        {
            string alternativeSpellingsText = showAOrthographyInfo && jmdictLookupResultExist && jmdictLookupResult!.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToTextWithParentheses(result.AlternativeSpellings, jmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
                : $"[{string.Join('、', result.AlternativeSpellings)}]";

            if (ownerWindow.MiningMode)
            {
                TextBox alternativeSpellingsTexBox = PopupWindowUtils.CreateTextBox(nameof(result.AlternativeSpellings),
                    alternativeSpellingsText,
                    configManager.AlternativeSpellingsColor,
                    configManager.AlternativeSpellingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(5, 0, 0, 0),
                    ownerWindow.PopupContextMenu);

                ownerWindow.AddEventHandlersToTextBox(alternativeSpellingsTexBox);

                _ = top.Children.Add(alternativeSpellingsTexBox);
            }
            else
            {
                TextBlock alternativeSpellingsTexBlock = PopupWindowUtils.CreateTextBlock(nameof(result.AlternativeSpellings),
                    alternativeSpellingsText,
                    configManager.AlternativeSpellingsColor,
                    configManager.AlternativeSpellingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(7, 0, 0, 0));

                _ = top.Children.Add(alternativeSpellingsTexBlock);
            }
        }

        if (configManager.DeconjugationInfoFontSize > 0)
        {
            if (result.DeconjugationProcess is not null)
            {
                if (ownerWindow.MiningMode)
                {
                    TextBox deconjugationProcessTextBox = PopupWindowUtils.CreateTextBox(nameof(result.DeconjugationProcess),
                        $"{result.MatchedText} {result.DeconjugationProcess}",
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(5, 0, 0, 0),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(deconjugationProcessTextBox);

                    _ = top.Children.Add(deconjugationProcessTextBox);
                }
                else
                {
                    TextBlock deconjugationProcessTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.DeconjugationProcess),
                        $"{result.MatchedText} {result.DeconjugationProcess}",
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(7, 0, 0, 0));

                    _ = top.Children.Add(deconjugationProcessTextBlock);
                }
            }
            else if (result.PrimarySpelling != result.MatchedText && (result.Readings is null || !result.Readings.AsReadOnlySpan().Contains(result.MatchedText)))
            {
                if (ownerWindow.MiningMode)
                {
                    TextBox matchedTextTextBox = PopupWindowUtils.CreateTextBox(nameof(result.MatchedText),
                        result.MatchedText,
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(5, 0, 0, 0),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(matchedTextTextBox);

                    _ = top.Children.Add(matchedTextTextBox);
                }
                else
                {
                    TextBlock matchedTextTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.MatchedText),
                        result.MatchedText,
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(7, 0, 0, 0));

                    _ = top.Children.Add(matchedTextTextBlock);
                }
            }
        }

        if (result.Frequencies is not null)
        {
            ReadOnlySpan<LookupFrequencyResult> allFrequencies = result.Frequencies.AsReadOnlySpan();
            List<LookupFrequencyResult> filteredFrequencies = new(allFrequencies.Length);
            foreach (ref readonly LookupFrequencyResult frequency in allFrequencies)
            {
                if (frequency.Freq is > 0 and < int.MaxValue)
                {
                    filteredFrequencies.Add(frequency);
                }
            }

            ReadOnlySpan<LookupFrequencyResult> validFrequencies = filteredFrequencies.AsReadOnlySpan();

            if (validFrequencies.Length > 0)
            {
                TextBlock frequencyTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Frequencies),
                    LookupResultUtils.FrequenciesToText(validFrequencies, false, result.Frequencies.Count is 1),
                    configManager.FrequencyColor,
                    configManager.FrequencyFontSize,
                    VerticalAlignment.Top,
                    new Thickness(7, 0, 0, 0));

                _ = top.Children.Add(frequencyTextBlock);
            }
        }

        if (configManager.DictTypeFontSize > 0)
        {
            TextBlock dictTypeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Dict.Name),
                result.Dict.Name,
                configManager.DictTypeColor,
                configManager.DictTypeFontSize,
                VerticalAlignment.Top,
                new Thickness(7, 0, 0, 0));

            _ = top.Children.Add(dictTypeTextBlock);
        }

        if (ownerWindow.MiningMode && configManager.MiningButtonFontSize > 0)
        {
            Button miningButton = new()
            {
                Name = "MiningButton",
                Content = '➕',
                ToolTip = lookupDisplayResult.IsDuplicate ? "Duplicate note" : "Mine",
                Foreground = lookupDisplayResult.IsDuplicate ? Brushes.OrangeRed : configManager.MiningButtonColor,
                VerticalAlignment = VerticalAlignment.Top,
                VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Cursor = Cursors.Arrow,
                BorderThickness = new Thickness(),
                Padding = new Thickness(),
                FontSize = configManager.MiningButtonFontSize,
                Focusable = false,
                Height = double.NaN,
                Width = double.NaN
            };

            miningButton.PreviewMouseUp += ownerWindow.MiningButton_PreviewMouseUp;

            _ = top.Children.Add(miningButton);
        }

        // bottom
        StackPanel bottom = new();

        if (result.FormattedDefinitions is not null)
        {
            if (ownerWindow.MiningMode)
            {
                TextBox definitionsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.FormattedDefinitions),
                    result.FormattedDefinitions,
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2),
                    ownerWindow.PopupContextMenu);

                ownerWindow.AddEventHandlersToDefinitionsTextBox(definitionsTextBox);
                _ = bottom.Children.Add(definitionsTextBox);
            }

            else
            {
                TextBlock definitionsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.FormattedDefinitions),
                    result.FormattedDefinitions,
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(definitionsTextBlock);
            }
        }

        KanjiLookupResult? kanjiLookupResult = result.KanjiLookupResult;
        if (kanjiLookupResult is not null)
        {
            if (kanjiLookupResult.OnReadings is not null)
            {
                if (ownerWindow.MiningMode)
                {
                    TextBox onReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.OnReadings),
                        $"On: {string.Join('、', kanjiLookupResult.OnReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(onReadingsTextBox);

                    _ = bottom.Children.Add(onReadingsTextBox);
                }

                else
                {
                    TextBlock onReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.OnReadings),
                        $"On: {string.Join('、', kanjiLookupResult.OnReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(onReadingsTextBlock);
                }
            }

            if (kanjiLookupResult.KunReadings is not null)
            {
                if (ownerWindow.MiningMode)
                {
                    TextBox kunReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.KunReadings),
                        $"Kun: {string.Join('、', kanjiLookupResult.KunReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(kunReadingsTextBox);

                    _ = bottom.Children.Add(kunReadingsTextBox);
                }

                else
                {
                    TextBlock kunReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KunReadings),
                        $"Kun: {string.Join('、', kanjiLookupResult.KunReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(kunReadingsTextBlock);
                }
            }

            if (kanjiLookupResult.NanoriReadings is not null)
            {
                if (ownerWindow.MiningMode)
                {
                    TextBox nanoriReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.NanoriReadings),
                        $"Nanori: {string.Join('、', kanjiLookupResult.NanoriReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(nanoriReadingsTextBox);

                    _ = bottom.Children.Add(nanoriReadingsTextBox);
                }

                else
                {
                    TextBlock nanoriReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.NanoriReadings),
                        $"Nanori: {string.Join('、', kanjiLookupResult.NanoriReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(nanoriReadingsTextBlock);
                }
            }

            if (kanjiLookupResult.RadicalNames is not null)
            {
                if (ownerWindow.MiningMode)
                {
                    TextBox radicalNameTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.RadicalNames),
                        $"Radical names: {string.Join('、', kanjiLookupResult.RadicalNames)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(radicalNameTextBox);

                    _ = bottom.Children.Add(radicalNameTextBox);
                }

                else
                {
                    TextBlock radicalNameTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.RadicalNames),
                        $"Radical names: {string.Join('、', kanjiLookupResult.RadicalNames)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(radicalNameTextBlock);
                }
            }

            if (kanjiLookupResult.KanjiGrade is not byte.MaxValue)
            {
                TextBlock gradeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KanjiGrade),
                    $"Grade: {LookupResultUtils.GradeToText(kanjiLookupResult.KanjiGrade)}",
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(gradeTextBlock);
            }

            if (kanjiLookupResult.StrokeCount > 0)
            {
                TextBlock strokeCountTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.StrokeCount),
                    string.Create(CultureInfo.InvariantCulture, $"Stroke count: {kanjiLookupResult.StrokeCount}"),
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(strokeCountTextBlock);
            }

            if (kanjiLookupResult.KanjiComposition is not null)
            {
                string composition = $"Composition: {string.Join('、', kanjiLookupResult.KanjiComposition)}";
                if (ownerWindow.MiningMode)
                {
                    TextBox compositionTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.KanjiComposition),
                        composition,
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(compositionTextBox);

                    _ = bottom.Children.Add(compositionTextBox);
                }

                else
                {
                    TextBlock compositionTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KanjiComposition),
                        composition,
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(compositionTextBlock);
                }
            }

            if (kanjiLookupResult.KanjiStats is not null)
            {
                if (ownerWindow.MiningMode)
                {
                    TextBox kanjiStatsTextBlock = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.KanjiStats),
                        $"Statistics:\n{kanjiLookupResult.KanjiStats}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        ownerWindow.PopupContextMenu);

                    ownerWindow.AddEventHandlersToTextBox(kanjiStatsTextBlock);

                    _ = bottom.Children.Add(kanjiStatsTextBlock);
                }

                else
                {
                    TextBlock kanjiStatsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KanjiStats),
                        $"Statistics:\n{kanjiLookupResult.KanjiStats}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(kanjiStatsTextBlock);
                }
            }
        }

        if (result.ImagePaths is not null)
        {
            ShowImagesOption? showImagesOption = result.Dict.Options.ShowImagesOption;
            Debug.Assert(showImagesOption is not null);
            bool showImages = showImagesOption.Value;

            if (showImages)
            {
                for (int i = 0; i < result.ImagePaths.Length; i++)
                {
                    string imagePath = result.ImagePaths[i];

                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

                    try
                    {
                        BitmapFrame frame = BitmapFrame.Create(bitmap.UriSource, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                        if (frame.PixelWidth > frame.PixelHeight)
                        {
                            bitmap.DecodePixelWidth = double.ConvertToIntegerNative<int>(lookupDisplayResult.OwnerWindow.MaxWidth);
                        }
                        else
                        {
                            bitmap.DecodePixelHeight = double.ConvertToIntegerNative<int>(lookupDisplayResult.OwnerWindow.MaxHeight);
                        }

                        bitmap.EndInit();
                        bitmap.Freeze();

                        Image image = new()
                        {
                            Name = $"Image{i}",
                            Source = bitmap,
                            Stretch = Stretch.Uniform,
                            StretchDirection = StretchDirection.DownOnly,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(2, 2, 2, 4)
                        };

                        _ = bottom.Children.Add(image);
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        Utils.Logger.Error(ex, "Image path not found {ImagePath}", imagePath);
                        showImagesOption.Value = false;
                    }
                }
            }
        }

        if (lookupDisplayResult.NonLastItem)
        {
            _ = bottom.Children.Add(new Separator
            {
                Height = 2,
                Background = configManager.SeparatorColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        StackPanel stackPanel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(2),
            Background = Brushes.Transparent,
            Tag = result.Dict,
            Children =
            {
                top, bottom
            }
        };

        stackPanel.MouseEnter += ownerWindow.ListViewItem_MouseEnter;

        return stackPanel;
    }
}
