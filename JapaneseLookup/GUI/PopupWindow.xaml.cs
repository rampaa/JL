using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using JapaneseLookup.Anki;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PopupWindow.xaml
    /// </summary>
    public partial class PopupWindow : Window
    {
        private static PopupWindow _instance;
        private static int _playAudioIndex;

        private static readonly System.Windows.Interop.WindowInteropHelper InteropHelper =
            new(Application.Current.MainWindow!);

        private static readonly System.Windows.Forms.Screen ActiveScreen =
            System.Windows.Forms.Screen.FromHandle(InteropHelper.Handle);

        public static PopupWindow Instance
        {
            get { return _instance ??= new PopupWindow(); }
        }

        public PopupWindow()
        {
            InitializeComponent();
            MaxWidth = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxWidth") ??
                                 throw new InvalidOperationException());
            MaxHeight = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxHeight") ??
                                  throw new InvalidOperationException());
        }

        public void UpdatePosition(Point cursorPosition)
        {
            var needsFlipX = ConfigManager.PopupFlipX && cursorPosition.X + Width > ActiveScreen.Bounds.Width;
            var needsFlipY = ConfigManager.PopupFlipY && cursorPosition.Y + Height > ActiveScreen.Bounds.Height;

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

            // push if +OOB
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

        internal static StackPanel MakeResultStackPanel(string sentence, Dictionary<LookupResult, List<string>> result,
            int index)
        {
            var innerStackPanel = new StackPanel
            {
                Margin = new Thickness(4, 2, 4, 2),
            };
            var top = new WrapPanel();
            var bottom = new StackPanel();


            // top
            TextBlock textBlockFoundSpelling = null;
            TextBlock textBlockPOrthographyInfo = null;
            TextBlock textBlockReadings = null;
            TextBlock textBlockAlternativeSpellings = null;
            TextBlock textBlockProcess = null;
            TextBlock textBlockFrequency = null;
            var textBlockContext = new TextBlock
            {
                Name = "context",
                Text = sentence,
                Visibility = Visibility.Collapsed
            };
            TextBlock textBlockFoundForm = null;
            TextBlock textBlockEdictID = null;

            // bottom
            TextBlock textBlockDefinitions = null;
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
                            Visibility = Visibility.Collapsed
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
                        };
                        break;


                    // EDICT
                    case LookupResult.FoundSpelling:
                        textBlockFoundSpelling = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = value[0],
                            Tag = index, // for audio
                            Foreground = ConfigManager.FoundSpellingColor,
                            FontSize = ConfigManager.FoundSpellingFontSize,
                        };
                        textBlockFoundSpelling.MouseEnter += FoundSpelling_MouseEnter; // for audio
                        textBlockFoundSpelling.MouseLeave += FoundSpelling_MouseLeave; // for audio
                        textBlockFoundSpelling.PreviewMouseUp += FoundSpelling_PreviewMouseUp; // for mining
                        break;

                    case LookupResult.KanaSpellings:
                        // var textBlockKanaSpellings = new TextBlock
                        // {
                        //     Name = "kanaSpellings",
                        //     Text = string.Join(" ", result["kanaSpellings"]),
                        //     TextWrapping = TextWrapping.Wrap,
                        //     Foreground = Brushes.White
                        // };
                        break;

                    case LookupResult.Readings:
                        textBlockReadings = MakeTextBlockReadings(result);
                        break;

                    case LookupResult.Definitions:
                        textBlockDefinitions = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = string.Join(", ", value),
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = ConfigManager.DefinitionsColor,
                            FontSize = ConfigManager.DefinitionsFontSize,
                            Margin = new Thickness(2, 2, 2, 2),
                        };
                        break;

                    case LookupResult.EdictID:
                        textBlockEdictID = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = string.Join(", ", value),
                            Visibility = Visibility.Collapsed
                        };
                        break;

                    case LookupResult.AlternativeSpellings:
                        textBlockAlternativeSpellings = MakeTextBlockAlternativeSpellings(result);
                        break;

                    case LookupResult.Process:
                        textBlockProcess = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = string.Join(", ", value),
                            Foreground = ConfigManager.ProcessColor,
                            FontSize = ConfigManager.ProcessFontSize,
                            Margin = new Thickness(5, 0, 0, 0),
                        };
                        break;

                    case LookupResult.POrthographyInfoList:
                        textBlockPOrthographyInfo = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "(" + string.Join(",", value) + ")",
                            //Foreground = ConfigManager.pOrthographyInfoColor,
                            //FontSize = ConfigManager.pOrthographyInfoFontSize,
                            Margin = new Thickness(5, 0, 0, 0),
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
                        if (!value.Any())
                            break;

                        textBlockOnReadings = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "On" + ": " + string.Join(", ", value),
                            Foreground = ConfigManager.ReadingsColor,
                            FontSize = ConfigManager.ReadingsFontSize,
                            Margin = new Thickness(2, 0, 0, 0),
                        };
                        break;

                    case LookupResult.KunReadings:
                        if (!value.Any())
                            break;

                        textBlockKunReadings = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = "Kun" + ": " + string.Join(", ", value),
                            Foreground = ConfigManager.ReadingsColor,
                            FontSize = ConfigManager.ReadingsFontSize,
                            Margin = new Thickness(2, 0, 0, 0),
                        };
                        break;

                    case LookupResult.Nanori:
                        if (!value.Any())
                            break;

                        textBlockNanori = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = key + ": " + string.Join(", ", value),
                            Foreground = ConfigManager.ReadingsColor,
                            FontSize = ConfigManager.ReadingsFontSize,
                            Margin = new Thickness(2, 0, 0, 0),
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
                        };
                        break;

                    case LookupResult.Grade:
                        var gradeString = "";
                        var gradeInt = Convert.ToInt32(value[0]);
                        gradeString = gradeInt switch
                        {
                            0 => "Hyougai",
                            <=6 => $"Kyouiku ({gradeInt})",
                            8 => $"Jouyou ({gradeInt})",
                            <=10 => $"Jinmeiyou ({gradeInt})",
                            _ => gradeString
                        };

                        textBlockGrade = new TextBlock
                        {
                            Name = key.ToString(),
                            Text = key + ": " + gradeString,
                            // Foreground = ConfigManager. Color,
                            FontSize = ConfigManager.DefinitionsFontSize,
                            Margin = new Thickness(2, 2, 2, 2),
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
                textBlockContext, textBlockFoundForm, textBlockEdictID, // undisplayed, for mining
                textBlockFrequency,
            };
            foreach (TextBlock baby in babies)
            {
                if (baby == null) continue;

                Enum.TryParse(baby.Name, out LookupResult enumName);

                // common emptiness check; these two have their text as inline Runs
                if (baby.Text == "" &&
                    !(enumName == LookupResult.AlternativeSpellings || enumName == LookupResult.Readings))
                    continue;

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

            innerStackPanel.Children.Add(top);
            innerStackPanel.Children.Add(bottom);
            return innerStackPanel;
        }

        private static TextBlock MakeTextBlockReadings(Dictionary<LookupResult, List<string>> result)
        {
            result.TryGetValue(LookupResult.ROrthographyInfoList, out var rOrthographyInfoList);

            var readings = result[LookupResult.Readings];

            var textBlockReadings = new TextBlock
            {
                Name = LookupResult.Readings.ToString(),
                Text = "",
                Tag = string.Join(", ", readings), // for mining
                Foreground = ConfigManager.ReadingsColor,
                FontSize = ConfigManager.ReadingsFontSize,
                Margin = new Thickness(5, 0, 0, 0),
            };

            if (readings.Count == 0) return textBlockReadings;

            for (var index = 0; index < readings.Count; index++)
            {
                var runReading = new Run(readings[index])
                {
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                };
                textBlockReadings.Inlines.Add(runReading);

                if (rOrthographyInfoList != null)
                {
                    if (index < rOrthographyInfoList.Count)
                    {
                        var runReadingOrtho = new Run("(" + rOrthographyInfoList[index] + ")")
                        {
                            Foreground = ConfigManager.ROrthographyInfoColor,
                            FontSize = ConfigManager.ROrthographyInfoFontSize,
                        };
                        if (runReadingOrtho.Text != "()")
                        {
                            textBlockReadings.Inlines.Add(" ");
                            textBlockReadings.Inlines.Add(runReadingOrtho);
                        }
                    }
                }

                if (index != readings.Count - 1)
                {
                    textBlockReadings.Inlines.Add(", ");
                }
            }

            return textBlockReadings;
        }

        private static TextBlock MakeTextBlockAlternativeSpellings(Dictionary<LookupResult, List<string>> result)
        {
            var alternativeSpellings = result[LookupResult.AlternativeSpellings];

            var textBlockAlternativeSpellings = new TextBlock
            {
                Name = LookupResult.AlternativeSpellings.ToString(),
                Text = "",
                Tag = string.Join(", ", alternativeSpellings), // for mining
                Foreground = ConfigManager.AlternativeSpellingsColor,
                FontSize = ConfigManager.AlternativeSpellingsFontSize,
                Margin = new Thickness(5, 0, 0, 0),
            };

            if (alternativeSpellings.Count == 0) return textBlockAlternativeSpellings;

            textBlockAlternativeSpellings.Inlines.Add("(");

            for (var index = 0; index < alternativeSpellings.Count; index++)
            {
                var runAlt = new Run(alternativeSpellings[index])
                {
                    Foreground = ConfigManager.AlternativeSpellingsColor,
                    FontSize = ConfigManager.AlternativeSpellingsFontSize,
                };
                textBlockAlternativeSpellings.Inlines.Add(runAlt);

                if (index < alternativeSpellings.Count)
                {
                    var runAltOrtho = new Run("(" + alternativeSpellings[index] + ")")
                    {
                        Foreground = ConfigManager.AOrthographyInfoColor,
                        FontSize = ConfigManager.AOrthographyInfoFontSize,
                    };
                    if (runAltOrtho.Text != "()")
                    {
                        textBlockAlternativeSpellings.Inlines.Add(" ");
                        textBlockAlternativeSpellings.Inlines.Add(runAltOrtho);
                    }
                }

                if (index != alternativeSpellings.Count - 1)
                {
                    textBlockAlternativeSpellings.Inlines.Add(", ");
                }
            }

            textBlockAlternativeSpellings.Inlines.Add(")");

            return textBlockAlternativeSpellings;
        }

        private static void FoundSpelling_MouseEnter(object sender, MouseEventArgs e)
        {
            var textBlock = (TextBlock) sender;
            _playAudioIndex = (int) textBlock.Tag;
        }

        private static void FoundSpelling_MouseLeave(object sender, MouseEventArgs e)
        {
            _playAudioIndex = 0;
        }

        private static async void FoundSpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MiningMode = false;
            Instance.Hide();

            string foundSpelling = null;
            string readings = null;
            string definitions = "";
            string context = null;
            string foundForm = null;
            string edictID = null;
            var timeLocal = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
            string alternativeSpellings = null;
            string frequency = null;

            var textBlock = (TextBlock) sender;
            var top = (WrapPanel) textBlock.Parent;
            foreach (TextBlock child in top.Children)
            {
                if (child.Name == "context")
                {
                    context = child.Text;
                }

                Enum.TryParse(child.Name, out LookupResult result);
                switch (result)
                {
                    case LookupResult.FoundSpelling:
                        foundSpelling = child.Text;
                        break;
                    case LookupResult.Readings:
                        readings = (string) child.Tag;
                        break;
                    // case "context":
                    //     context = child.Text;
                    //     break;
                    case LookupResult.FoundForm:
                        foundForm = child.Text;
                        break;
                    case LookupResult.EdictID:
                        edictID = child.Text;
                        break;
                    case LookupResult.AlternativeSpellings:
                        alternativeSpellings = (string) child.Tag;
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

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var innerStackPanel = (StackPanel) top.Parent;
            var bottom = (StackPanel) innerStackPanel.Children[1];
            foreach (TextBlock child in bottom.Children)
            {
                Enum.TryParse(child.Name, out LookupResult result);
                switch (result)
                {
                    case LookupResult.Definitions:
                        definitions += child.Text;
                        break;
                    case LookupResult.StrokeCount:
                        // TODO
                        break;
                    case LookupResult.Grade:
                        // TODO
                        break;
                    case LookupResult.Composition:
                        // TODO
                        break;
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
                frequency
            );
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

            // TODO: find a better solution for this that avoids adding an element to Instance.StackPanel
            var mediaElement = new MediaElement { Source = uri, Volume = 1, Visibility = Visibility.Collapsed };
            Instance.StackPanel.Children.Add(mediaElement);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.M:
                {
                    MainWindow.MiningMode = true;
                    PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    // TODO: Tell the user that they are in mining mode
                    Instance.Activate();
                    Instance.Focus();

                    break;
                }

                case Key.P:
                {
                    string foundSpelling = null;
                    string reading = null;

                    var innerStackPanel = (StackPanel) StackPanel.Children[_playAudioIndex];
                    var top = (WrapPanel) innerStackPanel.Children[0];

                    foreach (TextBlock child in top.Children)
                    {
                        Enum.TryParse(child.Name, out LookupResult result);
                        switch (result)
                        {
                            case LookupResult.FoundSpelling:
                                foundSpelling = child.Text;
                                break;
                            case LookupResult.Readings:
                                reading = ((string) child.Tag).Split(",")[0];
                                break;
                        }
                    }

                    PlayAudio(foundSpelling, reading);

                    break;
                }

                case Key.Escape:
                {
                    if (MainWindow.MiningMode)
                    {
                        MainWindow.MiningMode = false;
                        PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                        Hide();
                    }

                    break;
                }

                case Key.K:
                    {
                        ConfigManager.KanjiMode = !ConfigManager.KanjiMode;
                        break;
                    }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}