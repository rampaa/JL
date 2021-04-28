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
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

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

        private string _lastWord = "";

        internal static bool MiningMode = false;

        private static bool _ready = false;

        // Consider making max search length configurable.
        private const int MaxSearchLength = 37;

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        private static readonly List<string> JapanesePunctuation = new(new[]
            {"。", "！", "？", "…", "―", "\n"});

        internal static string LastSentence;

        internal const string FakeFrequency = "1000000";

        public MainWindow()
        {
            InitializeComponent();

            Task<Dictionary<string, List<List<JsonElement>>>> taskFreqLoaderVN = Task.Run(() =>
                FrequencyLoader.LoadJSON("../net5.0-windows/Resources/freqlist_vns.json"));
            Task<Dictionary<string, List<List<JsonElement>>>> taskFreqLoaderNovel = Task.Run(() =>
                FrequencyLoader.LoadJSON("../net5.0-windows/Resources/freqlist_novels.json"));
            Task<Dictionary<string, List<List<JsonElement>>>> taskFreqLoaderNarou = Task.Run(() =>
                FrequencyLoader.LoadJSON("../net5.0-windows/Resources/freqlist_narou.json"));
            Task.Run(JMdictLoader.Loader).ContinueWith(_ =>
            {
                //Task.WaitAll(taskFreqLoaderVN, taskFreqLoaderNovel, taskFreqLoaderNarou);
                FrequencyLoader.AddToJMdict("VN", taskFreqLoaderVN.Result);
                FrequencyLoader.AddToJMdict("Novel", taskFreqLoaderNovel.Result);
                FrequencyLoader.AddToJMdict("Narou", taskFreqLoaderNarou.Result);
                _ready = true;
            });

            // init AnkiConnect so that it doesn't block later
            // Task.Run(AnkiConnect.GetDeckNames);
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
                        MainTextBox.Text = text;
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
            if (MiningMode) return;

            int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
            if (charPosition != -1)
            {
                (string sentence, int endPosition) = FindSentence(MainTextBox.Text, charPosition);
                string text;
                if (endPosition - charPosition + 1 < MaxSearchLength)
                    text = MainTextBox.Text[charPosition..endPosition];
                else
                    text = MainTextBox.Text[charPosition..(charPosition + MaxSearchLength)];

                // if (parsedWord == _lastWord) return;
                // _lastWord = parsedWord;

                var results = LookUp(text);

                if (results != null)
                {
                    PopupWindow.Instance.StackPanel.Children.Clear();
                    PopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

                    PopupWindow.Instance.Show();
                    PopupWindow.Instance.Activate();
                    PopupWindow.Instance.Focus();
                    PopupWindow.DisplayResults(sentence, results);
                }

                else
                    PopupWindow.Instance.Hide();
            }
            else
                PopupWindow.Instance.Hide();
        }

        private static (string sentence, int endPosition) FindSentence(string text, int position)
        {
            int startPosition = -1;
            int endPosition = -1;

            foreach (string punctuation in JapanesePunctuation)
            {
                int tempIndex = text.LastIndexOf(punctuation, position, StringComparison.Ordinal);

                if (tempIndex > startPosition)
                    startPosition = tempIndex;

                tempIndex = text.IndexOf(punctuation, position, StringComparison.Ordinal);
                if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                    endPosition = tempIndex;
            }

            ++startPosition;

            if (endPosition == -1)
                endPosition = text.Length - 1;

            // Consider trimming \t, \r, (, ), "　", " "
            return (
                text.Substring(startPosition, endPosition - startPosition + 1)
                    .Trim('「', '」', '『', '』', '（', '）', '\n'),
                endPosition
            );
            //text = text.Substring(startPosition, endPosition - startPosition + 1).TrimStart('「', '『', '（', '\n').TrimEnd('」', '』', '）', '\n');
        }

        private void MainTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            PopupWindow.Instance.Close();
        }

        private void MainTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!MiningMode)
                PopupWindow.Instance.Hide();
        }

        public List<Dictionary<string, List<string>>> LookUp(string text)
        {
            Dictionary<string, Tuple<List<Results>, List<string>>> llresults = new();

            for (int i = 0; i < text.Length; i++)
            {
                string foundText = text[..^i];

                // if (_lastWord == foundText) return null;
                // _lastWord = foundText;
                var deconjugationResults = Deconjugation.Deconjugate(foundText);

                foreach (var result in deconjugationResults)
                {
                    if (JMdictLoader.jMdictDictionary.TryGetValue(result.Text, out var temp))
                    {
                        llresults.TryAdd(result.Text, Tuple.Create(temp, result.Process));
                    }
                }

                if (JMdictLoader.jMdictDictionary.TryGetValue(foundText, out var tempResult))
                {
                    llresults.TryAdd(foundText, Tuple.Create(tempResult, new List<string>()));
                }
            }

            if (!llresults.Any())
                return null;

            var results = new List<Dictionary<string, List<string>>>();

            foreach (var rsts in llresults)
            {
                foreach (var jMDictResult in rsts.Value.Item1)
                {
                    var result = new Dictionary<string, List<string>>();

                    List<string> foundSpelling = new();
                    foundSpelling.Add(rsts.Key);
                    result.Add("foundSpelling", foundSpelling);
                    result.Add("process", rsts.Value.Item2);

                    var jmdictID = new List<string> {jMDictResult.Id};
                    var definitions = jMDictResult.Definitions.Select(definition => definition + "\n").ToList();
                    var readings = jMDictResult.Readings.ToList();
                    var alternativeSpellings = jMDictResult.AlternativeSpellings.ToList();

                    // TODO: Config.FrequencyList instead of "VN"
                    jMDictResult.FrequencyDict.TryGetValue("VN", out var freqList);

                    // causes OrderBy to put null values first :(
                    // var frequency = new List<string> {freqList?.FrequencyRank.ToString()};
                    var maybeFreq = freqList?.FrequencyRank;
                    var frequency = new List<string> {maybeFreq == null ? FakeFrequency : maybeFreq.ToString()};

                    result.Add("readings", readings);
                    result.Add("definitions", definitions);
                    result.Add("jmdictID", jmdictID);
                    result.Add("alternativeSpellings", alternativeSpellings);
                    result.Add("frequency", frequency);

                    results.Add(result);
                }
            }

            results = results
                .OrderByDescending(dict => dict["foundSpelling"][0].Length)
                .ThenBy(dict => Convert.ToInt32(dict["frequency"][0])).ToList();
            return results;
        }
    }
}