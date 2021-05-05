using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JapaneseLookup.Anki;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PopupWindow.xaml
    /// </summary>
    public partial class PopupWindow : Window
    {
        private static PopupWindow _instance;

        public static PopupWindow Instance
        {
            get { return _instance ??= new PopupWindow(); }
        }

        public PopupWindow()
        {
            InitializeComponent();
        }

        public static void UpdatePosition(Point position)
        {
            Instance.Left = position.X + 0;
            Instance.Top = position.Y + 20;
        }

        private void StackPanel_KeyDown(object sender, KeyEventArgs e)
        {
        }

        internal static void DisplayResults(string sentence,
            List<Dictionary<string, List<string>>> results)
        {
            foreach (var result in results)
            {
                var innerStackPanel = new StackPanel();

                var textBlockFoundSpelling = new TextBlock
                {
                    Name = "foundSpelling",
                    Text = result["foundSpelling"][0],
                    Foreground = Brushes.White,
                };
                textBlockFoundSpelling.PreviewMouseUp += FoundSpelling_PreviewMouseUp;
                textBlockFoundSpelling.KeyDown += FoundSpelling_KeyDown;

                var textBlockKanaSpellings = new TextBlock
                {
                    Name = "kanaSpellings",
                    Text = string.Join(" ", result["kanaSpellings"]),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White
                };

                var textBlockReadings = new TextBlock
                {
                    Name = "readings",
                    Text = string.Join(", ", result["readings"]),
                    Foreground = Brushes.White
                };

                var textBlockDefinitions = new TextBlock
                {
                    Name = "definitions",
                    Text = string.Join("", result["definitions"]),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White
                };

                var textBlockContext = new TextBlock
                {
                    Name = "context",
                    Text = sentence,
                    Foreground = Brushes.White,
                    Visibility = Visibility.Collapsed
                };

                var textBlockFoundForm = new TextBlock
                {
                    Name = "foundForm",
                    Text = string.Join("", result["foundForm"]),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White,

                    // decide if we want to display this
                    Visibility = Visibility.Collapsed
                };

                var textBlockJmdictID = new TextBlock
                {
                    Name = "jmdictID",
                    Text = string.Join(", ", result["jmdictID"]),
                    Visibility = Visibility.Collapsed
                };

                var textBlockAlternativeSpellings = new TextBlock
                {
                    Name = "alternativeSpellings",
                    Text = string.Join(", ", result["alternativeSpellings"]),
                    Foreground = Brushes.White
                };

                var textBlockProcess = new TextBlock
                {
                    Name = "process",
                    Text = string.Join(", ", result["process"]),
                    Foreground = Brushes.White,
                };

                var frequency = string.Join(", ", result["frequency"]);
                var textBlockFrequency = new TextBlock
                {
                    Name = "frequency",
                    Text = frequency,
                    Foreground = Brushes.White,
                };

                innerStackPanel.Children.Add(textBlockFoundSpelling);
                //innerStackPanel.Children.Add(textBlockKanaSpellings);
                innerStackPanel.Children.Add(textBlockReadings);
                innerStackPanel.Children.Add(textBlockDefinitions);
                innerStackPanel.Children.Add(textBlockContext);
                innerStackPanel.Children.Add(textBlockFoundForm);
                innerStackPanel.Children.Add(textBlockJmdictID);
                innerStackPanel.Children.Add(textBlockAlternativeSpellings);
                innerStackPanel.Children.Add(textBlockProcess);
                if (frequency != MainWindowUtilities.FakeFrequency)
                    innerStackPanel.Children.Add(textBlockFrequency);

                Instance.StackPanel.Children.Add(innerStackPanel);
                Instance.StackPanel.Children.Add(new Separator());
            }
        }

        private static void FoundSpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MiningMode = false;
            Instance.Hide();

            // doesn't work :(
            // string readings = stackPanel.FindName("readings").ToString();

            string foundSpelling = null;
            string readings = null;
            string definitions = null;
            string context = null;
            string definitionsRaw = null;
            string foundForm = null;
            string jmdictID = null;
            var timeLocal = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
            string alternativeSpellings = null;
            string frequency = null;

            var textBlock = (TextBlock) sender;
            var stackPanel = (StackPanel) textBlock.Parent;

            foreach (TextBlock child in stackPanel.Children)
            {
                switch (child.Name)
                {
                    case "foundSpelling":
                        foundSpelling = child.Text;
                        break;
                    case "readings":
                        readings = child.Text;
                        break;
                    case "definitions":
                        // TODO: definitions = html
                        definitionsRaw = child.Text;
                        break;
                    case "context":
                        context = child.Text;
                        break;
                    case "foundForm":
                        foundForm = child.Text;
                        break;
                    case "jmdictID":
                        jmdictID = child.Text;
                        break;
                    case "alternativeSpellings":
                        alternativeSpellings = child.Text;
                        break;
                    case "frequency":
                        frequency = child.Text;
                        break;
                }
            }

            Mining.Mine(
                foundSpelling,
                readings,
                definitions,
                context,
                definitionsRaw,
                foundForm,
                jmdictID,
                timeLocal,
                alternativeSpellings,
                frequency
            );
        }

        static void PlayAudio(string foundSpelling, string reading)
        {
            Debug.WriteLine(foundSpelling + " " + reading);

            Uri uri = new(
                "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji=" +
                foundSpelling +
                "&kana=" +
                reading
            );

            // var sound = AnkiConnect.GetAudio("猫", "ねこ").Result;
            var test = new MediaElement
                {Source = uri, Volume = 1, Visibility = Visibility.Collapsed};
            Instance.StackPanel.Children.Add(test);
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
                    var innerStackPanel = (StackPanel) StackPanel.Children[0];
                    string foundSpelling = null;
                    string reading = null;

                    foreach (TextBlock child in innerStackPanel.Children)
                    {
                        switch (child.Name)
                        {
                            case "foundSpelling":
                                foundSpelling = child.Text;
                                break;
                            case "readings":
                                reading = child.Text.Split(",")[0];
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
                    }

                    break;
                }
            }
        }

        private static void FoundSpelling_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.P:
                {
                    var textBlock = (TextBlock) sender;
                    var innerStackPanel = (StackPanel) textBlock.Parent;
                    string foundSpelling = null;
                    string reading = null;

                    foreach (TextBlock child in innerStackPanel.Children)
                    {
                        switch (child.Name)
                        {
                            case "foundSpelling":
                                foundSpelling = child.Text;
                                break;
                            case "readings":
                                reading = child.Text.Split(",")[0];
                                break;
                        }
                    }

                    PlayAudio(foundSpelling, reading);

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