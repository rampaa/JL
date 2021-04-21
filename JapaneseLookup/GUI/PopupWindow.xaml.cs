using System;
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

        // TODO: ShowInTaskbar = false
        public static PopupWindow Instance
        {
            get { return _instance ??= new PopupWindow(); }
        }

        public PopupWindow()
        {
            InitializeComponent();
        }

        private void StackPanel_KeyDown(object sender, KeyEventArgs e)
        {
        }

        internal static void Display(Point position, string parsedWord)
        {
            Instance.Left = position.X;
            Instance.Top = position.Y + 30;

            Instance.StackPanel.Children.Clear();
            var results = MainWindow.LookUp(parsedWord);
            if (results == null)
            {
                Instance.Hide();
                return;
            }

            foreach (var result in results)
            {
                var stackPanel = new StackPanel();

                var textBlockFoundSpelling = new TextBlock
                {
                    Name = "foundSpelling",
                    Text = parsedWord,
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
                var textBlockFrequency = new TextBlock
                {
                    Name = "frequency",
                    Text = string.Join(", ", result["frequency"]),
                    Foreground = Brushes.White
                };

                stackPanel.Children.Add(textBlockFoundSpelling);
                stackPanel.Children.Add(textBlockReadings);
                stackPanel.Children.Add(textBlockDefinitions);
                stackPanel.Children.Add(textBlockJmdictID);
                stackPanel.Children.Add(textBlockAlternativeSpellings);
                stackPanel.Children.Add(textBlockFrequency);

                Instance.StackPanel.Children.Add(stackPanel);
                Instance.StackPanel.Children.Add(new Separator());
            }

            Instance.Show();
        }

        private static void FoundSpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MiningMode = false;
            PopupWindow.Instance.Hide();

            // doesn't work :(
            // string readings = stackPanel.FindName("readings").ToString();

            string foundSpelling = null;
            string readings = null;
            string definitions = null;
            string context = null;
            string definitionsRaw = null;
            string foundText = null;
            string jmdictID = null;
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
                        // TODO: context = child.Text;
                        break;
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

            string timeLocal = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

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
    }
}