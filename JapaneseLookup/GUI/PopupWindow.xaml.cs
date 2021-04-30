using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
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

        // TODO: ShowInTaskbar = false
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
                var stackPanel = new StackPanel();

                var textBlockFoundSpelling = new TextBlock
                {
                    Name = "foundSpelling",
                    Text = result["foundSpelling"][0],
                    Foreground = Brushes.White,
                };
                textBlockFoundSpelling.PreviewMouseUp += FoundSpelling_PreviewMouseUp;

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

                var frequency = string.Join(", ", result["frequency"]);
                var textBlockFrequency = new TextBlock
                {
                    Name = "frequency",
                    Text = frequency,
                    Foreground = Brushes.White,
                };

                var process = string.Join(", ", result["process"]);
                var textBlockProcess = new TextBlock
                {
                    Name = "process",
                    Text = process,
                    Foreground = Brushes.White,
                };

                stackPanel.Children.Add(textBlockFoundSpelling);
                stackPanel.Children.Add(textBlockReadings);
                stackPanel.Children.Add(textBlockDefinitions);
                stackPanel.Children.Add(textBlockJmdictID);
                stackPanel.Children.Add(textBlockAlternativeSpellings);
                if (frequency != MainWindow.FakeFrequency)
                    stackPanel.Children.Add(textBlockFrequency);
                stackPanel.Children.Add(textBlockProcess);
                Instance.StackPanel.Children.Add(stackPanel);
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
            var context = MainWindow.LastSentence;
            string definitionsRaw = null;
            string foundText = null;
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
                    // case "context":
                    //
                    //     break;
                    case "foundText":
                        // TODO: foundText = child.Text;
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
                foundText,
                jmdictID,
                timeLocal,
                alternativeSpellings,
                frequency
            );
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.M:
                {
                    MainWindow.MiningMode = true;
                    // TODO: Tell the user that they are in mining mode
                    // PopupWindow.Instance.ScrollViewer.Visibility = Visibility.Visible;
                    Instance.Focus();

                    break;
                }
                case Key.C:
                {
                    var miningSetupWindow = new MiningSetupWindow();
                    miningSetupWindow.Show();

                    break;
                }
                case Key.P:
                {
                    // TODO: Play audio

                    break;
                }

                case Key.Escape:
                {
                    if (MainWindow.MiningMode)
                        MainWindow.MiningMode = false;
                    break;
                }
            }
        }
    }
}