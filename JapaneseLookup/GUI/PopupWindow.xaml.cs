using JapaneseLookup.Anki;
using JapaneseLookup.Lookup;
using JapaneseLookup.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PopupWindow.xaml
    /// </summary>
    public partial class PopupWindow : Window
    {
        private static int _playAudioIndex;

        private static readonly System.Windows.Interop.WindowInteropHelper InteropHelper =
            new(MainWindow.Instance);

        private static readonly System.Windows.Forms.Screen ActiveScreen =
            System.Windows.Forms.Screen.FromHandle(InteropHelper.Handle);

        private PopupWindow ChildPopupWindow { get; set; }

        private int CurrentCharPosition { get; set; }

        private string CurrentText { get; set; }

        public string LastText { get; set; }

        public bool MiningMode { get; private set; }

        private List<Dictionary<LookupResult, List<string>>> LastLookupResults { get; set; } = new();

        public ObservableCollection<StackPanel> ResultStackPanels { get; } = new();

        public PopupWindow()
        {
            InitializeComponent();

            MaxHeight = ConfigManager.PopupMaxHeight;
            MaxWidth = ConfigManager.PopupMaxWidth;
            Background = ConfigManager.PopupBackgroundColor;
            FontFamily = ConfigManager.PopupFont;

            if (ConfigManager.PopupDynamicWidth && ConfigManager.PopupDynamicHeight)
                SizeToContent = SizeToContent.WidthAndHeight;
            else if (ConfigManager.PopupDynamicWidth)
                SizeToContent = SizeToContent.Width;
            else if (ConfigManager.PopupDynamicHeight)
                SizeToContent = SizeToContent.Height;
            else
                SizeToContent = SizeToContent.Manual;
            // need to initialize window (position) for later
            Show();
            Hide();
        }

        public void TextBox_MouseMove(TextBox tb)
        {
            if (MiningMode || ConfigManager.InactiveLookupMode) return;

            UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

            int charPosition = tb.GetCharacterIndexFromPoint(Mouse.GetPosition(tb), false);
            if (charPosition != -1)
            {
                if (charPosition > 0 && char.IsHighSurrogate(tb.Text[charPosition - 1]))
                    --charPosition;

                CurrentText = tb.Text;
                CurrentCharPosition = charPosition;

                int endPosition = MainWindowUtilities.FindWordBoundary(tb.Text, charPosition);

                string text;
                if (endPosition - charPosition <= ConfigManager.MaxSearchLength)
                    text = tb.Text[charPosition..endPosition];
                else
                    text = tb.Text[charPosition..(charPosition + ConfigManager.MaxSearchLength)];

                if (text == LastText) return;
                LastText = text;

                var lookupResults = Lookup.Lookup.LookupText(text);

                if (lookupResults != null && lookupResults.Any())
                {
                    if (ConfigManager.HighlightLongestMatch)
                    {
                        tb.Focus();
                        tb.Select(charPosition, lookupResults[0][LookupResult.FoundForm][0].Length);
                    }

                    ResultStackPanels.Clear();

                    Visibility = Visibility.Visible;

                    Activate();
                    Focus();

                    LastLookupResults = lookupResults;
                    DisplayResults(false);
                }
                else
                {
                    Visibility = Visibility.Hidden;
                    tb.Select(0, 0);
                }
            }
            else
            {
                LastText = "";
                Visibility = Visibility.Hidden;
                tb.Select(0, 0);
            }
        }

        private void UpdatePosition(Point cursorPosition)
        {
            bool needsFlipX = ConfigManager.PopupFlipX && cursorPosition.X + Width > ActiveScreen.Bounds.Width;
            bool needsFlipY = ConfigManager.PopupFlipY && cursorPosition.Y + Height > ActiveScreen.Bounds.Height;

            double newLeft;
            double newTop;

            if (needsFlipX)
            {
                // flip Leftwards while preventing -OOB
                newLeft = cursorPosition.X - Width - ConfigManager.PopupXOffset * 2;
                if (newLeft < 0) newLeft = 0;
            }
            else
            {
                // no flip
                newLeft = cursorPosition.X + ConfigManager.PopupXOffset;
            }

            if (needsFlipY)
            {
                // flip Upwards while preventing -OOB
                newTop = cursorPosition.Y - Height - ConfigManager.PopupYOffset * 2;
                if (newTop < 0) newTop = 0;
            }
            else
            {
                // no flip
                newTop = cursorPosition.Y + ConfigManager.PopupYOffset;
            }

            // stick to edges if +OOB
            if (newLeft + Width > ActiveScreen.Bounds.Width)
            {
                newLeft = ActiveScreen.Bounds.Width - Width;
            }

            if (newTop + Height > ActiveScreen.Bounds.Height)
            {
                newTop = ActiveScreen.Bounds.Height - Height;
            }

            Left = newLeft;
            Top = newTop;
        }

        private void DisplayResults(bool generateAllResults)
        {
            int resultCount = LastLookupResults.Count;

            for (int index = 0; index < resultCount; index++)
            {
                if (!generateAllResults)
                {
                    PopupListBox.UpdateLayout();

                    if (PopupListBox.ActualHeight >= MaxHeight)
                        return;
                }

                ResultStackPanels.Add(MakeResultStackPanel(LastLookupResults[index], index, resultCount));
            }
        }

        private StackPanel MakeResultStackPanel(Dictionary<LookupResult, List<string>> result,
            int index, int resultsCount)
        {
            var innerStackPanel = new StackPanel
            {
                Margin = new Thickness(4, 2, 4, 2),
            };
            var top = new WrapPanel();
            var bottom = new StackPanel();

            innerStackPanel.Children.Add(top);
            innerStackPanel.Children.Add(bottom);

            // top
            TextBlock textBlockFoundSpelling = null;
            TextBlock textBlockPOrthographyInfo = null;
            TextBlock textBlockReadings = null;
            TextBlock textBlockAlternativeSpellings = null;
            TextBlock textBlockProcess = null;
            TextBlock textBlockFrequency = null;
            TextBlock textBlockFoundForm = null;
            TextBlock textBlockDictType = null;
            TextBlock textBlockEdictID = null;

            // bottom
            UIElement textBlockDefinitions = null;
            TextBlock textBlockNanori = null;
            TextBlock textBlockOnReadings = null;
            TextBlock textBlockKunReadings = null;
            TextBlock textBlockStrokeCount = null;
            TextBlock textBlockGrade = null;
            TextBlock textBlockComposition = null;


            foreach ((LookupResult key, var value) in result)
            {
                switch (key)
                {
                    // common
                    case LookupResult.FoundForm:
                        textBlockFoundForm = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = string.Join("", value),
                            Visibility = Visibility.Collapsed,
                        };
                        break;

                    case LookupResult.Frequency:

                        textBlockFrequency = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "#" + string.Join(", ", value),
                            Foreground = ConfigManager.FrequencyColor,
                            FontSize = ConfigManager.FrequencyFontSize,
                            Margin = new Thickness(5, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.DictType:
                        textBlockDictType = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = value[0],
                            Foreground = ConfigManager.DictTypeColor,
                            FontSize = ConfigManager.DictTypeFontSize,
                            Margin = new Thickness(5, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;


                    // EDICT
                    case LookupResult.FoundSpelling:
                        textBlockFoundSpelling = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = value[0],
                            Tag = index, // for audio
                            Foreground = ConfigManager.PrimarySpellingColor,
                            FontSize = ConfigManager.PrimarySpellingFontSize,
                            TextWrapping = TextWrapping.Wrap,
                        };
                        textBlockFoundSpelling.MouseEnter += FoundSpelling_MouseEnter; // for audio
                        textBlockFoundSpelling.MouseLeave += FoundSpelling_MouseLeave; // for audio
                        textBlockFoundSpelling.PreviewMouseUp += FoundSpelling_PreviewMouseUp; // for mining
                        break;

                    //case LookupResult.KanaSpellings:
                    //    // var textBlockKanaSpellings = new TextBlock
                    //    // {
                    //    //     Name = "kanaSpellings",
                    //    //     Text = string.Join(" ", result["kanaSpellings"]),
                    //    //     TextWrapping = TextWrapping.Wrap,
                    //    //     Foreground = Brushes.White
                    //    // };
                    //    break;

                    case LookupResult.Readings:
                        result.TryGetValue(LookupResult.ROrthographyInfoList, out var rOrthographyInfoList);
                        rOrthographyInfoList ??= new List<string>();

                        textBlockReadings =
                            PopupWindowUtilities.MakeTextBlockReadings(result[LookupResult.Readings],
                                rOrthographyInfoList);
                        break;

                    case LookupResult.Definitions:
                        if (MiningMode)
                        {
                            textBlockDefinitions = new TextBox
                            {
                                Name = key.ToString(),
                                Text = string.Join(", ", value),
                                TextWrapping = TextWrapping.Wrap,
                                Background = Brushes.Transparent,
                                Foreground = ConfigManager.DefinitionsColor,
                                FontSize = ConfigManager.DefinitionsFontSize,
                                BorderThickness = new Thickness(0, 0, 0, 0),
                                Margin = new Thickness(2, 2, 2, 2),
                                IsReadOnly = true,
                                IsUndoEnabled = false,
                                Cursor = Cursors.Arrow,
                                SelectionBrush = ConfigManager.HighlightColor,
                                IsInactiveSelectionHighlightEnabled = true,
                            };

                            textBlockDefinitions.MouseMove += (sender, _) =>
                            {
                                ChildPopupWindow ??= new PopupWindow();

                                // prevents stray PopupWindows being created when you move your mouse too fast
                                if (MiningMode)
                                    ChildPopupWindow.Definitions_MouseMove((TextBox)sender);
                            };
                        }
                        else
                        {
                            textBlockDefinitions = new TextBlock
                            {
                                Name = key.ToString(),
                                Text = string.Join(", ", value),
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = ConfigManager.DefinitionsColor,
                                FontSize = ConfigManager.DefinitionsFontSize,
                                Margin = new Thickness(2, 2, 2, 2),
                            };
                        }

                        break;

                    case LookupResult.EdictID:
                        textBlockEdictID = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = string.Join(", ", value),
                            Visibility = Visibility.Collapsed,
                        };
                        break;

                    case LookupResult.AlternativeSpellings:
                        result.TryGetValue(LookupResult.AOrthographyInfoList, out var aOrthographyInfoList);
                        aOrthographyInfoList ??= new List<string>();

                        textBlockAlternativeSpellings =
                            PopupWindowUtilities.MakeTextBlockAlternativeSpellings(
                                result[LookupResult.AlternativeSpellings],
                                aOrthographyInfoList);
                        break;

                    case LookupResult.Process:
                        textBlockProcess = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = string.Join(", ", value),
                            Foreground = ConfigManager.DeconjugationInfoColor,
                            FontSize = ConfigManager.DeconjugationInfoFontSize,
                            Margin = new Thickness(5, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.POrthographyInfoList:

                        textBlockPOrthographyInfo = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "(" + string.Join(",", value) + ")",
                            Foreground = ConfigManager.POrthographyInfoColor,
                            FontSize = ConfigManager.POrthographyInfoFontSize,
                            Margin = new Thickness(5, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.ROrthographyInfoList:
                        // processed in MakeTextBlockReadings()
                        break;

                    case LookupResult.AOrthographyInfoList:
                        // processed in MakeTextBlockAlternativeSpellings()
                        break;


                    // KANJIDIC
                    case LookupResult.OnReadings:
                        if (value?.Any() != true)
                            break;

                        textBlockOnReadings = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "On" + ": " + string.Join(", ", value),
                            Foreground = ConfigManager.ReadingsColor,
                            FontSize = ConfigManager.ReadingsFontSize,
                            Margin = new Thickness(2, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.KunReadings:
                        if (value?.Any() != true)
                            break;

                        textBlockKunReadings = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "Kun" + ": " + string.Join(", ", value),
                            Foreground = ConfigManager.ReadingsColor,
                            FontSize = ConfigManager.ReadingsFontSize,
                            Margin = new Thickness(2, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.Nanori:
                        if (value?.Any() != true)
                            break;

                        textBlockNanori = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = key + ": " + string.Join(", ", value),
                            Foreground = ConfigManager.ReadingsColor,
                            FontSize = ConfigManager.ReadingsFontSize,
                            Margin = new Thickness(2, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.StrokeCount:
                        textBlockStrokeCount = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "Strokes" + ": " + string.Join(", ", value),
                            // Foreground = ConfigManager. Color,
                            FontSize = ConfigManager.DefinitionsFontSize,
                            Margin = new Thickness(2, 2, 2, 2),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.Grade:
                        var gradeString = "";
                        var gradeInt = Convert.ToInt32(value[0]);
                        gradeString = gradeInt switch
                        {
                            0 => "Hyougai",
                            <= 6 => $"Kyouiku ({gradeInt})",
                            8 => $"Jouyou ({gradeInt})",
                            <= 10 => $"Jinmeiyou ({gradeInt})",
                            _ => gradeString
                        };

                        textBlockGrade = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = key + ": " + gradeString,
                            // Foreground = ConfigManager. Color,
                            FontSize = ConfigManager.DefinitionsFontSize,
                            Margin = new Thickness(2, 2, 2, 2),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.Composition:
                        textBlockComposition = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = key + ": " + string.Join(", ", value),
                            // Foreground = ConfigManager. Color,
                            FontSize = ConfigManager.ReadingsFontSize,
                            Margin = new Thickness(2, 2, 2, 2),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            TextBlock[] babies =
            {
                textBlockFoundSpelling, textBlockPOrthographyInfo,
                textBlockReadings,
                textBlockAlternativeSpellings,
                textBlockProcess,
                textBlockFoundForm, textBlockEdictID, // undisplayed, for mining
                textBlockFrequency, textBlockDictType
            };
            foreach (TextBlock baby in babies)
            {
                if (baby == null) continue;

                if (Enum.TryParse(baby.Name, out LookupResult enumName))
                {
                    // common emptiness check; these two have their text as inline Runs
                    if (baby.Text == "" &&
                        !(enumName == LookupResult.AlternativeSpellings || enumName == LookupResult.Readings))
                        continue;
                }

                // POrthographyInfo check
                if (baby.Text == "()")
                    continue;

                // Frequency check
                if ((baby.Text == ("#" + MainWindowUtilities.FakeFrequency)) || baby.Text == "#0")
                    continue;

                top.Children.Add(baby);
            }

            bottom.Children.Add(textBlockDefinitions);

            TextBlock[] babiesKanji =
            {
                textBlockOnReadings,
                textBlockKunReadings,
                textBlockNanori,
                textBlockGrade,
                textBlockStrokeCount,
                textBlockComposition,
            };
            foreach (TextBlock baby in babiesKanji)
            {
                if (baby == null) continue;

                // common emptiness check
                if (baby.Text == "")
                    continue;

                bottom.Children.Add(baby);
            }

            if (index != resultsCount - 1)
            {
                bottom.Children.Add(new Separator
                {
                    // TODO: Fix thickness' differing from one separator to another
                    // Width = PopupWindow.Width,
                    Background = ConfigManager.SeparatorColor
                });
            }

            return innerStackPanel;
        }

        private static void FoundSpelling_MouseEnter(object sender, MouseEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            _playAudioIndex = (int)textBlock.Tag;
        }

        private static void FoundSpelling_MouseLeave(object sender, MouseEventArgs e)
        {
            _playAudioIndex = 0;
        }

        private async void FoundSpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MiningMode = false;
            Hide();

            string foundSpelling = null;
            string readings = null;
            var definitions = "";
            string context = PopupWindowUtilities.FindSentence(CurrentText, CurrentCharPosition);
            string foundForm = null;
            string edictID = null;
            var timeLocal = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
            string alternativeSpellings = null;
            string frequency = null;
            string strokeCount = null;
            string grade = null;
            string composition = null;

            var textBlock = (TextBlock)sender;
            var top = (WrapPanel)textBlock.Parent;
            foreach (TextBlock child in top.Children)
            {
                if (Enum.TryParse(child.Name, out LookupResult result))
                {
                    switch (result)
                    {
                        case LookupResult.FoundSpelling:
                            foundSpelling = child.Text;
                            break;
                        case LookupResult.Readings:
                            readings = (string)child.Tag;
                            break;
                        case LookupResult.FoundForm:
                            foundForm = child.Text;
                            break;
                        case LookupResult.EdictID:
                            edictID = child.Text;
                            break;
                        case LookupResult.AlternativeSpellings:
                            alternativeSpellings = (string)child.Tag;
                            break;
                        case LookupResult.Frequency:
                            frequency = child.Text;
                            break;
                        case LookupResult.OnReadings:
                            readings += child.Text + " ";
                            break;
                        case LookupResult.KunReadings:
                            readings += child.Text + " ";
                            break;
                        case LookupResult.Nanori:
                            readings += child.Text + " ";
                            break;
                        case LookupResult.DictType:
                            // TODO
                            break;
                        case LookupResult.Process:
                            // TODO
                            break;
                    }
                }
            }

            var innerStackPanel = (StackPanel)top.Parent;
            var bottom = (StackPanel)innerStackPanel.Children[1];
            foreach (object child in bottom.Children)
            {
                if (child is TextBox textBox)
                {
                    definitions += textBox.Text;
                    break;
                }

                if (child is not TextBlock)
                    continue;

                textBlock = (TextBlock)child;

                if (Enum.TryParse(textBlock.Name, out LookupResult result))
                {
                    switch (result)
                    {
                        case LookupResult.StrokeCount:
                            strokeCount += textBlock.Text;
                            break;
                        case LookupResult.Grade:
                            grade += textBlock.Text;
                            break;
                        case LookupResult.Composition:
                            composition += textBlock.Text;
                            break;
                    }
                }
            }

            await Mining.Mine(
                foundSpelling,
                readings,
                definitions,
                context,
                foundForm,
                edictID,
                timeLocal,
                alternativeSpellings,
                frequency,
                strokeCount,
                grade,
                composition
            ).ConfigureAwait(false);
        }

        private void Definitions_MouseMove(TextBox tb)
        {
            if (MainWindowUtilities.JapaneseRegex.IsMatch(tb.Text))
                TextBox_MouseMove(tb);
        }

        private static void PlayAudio(string foundSpelling, string reading)
        {
            Debug.WriteLine("Attempting to play audio: " + foundSpelling + " " + reading);

            if (reading == "") reading = foundSpelling;

            Uri uri = new(
                "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji=" +
                foundSpelling +
                "&kana=" +
                reading
            );

            // var sound = AnkiConnect.GetAudio("猫", "ねこ").Result;

            // TODO: find a better solution for this that has less latency and prevents the noaudio clip from playing
            var mediaElement = new MediaElement { Source = uri, Volume = 1, Visibility = Visibility.Collapsed };
            MainWindow.Instance.MainGrid.Children.Add(mediaElement);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Utils.KeyGestureComparer(e, ConfigManager.MiningModeKeyGesture))
            {
                MiningMode = true;
                PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                // TODO: Tell the user that they are in mining mode
                Activate();
                Focus();

                ResultStackPanels.Clear();

                DisplayResults(true);
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.PlayAudioKeyGesture))
            {
                string foundSpelling = null;
                string reading = null;

                var innerStackPanel = (StackPanel)PopupListBox.Items[_playAudioIndex];
                var top = (WrapPanel)innerStackPanel.Children[0];

                foreach (TextBlock child in top.Children)
                {
                    if (Enum.TryParse(child.Name, out LookupResult result))
                    {
                        switch (result)
                        {
                            case LookupResult.FoundSpelling:
                                foundSpelling = child.Text;
                                break;
                            case LookupResult.Readings:
                                reading = ((string)child.Tag).Split(",")[0];
                                break;
                        }
                    }
                }

                PlayAudio(foundSpelling, reading);
            }

            else if (e.Key == Key.Escape)
            {
                MiningMode = false;
                PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                Hide();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.KanjiModeKeyGesture))
            {
                ConfigManager.KanjiMode = !ConfigManager.KanjiMode;
                LastText = "";
                //todo will only work for the FirstPopupWindow
                MainWindow.Instance.MainTextBox_MouseMove(null, null);
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowPreferencesWindowKeyGesture))
            {
                MainWindowUtilities.ShowPreferencesWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowAddNameWindowKeyGesture))
            {
                MainWindowUtilities.ShowAddNameWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowAddWordWindowKeyGesture))
            {
                MainWindowUtilities.ShowAddWordWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.SearchWithBrowserKeyGesture))
            {
                MainWindowUtilities.SearchWithBrowser();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.InactiveLookupModeKeyGesture))
            {
                ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (MiningMode) return;

            Hide();
            LastText = "";
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (MiningMode) return;

            Hide();
            LastText = "";
        }
    }
}