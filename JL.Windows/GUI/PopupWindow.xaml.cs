using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JL.Core;
using JL.Core.Anki;
using JL.Core.Dicts;
using JL.Core.Dicts.Kanjium;
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI
{
    /// <summary>
    /// Interaction logic for PopupWindow.xaml
    /// </summary>
    public partial class PopupWindow : Window
    {
        private readonly PopupWindow _parentPopupWindow;

        private PopupWindow _childPopupWindow;

        private TextBox _lastTextBox;

        private int _playAudioIndex;

        private int _currentCharPosition;

        private string _currentText;

        private string _lastSelectedText;

        private List<Dictionary<LookupResult, List<string>>> _lastLookupResults = new();

        public string LastText { get; set; }

        public bool MiningMode { get; set; }

        public ObservableCollection<StackPanel> ResultStackPanels { get; } = new();

        public PopupWindow()
        {
            InitializeComponent();
            Init();

            // need to initialize window (position) for later
            Show();
            Hide();
        }

        public PopupWindow(PopupWindow parentPopUp) : this()
        {
            _parentPopupWindow = parentPopUp;
        }

        public void Init()
        {
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

            TextBlockMiningModeReminder.Text =
                $"Click on an entry's main spelling to mine it," + Environment.NewLine +
                $"or press {ConfigManager.ClosePopupKeyGesture.Key} or click on the main window to exit.";
        }

        private void AddName(object sender, RoutedEventArgs e)
        {
            WindowsUtils.ShowAddNameWindow(_lastSelectedText);
        }

        private void AddWord(object sender, RoutedEventArgs e)
        {
            WindowsUtils.ShowAddWordWindow(_lastSelectedText);
        }

        private void ShowPreferences(object sender, RoutedEventArgs e)
        {
            WindowsUtils.ShowPreferencesWindow();
        }

        private void SearchWithBrowser(object sender, RoutedEventArgs e)
        {
            WindowsUtils.SearchWithBrowser(_lastSelectedText);
        }

        private void ShowManageDictionariesWindow(object sender, RoutedEventArgs e)
        {
            WindowsUtils.ShowManageDictionariesWindow();
        }

        public void TextBox_MouseMove(TextBox tb)
        {
            if (MiningMode || ConfigManager.InactiveLookupMode
                           || (ConfigManager.RequireLookupKeyPress
                               && !Keyboard.Modifiers.HasFlag(ConfigManager.LookupKey))
               )
                return;

            _lastTextBox = tb;

            if (ConfigManager.FixedPopupPositioning)
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            else
                UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

            int charPosition = tb.GetCharacterIndexFromPoint(Mouse.GetPosition(tb), false);
            if (charPosition != -1)
            {
                if (charPosition > 0 && char.IsHighSurrogate(tb.Text[charPosition - 1]))
                    --charPosition;

                _currentText = tb.Text;
                _currentCharPosition = charPosition;

                int endPosition = Utils.FindWordBoundary(tb.Text, charPosition);

                string text;
                if (endPosition - charPosition <= ConfigManager.MaxSearchLength)
                    text = tb.Text[charPosition..endPosition];
                else
                    text = tb.Text[charPosition..(charPosition + ConfigManager.MaxSearchLength)];

                if (text == LastText) return;
                LastText = text;

                ResultStackPanels.Clear();
                List<Dictionary<LookupResult, List<string>>> lookupResults = Lookup.LookupText(text);

                if (lookupResults != null && lookupResults.Any())
                {
                    _lastSelectedText = lookupResults[0][LookupResult.FoundForm][0];
                    if (ConfigManager.HighlightLongestMatch)
                    {
                        double verticalOffset = tb.VerticalOffset;

                        if (ConfigManager.PopupFocusOnLookup)
                        {
                            tb.Focus();
                        }

                        tb.Select(charPosition, lookupResults[0][LookupResult.FoundForm][0].Length);
                        tb.ScrollToVerticalOffset(verticalOffset);
                    }

                    Init();
                    Visibility = Visibility.Visible;

                    if (ConfigManager.PopupFocusOnLookup)
                    {
                        Activate();
                        Focus();
                    }

                    _lastLookupResults = lookupResults;
                    DisplayResults(false);
                }
                else
                {
                    Visibility = Visibility.Hidden;

                    if (ConfigManager.HighlightLongestMatch)
                    {
                        //Unselect(tb);
                    }
                }
            }
            else
            {
                LastText = "";
                Visibility = Visibility.Hidden;

                if (ConfigManager.HighlightLongestMatch)
                {
                    Unselect(tb);
                }
            }
        }

        public void LookupOnSelect(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.SelectedText))
                return;

            _lastTextBox = tb;

            PopUpScrollViewer.ScrollToTop();

            if (ConfigManager.FixedPopupPositioning)
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            else
                UpdatePosition(tb.PointToScreen(tb.GetRectFromCharacterIndex(tb.SelectionStart).BottomLeft));

            List<Dictionary<LookupResult, List<string>>> lookupResults = Lookup.LookupText(tb.SelectedText);

            if (lookupResults?.Any() ?? false)
            {
                ResultStackPanels.Clear();

                Init();

                PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                Visibility = Visibility.Visible;

                if (ConfigManager.PopupFocusOnLookup)
                {
                    Activate();
                    Focus();
                }

                _lastLookupResults = lookupResults;
                DisplayResults(true);
            }
            else
            {
                PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                Visibility = Visibility.Hidden;
            }
        }

        private void UpdatePosition(Point cursorPosition)
        {
            double mouseX = cursorPosition.X / WindowsUtils.Dpi.DpiScaleX;
            double mouseY = cursorPosition.Y / WindowsUtils.Dpi.DpiScaleY;

            bool needsFlipX = ConfigManager.PopupFlipX && mouseX + Width > WindowsUtils.DpiAwareWorkAreaWidth;
            bool needsFlipY = ConfigManager.PopupFlipY && mouseY + Height > WindowsUtils.DpiAwareWorkAreaHeight;

            double newLeft;
            double newTop;

            if (needsFlipX)
            {
                // flip Leftwards while preventing -OOB
                newLeft = mouseX - Width - WindowsUtils.DpiAwareXOffset * 2;
                if (newLeft < 0) newLeft = 0;
            }
            else
            {
                // no flip
                newLeft = mouseX + WindowsUtils.DpiAwareXOffset;
            }

            if (needsFlipY)
            {
                // flip Upwards while preventing -OOB
                newTop = mouseY - Height - WindowsUtils.DpiAwareYOffset * 2;
                if (newTop < 0) newTop = 0;
            }
            else
            {
                // no flip
                newTop = mouseY + WindowsUtils.DpiAwareYOffset;
            }

            // stick to edges if +OOB
            if (newLeft + Width > WindowsUtils.DpiAwareWorkAreaWidth)
            {
                newLeft = WindowsUtils.DpiAwareWorkAreaWidth - Width;
            }

            if (newTop + Height > WindowsUtils.DpiAwareWorkAreaHeight)
            {
                newTop = WindowsUtils.DpiAwareWorkAreaHeight - Height;
            }

            Left = newLeft;
            Top = newTop;
        }

        private void UpdatePosition(double x, double y)
        {
            Left = x;
            Top = y;
        }

        private void DisplayResults(bool generateAllResults)
        {
            int resultCount = _lastLookupResults.Count;

            for (int index = 0; index < resultCount; index++)
            {
                // if (!generateAllResults && index > 0)
                // {
                //     PopupListBox.UpdateLayout();
                //
                //     if (PopupListBox.ActualHeight >= MaxHeight - 30)
                //         return;
                // }

                ResultStackPanels.Add(MakeResultStackPanel(_lastLookupResults[index], index, resultCount));
            }
        }

        private StackPanel MakeResultStackPanel(Dictionary<LookupResult, List<string>> result,
            int index, int resultsCount)
        {
            var innerStackPanel = new StackPanel { Margin = new Thickness(4, 2, 4, 2), };
            WrapPanel top = new();
            StackPanel bottom = new();

            innerStackPanel.Children.Add(top);
            innerStackPanel.Children.Add(bottom);

            // top
            TextBlock textBlockFoundSpelling = null;
            TextBlock textBlockPOrthographyInfo = null;
            UIElement uiElementReadings = null;
            UIElement uiElementAlternativeSpellings = null;
            TextBlock textBlockProcess = null;
            TextBlock textBlockFrequency = null;
            TextBlock textBlockFoundForm = null;
            TextBlock textBlockDictType = null;
            TextBlock textBlockEdictID = null;

            // bottom
            UIElement uiElementDefinitions = null;
            TextBlock textBlockNanori = null;
            TextBlock textBlockOnReadings = null;
            TextBlock textBlockKunReadings = null;
            TextBlock textBlockStrokeCount = null;
            TextBlock textBlockGrade = null;
            TextBlock textBlockComposition = null;


            foreach ((LookupResult key, List<string> value) in result)
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
                        IEnumerable<DictType> dictTypeNames = Enum.GetValues(typeof(DictType)).Cast<DictType>();
                        DictType dictType = dictTypeNames.First(dictTypeName => dictTypeName.ToString() == value[0]);

                        textBlockDictType = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = dictType.GetDescription() ?? value[0],
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
                        result.TryGetValue(LookupResult.ROrthographyInfoList, out List<string> rOrthographyInfoList);
                        rOrthographyInfoList ??= new List<string>();

                        List<string> readings = result[LookupResult.Readings];

                        string readingsText =
                            PopupWindowUtilities.MakeUiElementReadingsText(readings, rOrthographyInfoList);

                        if (readingsText == "")
                            continue;

                        if (MiningMode || ConfigManager.LookupOnSelectOnly)
                        {
                            uiElementReadings = new TextBox()
                            {
                                Name = LookupResult.Readings.ToString(),
                                Text = readingsText,
                                TextWrapping = TextWrapping.Wrap,
                                Background = Brushes.Transparent,
                                Foreground = ConfigManager.ReadingsColor,
                                FontSize = ConfigManager.ReadingsFontSize,
                                BorderThickness = new Thickness(0, 0, 0, 0),
                                Margin = new Thickness(5, 0, 0, 0),
                                Padding = new Thickness(0),
                                IsReadOnly = true,
                                IsUndoEnabled = false,
                                Cursor = Cursors.Arrow,
                                SelectionBrush = ConfigManager.HighlightColor,
                                IsInactiveSelectionHighlightEnabled = true,
                                ContextMenu = PopupContextMenu,
                            };

                            uiElementReadings.PreviewMouseLeftButtonUp += UiElement_PreviewMouseLeftButtonUp;
                            uiElementReadings.MouseMove += PopupMouseMove;
                            uiElementReadings.LostFocus += Unselect;
                            uiElementReadings.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                        }
                        else
                        {
                            uiElementReadings = new TextBlock
                            {
                                Name = LookupResult.Readings.ToString(),
                                Text = readingsText,
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = ConfigManager.ReadingsColor,
                                FontSize = ConfigManager.ReadingsFontSize,
                                Margin = new Thickness(5, 0, 0, 0),
                            };
                        }

                        break;

                    case LookupResult.Definitions:
                        if (value?.Any() != true)
                            break;

                        if (MiningMode || ConfigManager.LookupOnSelectOnly)
                        {
                            uiElementDefinitions = new TextBox
                            {
                                Name = key.ToString(),
                                Text = string.Join(", ", value),
                                TextWrapping = TextWrapping.Wrap,
                                Background = Brushes.Transparent,
                                Foreground = ConfigManager.DefinitionsColor,
                                FontSize = ConfigManager.DefinitionsFontSize,
                                BorderThickness = new Thickness(0, 0, 0, 0),
                                Margin = new Thickness(2, 2, 2, 2),
                                Padding = new Thickness(0),
                                IsReadOnly = true,
                                IsUndoEnabled = false,
                                Cursor = Cursors.Arrow,
                                SelectionBrush = ConfigManager.HighlightColor,
                                IsInactiveSelectionHighlightEnabled = true,
                                ContextMenu = PopupContextMenu,
                            };

                            uiElementDefinitions.PreviewMouseLeftButtonUp += UiElement_PreviewMouseLeftButtonUp;

                            uiElementDefinitions.MouseMove += PopupMouseMove;
                            uiElementDefinitions.LostFocus += Unselect;
                            uiElementDefinitions.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                        }
                        else
                        {
                            uiElementDefinitions = new TextBlock
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
                        result.TryGetValue(LookupResult.AOrthographyInfoList, out List<string> aOrthographyInfoList);
                        aOrthographyInfoList ??= new List<string>();

                        List<string> alternativeSpellings = result[LookupResult.AlternativeSpellings];

                        string alternativeSpellingsText =
                            PopupWindowUtilities.MakeUiElementAlternativeSpellingsText(alternativeSpellings,
                                aOrthographyInfoList);

                        if (alternativeSpellingsText == "")
                            continue;

                        if (MiningMode || ConfigManager.LookupOnSelectOnly)
                        {
                            uiElementAlternativeSpellings = new TextBox()
                            {
                                Name = LookupResult.AlternativeSpellings.ToString(),
                                Text = alternativeSpellingsText,
                                TextWrapping = TextWrapping.Wrap,
                                Background = Brushes.Transparent,
                                Foreground = ConfigManager.AlternativeSpellingsColor,
                                FontSize = ConfigManager.AlternativeSpellingsFontSize,
                                BorderThickness = new Thickness(0, 0, 0, 0),
                                Margin = new Thickness(5, 0, 0, 0),
                                Padding = new Thickness(0),
                                IsReadOnly = true,
                                IsUndoEnabled = false,
                                Cursor = Cursors.Arrow,
                                SelectionBrush = ConfigManager.HighlightColor,
                                IsInactiveSelectionHighlightEnabled = true,
                                ContextMenu = PopupContextMenu,
                            };

                            uiElementAlternativeSpellings.PreviewMouseLeftButtonUp += UiElement_PreviewMouseLeftButtonUp;
                            uiElementAlternativeSpellings.MouseMove += PopupMouseMove;
                            uiElementAlternativeSpellings.LostFocus += Unselect;
                            uiElementAlternativeSpellings.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                        }
                        else
                        {
                            uiElementAlternativeSpellings = new TextBlock
                            {
                                Name = LookupResult.AlternativeSpellings.ToString(),
                                Text = alternativeSpellingsText,
                                TextWrapping = TextWrapping.Wrap,
                                Foreground = ConfigManager.AlternativeSpellingsColor,
                                FontSize = ConfigManager.AlternativeSpellingsFontSize,
                                Margin = new Thickness(5, 0, 0, 0),
                            };
                        }

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
                            Text = $"({string.Join(",", value)})",
                            Foreground = ConfigManager.PrimarySpellingColor,
                            FontSize = ConfigManager.PrimarySpellingFontSize,
                            Margin = new Thickness(5, 0, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                        };
                        break;

                    case LookupResult.ROrthographyInfoList:
                        // processed in MakeUiElementReadingsText()
                        break;

                    case LookupResult.AOrthographyInfoList:
                        // processed in MakeUiElementAlternativeSpellingsText()
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
                        string gradeString = "";
                        int gradeInt = Convert.ToInt32(value[0]);
                        switch (gradeInt)
                        {
                            case 0:
                                gradeString = "Hyougai";
                                break;
                            case <= 6:
                                gradeString = $"{gradeInt} (Kyouiku)";
                                break;
                            case 8:
                                gradeString = $"{gradeInt} (Jouyou)";
                                break;
                            case <= 10:
                                gradeString = $"{gradeInt} (Jinmeiyou)";
                                break;
                        }

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
                        throw new ArgumentOutOfRangeException(null, "Invalid LookupResult type");
                }
            }

            UIElement[] babies =
            {
                textBlockFoundSpelling, textBlockPOrthographyInfo, uiElementReadings, uiElementAlternativeSpellings,
                textBlockProcess, textBlockFoundForm, textBlockEdictID, textBlockFrequency, textBlockDictType
            };

            for (int i = 0; i < babies.Length; i++)
            {
                UIElement baby = babies[i];

                if (baby is TextBlock textBlock)
                {
                    if (textBlock == null) continue;

                    // common emptiness check
                    if (textBlock.Text == "")
                        continue;

                    // POrthographyInfo check
                    if (textBlock.Text == "()")
                        continue;

                    // Frequency check
                    if ((textBlock.Text == ("#" + Storage.FakeFrequency)) || textBlock.Text == "#0")
                        continue;

                    baby.MouseLeave += OnMouseLeave;

                    // 0 = textBlockFoundSpelling, 2 = uiElementReadings
                    if ((i == 0 || i == 2) && Storage.Dicts.TryGetValue(DictType.Kanjium, out Dict kanjiumDict) && kanjiumDict.Active)
                    {
                        List<string> readings = result[LookupResult.Readings];

                        if (i == 0 && readings.Any())
                        {
                            top.Children.Add(baby);
                        }

                        else
                        {
                            List<string> alternativeSpellings =
                                result.ContainsKey(LookupResult.AlternativeSpellings)
                                ? result[LookupResult.AlternativeSpellings]
                                : null;

                            Grid pitchAccentGrid = CreatePitchAccentGrid(result[LookupResult.FoundSpelling][0],
                                alternativeSpellings,
                                result[LookupResult.Readings],
                                textBlock.Text.Split(", ").ToList());

                            if (pitchAccentGrid.Children.Count == 0)
                            {
                                top.Children.Add(baby);
                            }

                            else
                            {
                                pitchAccentGrid.Children.Add(baby);
                                top.Children.Add(pitchAccentGrid);
                            }
                        }
                    }

                    else
                        top.Children.Add(baby);
                }

                else if (baby is TextBox textBox)
                {
                    if (textBox == null) continue;

                    // common emptiness check
                    if (textBox.Text == "")
                        continue;

                    baby.MouseLeave += OnMouseLeave;

                    if ((i == 0 || i == 2) && Storage.Dicts.TryGetValue(DictType.Kanjium, out Dict kanjiumDict) && kanjiumDict.Active)
                    {
                        List<string> readings = result[LookupResult.Readings];

                        if (i == 0 && readings.Any())
                        {
                            top.Children.Add(baby);
                        }

                        else
                        {
                            List<string> alternativeSpellings =
                                result.ContainsKey(LookupResult.AlternativeSpellings)
                                ? result[LookupResult.AlternativeSpellings]
                                : null;

                            Grid pitchAccentGrid = CreatePitchAccentGrid(result[LookupResult.FoundSpelling][0],
                                alternativeSpellings,
                                result[LookupResult.Readings],
                                textBox.Text.Split(", ").ToList());

                            if (pitchAccentGrid.Children.Count == 0)
                            {
                                top.Children.Add(baby);
                            }

                            else
                            {
                                pitchAccentGrid.Children.Add(baby);
                                top.Children.Add(pitchAccentGrid);
                            }
                        }
                    }

                    else
                        top.Children.Add(baby);
                }
            }

            if (uiElementDefinitions != null)
                bottom.Children.Add(uiElementDefinitions);

            TextBlock[] babiesKanji =
            {
                textBlockOnReadings, textBlockKunReadings, textBlockNanori, textBlockGrade, textBlockStrokeCount,
                textBlockComposition,
            };
            foreach (TextBlock baby in babiesKanji)
            {
                if (baby == null) continue;

                // common emptiness check
                if (baby.Text == "")
                    continue;

                baby.MouseLeave += OnMouseLeave;

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

            innerStackPanel.MouseLeave += OnMouseLeave;
            top.MouseLeave += OnMouseLeave;
            bottom.MouseLeave += OnMouseLeave;

            return innerStackPanel;
        }

        private static Grid CreatePitchAccentGrid(string foundSpelling, List<string> alternativeSpellings, List<string> readings, List<string> splitReadingsWithRInfo)
        {
            Dictionary<string, List<IResult>> kanjiumDict = Storage.Dicts[DictType.Kanjium].Contents;
            Grid pitchAccentGrid = new();

            bool hasReading = readings.Any();

            List<string> expressions = hasReading ? readings : new List<string> { foundSpelling };

            double horizontalOffsetForReaading = WindowsUtils.MeasureTextSize(" ", ConfigManager.ReadingsFontSize).Width;

            for (int i = 0; i < expressions.Count; i++)
            {
                string expression = expressions[i];
                List<string> combinedFormList = Kana.CreateCombinedForm(expression);

                if (i > 0)
                {
                    horizontalOffsetForReaading += WindowsUtils.MeasureTextSize(splitReadingsWithRInfo[i - 1] + ", ", ConfigManager.ReadingsFontSize).Width;
                }

                if (kanjiumDict.TryGetValue(expression, out List<IResult> kanjiumListResult))
                {
                    for (int j = 0; j < kanjiumListResult.Count; j++)
                    {
                        var kanjiumResult = (KanjiumResult)kanjiumListResult[j];

                        if (foundSpelling == kanjiumResult.Spelling || (alternativeSpellings?.Contains(kanjiumResult.Spelling) ?? false))
                        {
                            if (hasReading && expression != kanjiumResult.Reading)
                                continue;

                            Polyline polyline = new()
                            {
                                StrokeThickness = 2,
                                Stroke = ConfigManager.PitchAccentMarkerColor,
                                StrokeDashArray = new DoubleCollection { 1, 3 },
                            };

                            bool lowPitch = false;
                            double horizontalOffsetForChar = horizontalOffsetForReaading;
                            for (int k = 0; k < combinedFormList.Count; k++)
                            {
                                Size charSize = WindowsUtils.MeasureTextSize(combinedFormList[k], ConfigManager.ReadingsFontSize);

                                if (kanjiumResult.Position - 1 == k)
                                {
                                    polyline.Points.Add(new Point(horizontalOffsetForChar, 0));
                                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, charSize.Height));

                                    lowPitch = true;
                                }

                                else if (k == 0)
                                {
                                    polyline.Points.Add(new Point(horizontalOffsetForChar, charSize.Height));
                                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, charSize.Height));
                                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                                }

                                else
                                {
                                    double charHeight = lowPitch ? charSize.Height : 0;
                                    polyline.Points.Add(new Point(horizontalOffsetForChar, charHeight));
                                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, charHeight));
                                }

                                horizontalOffsetForChar += charSize.Width;
                            }

                            pitchAccentGrid.Children.Add(polyline);
                        }
                    }
                }
            }

            return pitchAccentGrid;
        }

        private void Unselect(object sender, RoutedEventArgs e)
        {
            Unselect((TextBox)sender);
            //((TextBox)sender).Select(0, 0);
        }

        private static void Unselect(TextBox tb)
        {
            double verticalOffset = tb.VerticalOffset;
            tb.Select(0, 0);
            tb.ScrollToVerticalOffset(verticalOffset);
        }

        private void TextBoxPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ManageDictionariesButton.IsEnabled = Storage.Ready
                                                 && !Storage.UpdatingJMdict
                                                 && !Storage.UpdatingJMnedict
                                                 && !Storage.UpdatingKanjidic;

            AddNameButton.IsEnabled = Storage.Ready;
            AddWordButton.IsEnabled = Storage.Ready;

            _lastSelectedText = ((TextBox)sender).SelectedText;
        }

        private void PopupMouseMove(object sender, MouseEventArgs e)
        {
            if (ConfigManager.LookupOnSelectOnly)
                return;

            _childPopupWindow ??= new PopupWindow(this);

            // prevents stray PopupWindows being created when you move your mouse too fast
            if (MiningMode)
                _childPopupWindow.Definitions_MouseMove((TextBox)sender);
        }

        private void FoundSpelling_MouseEnter(object sender, MouseEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            _playAudioIndex = (int)textBlock.Tag;
        }

        private void FoundSpelling_MouseLeave(object sender, MouseEventArgs e)
        {
            _playAudioIndex = 0;
        }

        private async void FoundSpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!MiningMode && !ConfigManager.LookupOnSelectOnly)
                return;

            MiningMode = false;
            TextBlockMiningModeReminder.Visibility = Visibility.Collapsed;
            Hide();

            var miningParams = new Dictionary<JLField, string>();
            foreach (JLField jlf in Enum.GetValues(typeof(JLField)))
            {
                miningParams[jlf] = "";
            }

            var textBlock = (TextBlock)sender;

            WrapPanel top;

            if (textBlock.Parent is Grid foundSpellingGrid)
            {
                top = (WrapPanel)foundSpellingGrid.Parent;
            }

            else
            {
                top = (WrapPanel)textBlock.Parent;
            }

            foreach (UIElement child in top.Children)
            {
                if (child is TextBox chi)
                {
                    if (Enum.TryParse(chi.Name, out LookupResult result))
                    {
                        switch (result)
                        {
                            case LookupResult.Readings:
                                miningParams[JLField.Readings] = chi.Text;
                                break;
                            case LookupResult.AlternativeSpellings:
                                miningParams[JLField.AlternativeSpellings] = chi.Text;
                                break;
                        }
                    }
                }

                else if (child is TextBlock ch)
                {
                    if (Enum.TryParse(ch.Name, out LookupResult result))
                    {
                        switch (result)
                        {
                            case LookupResult.FoundSpelling:
                                miningParams[JLField.FoundSpelling] = ch.Text;
                                break;
                            case LookupResult.FoundForm:
                                miningParams[JLField.FoundForm] = ch.Text;
                                break;
                            case LookupResult.EdictID:
                                miningParams[JLField.EdictID] = ch.Text;
                                break;
                            case LookupResult.Frequency:
                                miningParams[JLField.Frequency] = ch.Text;
                                break;
                            case LookupResult.DictType:
                                miningParams[JLField.DictType] = ch.Text;
                                break;
                            case LookupResult.Process:
                                miningParams[JLField.Process] = ch.Text;
                                break;
                        }
                    }
                }

                else if (child is Grid grid)
                {
                    foreach (UIElement uiElement in grid.Children)
                    {
                        if (uiElement is TextBlock textBlockCG)
                        {
                            if (Enum.TryParse(textBlockCG.Name, out LookupResult result))
                            {
                                switch (result)
                                {
                                    case LookupResult.FoundSpelling:
                                        miningParams[JLField.FoundSpelling] = textBlockCG.Text;
                                        break;
                                }
                            }
                        }

                        else if (uiElement is TextBox textBoxCG)
                        {
                            if (Enum.TryParse(textBoxCG.Name, out LookupResult result))
                            {
                                switch (result)
                                {
                                    case LookupResult.Readings:
                                        miningParams[JLField.Readings] = textBoxCG.Text;
                                        break;
                                }
                            }
                        }
                    }
                }

            }

            var innerStackPanel = (StackPanel)top.Parent;
            var bottom = (StackPanel)innerStackPanel.Children[1];
            foreach (object child in bottom.Children)
            {
                if (child is TextBox textBox)
                {
                    miningParams[JLField.Definitions] += textBox.Text.Replace("\n", "<br/>");
                    continue;
                }

                if (child is not TextBlock)
                    continue;

                textBlock = (TextBlock)child;

                if (Enum.TryParse(textBlock.Name, out LookupResult result))
                {
                    switch (result)
                    {
                        case LookupResult.StrokeCount:
                            miningParams[JLField.StrokeCount] += textBlock.Text;
                            break;
                        case LookupResult.Grade:
                            miningParams[JLField.Grade] += textBlock.Text;
                            break;
                        case LookupResult.Composition:
                            miningParams[JLField.Composition] += textBlock.Text;
                            break;
                        case LookupResult.OnReadings:
                        case LookupResult.KunReadings:
                        case LookupResult.Nanori:
                            if (!miningParams[JLField.Readings].EndsWith("<br/>"))
                            {
                                miningParams[JLField.Readings] += "<br/>";
                            }

                            miningParams[JLField.Readings] += textBlock.Text + "<br/>";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(null, "Invalid LookupResult type");
                    }
                }
            }

            miningParams[JLField.Context] = Utils.FindSentence(_currentText, _currentCharPosition);
            miningParams[JLField.TimeLocal] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

            await Mining.Mine(miningParams).ConfigureAwait(false);
        }

        private void Definitions_MouseMove(TextBox tb)
        {
            if (Storage.JapaneseRegex.IsMatch(tb.Text))
                TextBox_MouseMove(tb);
        }

        private void PopupListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            MouseWheelEventArgs e2 = new(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = ListBox.MouseWheelEvent,
                Source = e.Source
            };
            PopupListBox.RaiseEvent(e2);
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //int keyVal = (int)e.Key;
            //int numericKeyValue = -1;
            //if ((keyVal >= (int)Key.D1 && keyVal <= (int)Key.D9))
            //{
            //    numericKeyValue = (int)e.Key - (int)Key.D0 - 1;
            //}
            //else if (keyVal >= (int)Key.NumPad1 && keyVal <= (int)Key.NumPad9)
            //{
            //    numericKeyValue = (int)e.Key - (int)Key.NumPad0 - 1;
            //}

            if (WindowsUtils.KeyGestureComparer(e, ConfigManager.MiningModeKeyGesture))
            {
                MiningMode = true;
                TextBlockMiningModeReminder.Visibility = Visibility.Visible;

                PopUpScrollViewer.ScrollToTop();
                PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                Activate();
                Focus();

                ResultStackPanels.Clear();
                DisplayResults(true);
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.PlayAudioKeyGesture))
            {
                //int index = numericKeyValue != -1 ? numericKeyValue : _playAudioIndex;
                //if (index > PopupListBox.Items.Count - 1)
                //{
                //    WindowsUtils.Alert(AlertLevel.Error, "Index out of range");
                //    return;
                //}

                //var innerStackPanel = (StackPanel)PopupListBox.Items[index];

                string foundSpelling = null;
                string reading = null;

                var innerStackPanel = (StackPanel)PopupListBox.Items[_playAudioIndex];
                var top = (WrapPanel)innerStackPanel.Children[0];

                foreach (UIElement child in top.Children)
                {
                    if (child is TextBox chi)
                    {
                        if (Enum.TryParse(chi.Name, out LookupResult result))
                        {
                            switch (result)
                            {
                                case LookupResult.Readings:
                                    reading = chi.Text.Split(",")[0];
                                    break;
                            }
                        }
                    }

                    else if (child is TextBlock ch)
                    {
                        if (Enum.TryParse(ch.Name, out LookupResult result))
                        {
                            switch (result)
                            {
                                case LookupResult.FoundSpelling:
                                    foundSpelling = ch.Text;
                                    break;
                                case LookupResult.Readings:
                                    reading = ch.Text.Split(",")[0];
                                    break;
                            }
                        }
                    }

                    else if (child is Grid grid)
                    {
                        foreach (UIElement uiElement in grid.Children)
                        {
                            if (uiElement is TextBlock textBlockCG)
                            {
                                if (Enum.TryParse(textBlockCG.Name, out LookupResult result))
                                {
                                    switch (result)
                                    {
                                        case LookupResult.FoundSpelling:
                                            foundSpelling = textBlockCG.Text;
                                            break;
                                        case LookupResult.Readings:
                                            reading = textBlockCG.Text.Split(",")[0];
                                            break;
                                    }
                                }
                            }

                            else if (uiElement is TextBox textBoxCG)
                            {
                                if (Enum.TryParse(textBoxCG.Name, out LookupResult result))
                                {
                                    switch (result)
                                    {
                                        case LookupResult.Readings:
                                            reading = textBoxCG.Text.Split(",")[0];
                                            break;
                                    }
                                }
                            }

                        }
                    }
                }

                await Utils.GetAndPlayAudioFromJpod101(foundSpelling, reading, 1).ConfigureAwait(false);
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ClosePopupKeyGesture))
            {
                MiningMode = false;
                TextBlockMiningModeReminder.Visibility = Visibility.Collapsed;

                PopUpScrollViewer.ScrollToTop();
                PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

                if (ConfigManager.LookupOnSelectOnly && _parentPopupWindow == null)
                {
                    Unselect(MainWindow.Instance.MainTextBox);
                }

                else if (ConfigManager.LookupOnSelectOnly && _lastTextBox != null)
                {
                    Unselect(_lastTextBox);
                }

                Hide();
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.KanjiModeKeyGesture))
            {
                ConfigManager.Instance.KanjiMode = !ConfigManager.Instance.KanjiMode;
                LastText = "";
                //todo will only work for the FirstPopupWindow
                MainWindow.Instance.MainTextBox_MouseMove(null, null);
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ShowPreferencesWindowKeyGesture))
            {
                WindowsUtils.ShowPreferencesWindow();
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ShowAddNameWindowKeyGesture))
            {
                if (Storage.Ready)
                    WindowsUtils.ShowAddNameWindow(_lastSelectedText);
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ShowAddWordWindowKeyGesture))
            {
                if (Storage.Ready)
                    WindowsUtils.ShowAddWordWindow(_lastSelectedText);
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.ShowManageDictionariesWindowKeyGesture))
            {
                if (Storage.Ready)
                    WindowsUtils.ShowManageDictionariesWindow();
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.SearchWithBrowserKeyGesture))
            {
                WindowsUtils.SearchWithBrowser(_lastSelectedText);
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.InactiveLookupModeKeyGesture))
            {
                ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
            }
            else if (WindowsUtils.KeyGestureComparer(e, ConfigManager.MotivationKeyGesture))
            {
                WindowsUtils.Motivate($"Resources/Motivation");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!ConfigManager.LookupOnSelectOnly && !ConfigManager.FixedPopupPositioning && _childPopupWindow is { MiningMode: false })
            {
                _childPopupWindow.Hide();
                _childPopupWindow.LastText = "";
            }

            if (MiningMode || ConfigManager.LookupOnSelectOnly || ConfigManager.FixedPopupPositioning) return;

            Hide();
            LastText = "";
        }

        private void UiElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ConfigManager.LookupOnSelectOnly
                || Background.Opacity == 0
                || ConfigManager.InactiveLookupMode)
                return;

            //if (ConfigManager.RequireLookupKeyPress
            //    && !Keyboard.Modifiers.HasFlag(ConfigManager.LookupKey))
            //    return;

            _childPopupWindow ??= new PopupWindow(this);

            _childPopupWindow.LookupOnSelect((TextBox)sender);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!ConfigManager.LookupOnSelectOnly && !ConfigManager.FixedPopupPositioning && _childPopupWindow is { MiningMode: false })
            {
                _childPopupWindow.Hide();
                _childPopupWindow.LastText = "";
            }

            if (MiningMode || ConfigManager.LookupOnSelectOnly || ConfigManager.FixedPopupPositioning) return;

            Hide();
            LastText = "";

            if (ConfigManager.HighlightLongestMatch)
            {
                Unselect(MainWindow.Instance.MainTextBox);
            }
        }
    }
}
