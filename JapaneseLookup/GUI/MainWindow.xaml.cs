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

        private readonly IParser _parser = new Mecab();

        private string _lastWord = "";

        internal static bool MiningMode = false;

        private static bool _ready = false;

        // Consider making max search length configurable.
        private static int _maxSearchLength = 15;

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        private static readonly List<string> japanesePunctuation = new(new string[]
            {"。", "！", "？", "…", "―", "\n"});

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
            if (MiningMode) return;

            int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
            if (charPosition != -1)
            {
                (string sentence, int endPosition) = FindSentence(MainTextBox.Text, charPosition);
                string parsedWord;
                if (endPosition - charPosition + 1 < _maxSearchLength)
                    parsedWord = _parser.Parse(MainTextBox.Text[charPosition..endPosition]);
                else
                    parsedWord = _parser.Parse(MainTextBox.Text[charPosition..(charPosition + _maxSearchLength - 1)]);

                // TODO: Lookafter and lookbehind.
                // TODO: Show results correctly.

                PopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

                if (parsedWord == _lastWord) return;
                _lastWord = parsedWord;

                PopupWindow.Display(parsedWord);
            }
            else
            {
                PopupWindow.Instance.Hide();
            }
        }

        private static (string sentence, int endPosition) FindSentence(string text, int position)
        {
            int startPosition = -1;
            int endPosition = -1;

            foreach (string punctuation in japanesePunctuation)
            {
                int tempStartIndex = text.Substring(0, position).LastIndexOf(punctuation);
                if (tempStartIndex != -1 && (endPosition == -1 || tempStartIndex > startPosition))
                    startPosition = tempStartIndex + 1;

                int tempEndIndex = text.IndexOf(punctuation, position);
                if (tempEndIndex != -1 && (endPosition == -1 || tempEndIndex < endPosition))
                    endPosition = tempEndIndex;
            }

            if (startPosition == -1)
                startPosition = 0;

            if (endPosition == -1)
                endPosition = text.Length - 1;

            // Consider trimming \t, \r, (, ), "　", " "
            return (text.Substring(startPosition, endPosition - startPosition + 1).Trim('「', '」', '『', '』', '（', '）', '\n'), endPosition);
            //text = text.Substring(startPosition, endPosition - startPosition + 1).TrimStart('「', '『', '（', '\n').TrimEnd('」', '』', '）', '\n');
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
                    MiningMode = true;
                    // TODO: Tell the user that they are in mining mode
                    PopupWindow.Instance.Focus();

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
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            PopupWindow.Instance.Close();
        }

        public static List<Dictionary<string, List<string>>> LookUp(string parsedWord)
        {
            var results = new List<Dictionary<string, List<string>>>();

            if (!JMdictLoader.jMdictDictionary.TryGetValue(parsedWord, out List<Results> jMDictResults)) return null;

            foreach (var jMDictResult in jMDictResults)
            {
                var result = new Dictionary<string, List<string>>();

                var jmdictID = new List<string> {jMDictResult.Id};
                var definitions = jMDictResult.Definitions.Select(definition => definition + "\n").ToList();
                var readings = jMDictResult.Readings.ToList();
                var alternativeSpellings = jMDictResult.AlternativeSpellings.ToList();

                // TODO: Config.FrequencyList instead of "VN"
                jMDictResult.FrequencyDict.TryGetValue("VN", out var freqList);
                var frequency = new List<string> {freqList?.FrequencyRank.ToString()};

                result.Add("readings", readings);
                result.Add("definitions", definitions);
                result.Add("jmdictID", jmdictID);
                result.Add("alternativeSpellings", alternativeSpellings);
                result.Add("frequency", frequency);

                // jMDictResult.FrequencyDict.TryGetValue("VN", out var freq1);
                // jMDictResult.FrequencyDict.TryGetValue("Novel", out var freq2);
                // jMDictResult.FrequencyDict.TryGetValue("Narou", out var freq3);
                // Debug.WriteLine(freq1?.FrequencyRank);

                results.Add(result);
            }

            return results;
        }
    }
}