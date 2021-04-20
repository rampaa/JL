using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JapaneseLookup.Anki;
using JapaneseLookup.Parsers;
using JapaneseLookup.EDICT;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Regex JapaneseRegex =
            new(@"[\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u4e00-\u9faf\u3400-\u4dbf]");

        private string _backlog = "";
        private readonly IParser _parser = new Mecab();
        private string _lastWord = "";
        public static bool _miningMode = false;

        public MainWindow()
        {
            InitializeComponent();
            Task.Run(JMdictLoader.Loader);
            // init AnkiConnect so that it doesn't block later
            Task.Run(AnkiConnect.GetDeckNames);
            // Mining.Mine(null, null, null, null);

            CopyFromClipboard();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var windowClipboardManager = new ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += ClipboardChanged;

            CopyFromClipboard();
        }

        private void CopyFromClipboard()
        {
            if (Clipboard.ContainsText())
            {
                try
                {
                    string text = Clipboard.GetText();
                    if (JapaneseRegex.IsMatch(text))
                    {
                        _backlog += text + "\n";
                        MainTextBox.Text = text + "\n";
                    }
                }
                catch
                {
                }
            }
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            CopyFromClipboard();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private void MainTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_miningMode) return;

            int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
            if (charPosition != -1)
            {
                string parsedWord = _parser.Parse(MainTextBox.Text[charPosition..]);
                // if (parsedWord == _lastWord) return;
                // _lastWord = parsedWord;

                // TODO: Lookafter and lookbehind.
                // TODO: Show results correctly.

                Point position = PointToScreen(Mouse.GetPosition(this));
                PopupWindow popUpWindow = PopupWindow.Instance;
                popUpWindow.Left = position.X;
                popUpWindow.Top = position.Y + 30;

                popUpWindow.StackPanel.Children.Clear();
                var results = LookUp(parsedWord);
                if (results == null)
                {
                    popUpWindow.Hide();
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

                    var textBlockId = new TextBlock
                    {
                        Name = "id",
                        Text = string.Join(",", result["id"]),
                        Visibility = Visibility.Collapsed
                    };
                    var textBlockDefinitions = new TextBlock
                    {
                        Name = "definitions",
                        Text = string.Join("", result["definitions"]),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = Brushes.White
                    };
                    var textBlockReadings = new TextBlock
                    {
                        Name = "readings",
                        Text = string.Join(",", result["readings"]),
                        Foreground = Brushes.White
                    };
                    var textBlockAlternativeSpellings = new TextBlock
                    {
                        Name = "alternativeSpellings",
                        Text = string.Join(",", result["alternativeSpellings"]),
                        Foreground = Brushes.White
                    };

                    stackPanel.Children.Add(textBlockFoundSpelling);
                    stackPanel.Children.Add(textBlockId);
                    stackPanel.Children.Add(textBlockDefinitions);
                    stackPanel.Children.Add(textBlockReadings);
                    stackPanel.Children.Add(textBlockAlternativeSpellings);

                    popUpWindow.StackPanel.Children.Add(stackPanel);
                    popUpWindow.StackPanel.Children.Add(new Separator());
                }

                popUpWindow.Show();
            }
            else
            {
                PopupWindow.Instance.Hide();
            }
        }

        // jmdictid or id
        // pick one
        private static void FoundSpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _miningMode = false;

            // doesn't work :(
            // string readings = stackPanel.FindName("readings").ToString();

            string foundSpelling = null;
            string readings = null;
            string definitions = null;
            string context = null;
            string definitionsRaw = null;
            string foundText = null;
            string jmdictID = null;

            var textBlock = (TextBlock) sender;
            var stackPanel = (StackPanel) textBlock.Parent;

            foreach (TextBlock child in stackPanel.Children)
            {
                switch (child.Name)
                {
                    case "foundSpelling":
                        foundSpelling = child.Text;
                        break;
                    case "id":
                        jmdictID = child.Text;
                        break;
                    case "definitions":
                        // definitions = html
                        definitionsRaw = child.Text;
                        break;
                    case "readings":
                        readings = child.Text;
                        break;
                    // case "alternativeSpellings":
                    //     alternativeSpellings = child.Text;
                    //     break;
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
                timeLocal
            );
        }

        private void MainTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void MainTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.M:
                {
                    _miningMode = true;
                    PopupWindow.Instance.Focus();

                    break;
                }
                case Key.C:
                {
                    var miningSetupWindow = new MiningSetupWindow();
                    miningSetupWindow.Show();

                    break;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            PopupWindow.Instance.Close();
        }

        private static List<Dictionary<string, List<string>>> LookUp(string parsedWord)
        {
            var results = new List<Dictionary<string, List<string>>>();

            if (!JMdictLoader.jMdictDictionary.TryGetValue(parsedWord, out List<Results> temp)) return null;

            foreach (var renamethis in temp)
            {
                var result = new Dictionary<string, List<string>>();

                var id = new List<string> {renamethis.Id};
                var definitions = renamethis.Definitions.Select(definition => definition + "\n").ToList();
                var readings = renamethis.Readings.ToList();
                var alternativeSpellings = renamethis.AlternativeSpellings.ToList();

                result.Add("id", id);
                result.Add("definitions", definitions);
                result.Add("readings", readings);
                result.Add("alternativeSpellings", alternativeSpellings);

                results.Add(result);
            }

            return results;
        }
    }
}