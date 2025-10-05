using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JL.Core;
using JL.Core.Dicts.Options;
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Windows.Config;
using JL.Windows.Utilities;

namespace JL.Windows.GUI.Popup;

#pragma warning disable CA1812 // Internal class that is apparently never instantiated
internal sealed class PopupContentGenerator : Decorator
#pragma warning restore CA1812 // Internal class that is apparently never instantiated
{
    static PopupContentGenerator()
    {
        DataContextProperty.OverrideMetadata(typeof(PopupContentGenerator), new FrameworkPropertyMetadata(null, OnDataContextChanged));
    }

    private static void OnDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        PopupContentGenerator generator = (PopupContentGenerator)d;
        generator.Child = e.NewValue is LookupDisplayResult displayItem
            ? PrepareResultStackPanel(displayItem)
            : null;
    }

    private static StackPanel PrepareResultStackPanel(LookupDisplayResult lookupDisplayResult)
    {
        WrapPanel top = new()
        {
            Tag = lookupDisplayResult.Index
        };

        LookupResult result = lookupDisplayResult.LookupResult;
        PopupWindow ownerWindow = lookupDisplayResult.OwnerWindow;

        CreatePrimarySpelling(lookupDisplayResult, top);
        CreatePrimarySpellingOrthographyInfo(result, top);
        CreateReadings(lookupDisplayResult, top);
        CreateAudioButton(ownerWindow, top);
        CreateAlternativeSpellings(lookupDisplayResult, top);
        CreateDeconjugationInfo(lookupDisplayResult, top);
        CreateFrequencies(result, top);
        CreateDictName(result, top);
        CreateMiningButton(lookupDisplayResult, top);

        StackPanel bottom = new();
        CreateFormattedDefinition(lookupDisplayResult, bottom);
        CreateKanjiText(ownerWindow, result.KanjiLookupResult, bottom);
        CreateImages(lookupDisplayResult, bottom);
        CreateSeparator(lookupDisplayResult.NonLastItem, bottom);

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

    private static void CreatePrimarySpelling(LookupDisplayResult lookupDisplayResult, WrapPanel top)
    {
        FrameworkElement primarySpellingFrameworkElement;

        ConfigManager configManager = ConfigManager.Instance;
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

        if (result.Readings is null && result.PitchPositions is not null)
        {
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
    }

    private static void CreatePrimarySpellingOrthographyInfo(LookupResult result, WrapPanel top)
    {
        JmdictLookupResult? jmdictLookupResult = result.JmdictLookupResult;
        if (jmdictLookupResult?.PrimarySpellingOrthographyInfoList is null)
        {
            return;
        }

        DictOptions jmdictOptions = result.Dict.Options;
        Debug.Assert(jmdictOptions.POrthographyInfo is not null);
        bool showPOrthographyInfo = jmdictOptions.POrthographyInfo.Value;

        Debug.Assert(jmdictOptions.POrthographyInfoFontSize is not null);
        double pOrthographyInfoFontSize = jmdictOptions.POrthographyInfoFontSize.Value;

        if (!showPOrthographyInfo)
        {
            return;
        }

        TextBlock textBlockPOrthographyInfo = PopupWindowUtils.CreateTextBlock(nameof(jmdictLookupResult.PrimarySpellingOrthographyInfoList),
            $"[{string.Join(", ", jmdictLookupResult.PrimarySpellingOrthographyInfoList)}]",
            DictOptionManager.POrthographyInfoColor,
            pOrthographyInfoFontSize,
            VerticalAlignment.Center,
            new Thickness(3, 0, 0, 0));

        _ = top.Children.Add(textBlockPOrthographyInfo);
    }

    private static void CreateReadings(LookupDisplayResult lookupDisplayResult, WrapPanel top)
    {
        LookupResult result = lookupDisplayResult.LookupResult;
        if (result.Readings is null)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.ReadingsFontSize is 0)
        {
            return;
        }

        bool pitchPositionsExist = result.PitchPositions is not null;
        if (!pitchPositionsExist
            && result.KanjiLookupResult is not null
            && (result.KanjiLookupResult.KunReadings is not null || result.KanjiLookupResult.OnReadings is null))
        {
            return;
        }

        bool showROrthographyInfo = false;
        JmdictLookupResult? jmdictLookupResult = result.JmdictLookupResult;
        if (jmdictLookupResult is not null)
        {
            DictOptions jmdictOptions = result.Dict.Options;
            Debug.Assert(jmdictOptions.ROrthographyInfo is not null);
            showROrthographyInfo = jmdictOptions.ROrthographyInfo.Value;
        }

        string readingsText = showROrthographyInfo && jmdictLookupResult!.ReadingsOrthographyInfoList is not null
            ? LookupResultUtils.ElementWithOrthographyInfoToText(result.Readings, jmdictLookupResult.ReadingsOrthographyInfoList)
            : string.Join('、', result.Readings);

        PopupWindow ownerWindow = lookupDisplayResult.OwnerWindow;
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

    private static void CreateAudioButton(PopupWindow ownerWindow, WrapPanel top)
    {
        if (!ownerWindow.MiningMode)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.AudioButtonFontSize is 0)
        {
            return;
        }

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

    private static void CreateAlternativeSpellings(LookupDisplayResult lookupDisplayResult, WrapPanel top)
    {
        LookupResult result = lookupDisplayResult.LookupResult;
        if (result.AlternativeSpellings is null)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.AlternativeSpellingsFontSize is 0)
        {
            return;
        }

        bool showAOrthographyInfo = false;
        JmdictLookupResult? jmdictLookupResult = result.JmdictLookupResult;
        if (jmdictLookupResult is not null)
        {
            DictOptions jmdictOptions = result.Dict.Options;
            Debug.Assert(jmdictOptions.AOrthographyInfo is not null);
            showAOrthographyInfo = jmdictOptions.AOrthographyInfo.Value;
        }

        string alternativeSpellingsText = showAOrthographyInfo && jmdictLookupResult!.AlternativeSpellingsOrthographyInfoList is not null
            ? LookupResultUtils.ElementWithOrthographyInfoToTextWithParentheses(result.AlternativeSpellings, jmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
            : $"[{string.Join('、', result.AlternativeSpellings)}]";

        PopupWindow ownerWindow = lookupDisplayResult.OwnerWindow;
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

    private static void CreateDeconjugationInfo(LookupDisplayResult lookupDisplayResult, WrapPanel top)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.DeconjugationInfoFontSize is 0)
        {
            return;
        }

        PopupWindow ownerWindow = lookupDisplayResult.OwnerWindow;
        LookupResult result = lookupDisplayResult.LookupResult;
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

    private static void CreateFrequencies(LookupResult result, WrapPanel top)
    {
        if (result.Frequencies is null)
        {
            return;
        }

        ReadOnlySpan<LookupFrequencyResult> allFrequencies = result.Frequencies.AsReadOnlySpan();
        LookupFrequencyResult[] filteredFrequencies = ArrayPool<LookupFrequencyResult>.Shared.Rent(allFrequencies.Length);

        int count = 0;
        foreach (ref readonly LookupFrequencyResult frequency in allFrequencies)
        {
            if (frequency.Freq is > 0 and < int.MaxValue)
            {
                filteredFrequencies[count] = frequency;
                ++count;
            }
        }

        if (count > 0)
        {
            ReadOnlySpan<LookupFrequencyResult> validFrequencies = filteredFrequencies.AsSpan(0, count);
            ConfigManager configManager = ConfigManager.Instance;
            TextBlock frequencyTextBlock = PopupWindowUtils.CreateTextBlock(
                nameof(result.Frequencies),
                LookupResultUtils.FrequenciesToText(validFrequencies, false, result.Frequencies.Count is 1),
                configManager.FrequencyColor,
                configManager.FrequencyFontSize,
                VerticalAlignment.Top,
                new Thickness(7, 0, 0, 0));

            _ = top.Children.Add(frequencyTextBlock);
        }

        ArrayPool<LookupFrequencyResult>.Shared.Return(filteredFrequencies);
    }

    private static void CreateDictName(LookupResult result, WrapPanel top)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.DictTypeFontSize is 0)
        {
            return;
        }

        TextBlock dictTypeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Dict.Name),
            result.Dict.Name,
            configManager.DictTypeColor,
            configManager.DictTypeFontSize,
            VerticalAlignment.Top,
            new Thickness(7, 0, 0, 0));

        _ = top.Children.Add(dictTypeTextBlock);
    }

    private static void CreateMiningButton(LookupDisplayResult lookupDisplayResult, WrapPanel top)
    {
        PopupWindow ownerWindow = lookupDisplayResult.OwnerWindow;
        if (!ownerWindow.MiningMode)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MiningButtonFontSize is 0)
        {
            return;
        }

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

    private static void CreateFormattedDefinition(LookupDisplayResult lookupDisplayResult, StackPanel bottom)
    {
        LookupResult result = lookupDisplayResult.LookupResult;
        if (result.FormattedDefinitions is null)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        PopupWindow ownerWindow = lookupDisplayResult.OwnerWindow;
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

    private static void CreateKanjiText(PopupWindow ownerWindow, KanjiLookupResult? kanjiLookupResult, StackPanel bottom)
    {
        if (kanjiLookupResult is null)
        {
            return;
        }

        string? kanjiText = GetKanjiText(kanjiLookupResult);
        if (kanjiText is null)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (ownerWindow.MiningMode)
        {
            TextBox kanjiTextTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiText),
                kanjiText,
                configManager.DefinitionsColor,
                configManager.DefinitionsFontSize,
                VerticalAlignment.Center,
                new Thickness(0, 2, 2, 2),
                ownerWindow.PopupContextMenu);

            ownerWindow.AddEventHandlersToTextBox(kanjiTextTextBox);

            _ = bottom.Children.Add(kanjiTextTextBox);
        }
        else
        {
            TextBlock kanjiTextTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiText),
                kanjiText,
                configManager.DefinitionsColor,
                configManager.DefinitionsFontSize,
                VerticalAlignment.Center,
                new Thickness(2));

            _ = bottom.Children.Add(kanjiTextTextBlock);
        }
    }

    private static string? GetKanjiText(KanjiLookupResult kanjiLookupResult)
    {
        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
        if (kanjiLookupResult.OnReadings is not null)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"On: {string.Join('、', kanjiLookupResult.OnReadings)}");
        }

        if (kanjiLookupResult.KunReadings is not null)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{(sb.Length > 0 ? "\n" : "")}Kun: {string.Join('、', kanjiLookupResult.KunReadings)}");
        }

        if (kanjiLookupResult.NanoriReadings is not null)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{(sb.Length > 0 ? "\n" : "")}Nanori: {string.Join('、', kanjiLookupResult.NanoriReadings)}");
        }

        if (kanjiLookupResult.RadicalNames is not null)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{(sb.Length > 0 ? "\n" : "")}Radical names: {string.Join('、', kanjiLookupResult.RadicalNames)}");
        }

        if (kanjiLookupResult.KanjiGrade is not byte.MaxValue)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{(sb.Length > 0 ? "\n" : "")}Grade: {LookupResultUtils.GradeToText(kanjiLookupResult.KanjiGrade)}");
        }

        if (kanjiLookupResult.KanjiGrade is not 0)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{(sb.Length > 0 ? "\n" : "")}Stroke count: {kanjiLookupResult.StrokeCount}");
        }

        if (kanjiLookupResult.KanjiComposition is not null)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{(sb.Length > 0 ? "\n" : "")}Composition: {string.Join('、', kanjiLookupResult.KanjiComposition)}");
        }

        if (kanjiLookupResult.KanjiStats is not null)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{(sb.Length > 0 ? "\n" : "")}Statistics:\n{kanjiLookupResult.KanjiStats}");
        }

        string? result = sb.Length > 0 ? sb.ToString() : null;
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return result;
    }

    private static void CreateImages(LookupDisplayResult lookupDisplayResult, StackPanel bottom)
    {
        LookupResult result = lookupDisplayResult.LookupResult;
        if (result.ImagePaths is null)
        {
            return;
        }

        ShowImagesOption? showImagesOption = result.Dict.Options.ShowImagesOption;
        Debug.Assert(showImagesOption is not null);
        bool showImages = showImagesOption.Value;
        if (!showImages)
        {
            return;
        }

        for (int i = 0; i < result.ImagePaths.Length; i++)
        {
            string imagePath = Path.GetFullPath(result.ImagePaths[i], AppInfo.ApplicationPath);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
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
                LoggerManager.Logger.Error(ex, "Image path not found {ImagePath}", imagePath);
                showImagesOption.Value = false;
            }
        }
    }

    private static void CreateSeparator(bool nonLastItem, StackPanel bottom)
    {
        if (!nonLastItem)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        _ = bottom.Children.Add(new Separator
        {
            Height = 2,
            Background = configManager.SeparatorColor,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        });
    }
}
