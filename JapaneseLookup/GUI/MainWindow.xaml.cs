using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JapaneseLookup.Anki;
using JapaneseLookup.Deconjugation;
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
            Task.Run(AnkiConnect.GetDeckNames);
            // Mining.Mine(null, null, null, null);

            CopyFromClipboard();

            AnkiConnect.GetAudio("猫", "ねこ");
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

            PopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

            int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
            if (charPosition != -1)
            {
                (string sentence, int endPosition) = FindSentence(MainTextBox.Text, charPosition);
                string text;
                if (endPosition - charPosition + 1 < MaxSearchLength)
                    text = MainTextBox.Text[charPosition..(endPosition + 1)];
                else
                    text = MainTextBox.Text[charPosition..(charPosition + MaxSearchLength)];

                if (text == _lastWord) return;
                _lastWord = text;

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
            {
                _lastWord = "";
                PopupWindow.Instance.Hide();
            }
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

        public static List<Dictionary<string, List<string>>> LookUp(string text)
        {
            Dictionary<string, (List<Results> jMdictResults, List<string> processList, string foundForm)> llresults =
                new();
            int succAttempt = 0;
            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);
                Debug.WriteLine(textInHiragana);
                Debug.WriteLine(Kana.LongVowelMarkConverter(textInHiragana));

                bool tryLongVowelConversion = true;

                if (JMdictLoader.jMdictDictionary.TryGetValue(textInHiragana, out var tempResult))
                {
                    llresults.TryAdd(text[..^i], (tempResult, new List<string>(), text[..^i]));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    var deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
                    foreach (var result in deconjugationResults)
                    {
                        if (JMdictLoader.jMdictDictionary.TryGetValue(result.Text, out var temp))
                        {
                            List<Results> resultsList = new();

                            foreach (var rslt in temp)
                            {
                                if (rslt.WordClasses.Intersect(result.Tags).Any())
                                {
                                    resultsList.Add(rslt);
                                }
                            }

                            if (resultsList.Any())
                            {
                                llresults.TryAdd(result.Text,
                                    (resultsList, result.Process, text[..result.OriginalText.Length]));
                                ++succAttempt;
                                tryLongVowelConversion = false;
                            }
                        }
                    }
                }

                if (tryLongVowelConversion)
                {
                    if (textInHiragana[0] != 'ー' && textInHiragana.Contains("ー"))
                    {
                        string textWithoutLongVowelMark = Kana.LongVowelMarkConverter(textInHiragana);
                        if (JMdictLoader.jMdictDictionary.TryGetValue(textWithoutLongVowelMark, out var tmpResult))
                        {
                            llresults.TryAdd(text[..^i], (tmpResult, new List<string>(), text[..^i]));
                        }
                    }
                }
            }

            if (!llresults.Any())
                return null;

            var results = new List<Dictionary<string, List<string>>>();

            foreach (var rsts in llresults)
            {
                foreach (var jMDictResult in rsts.Value.jMdictResults)
                {
                    var result = new Dictionary<string, List<string>>();

                    var foundSpelling = new List<string> {rsts.Key};
                    result.Add("foundSpelling", foundSpelling);

                    result.Add("process", rsts.Value.processList);

                    var foundForm = new List<string> {rsts.Value.foundForm};
                    result.Add("foundForm", foundForm);

                    result.Add("foundText", foundSpelling);

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
                .OrderByDescending(dict => dict["foundForm"][0].Length)
                .ThenBy(dict => Convert.ToInt32(dict["frequency"][0])).ToList();
            return results;
        }
    }
}