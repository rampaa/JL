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

        // private string _lastWord = "";

        internal static bool MiningMode = false;

        private static bool _isEverythingReady = false;

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
                _isEverythingReady = true;
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
                string parsedWord = _parser.Parse(MainTextBox.Text[charPosition..]);
                // if (parsedWord == _lastWord) return;
                // _lastWord = parsedWord;

                // TODO: Lookafter and lookbehind.
                // TODO: Show results correctly.

                Point position = PointToScreen(Mouse.GetPosition(this));
                PopupWindow.Display(position, parsedWord);
            }
            else
            {
                PopupWindow.Instance.Hide();
            }
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

                // if (_isEverythingReady)
                // {
                //     jMDictResult.FrequencyDict.TryGetValue("VN", out var freq1);
                //     jMDictResult.FrequencyDict.TryGetValue("Novel", out var freq2);
                //     jMDictResult.FrequencyDict.TryGetValue("Narou", out var freq3);
                //     Debug.WriteLine(freq1?.FrequencyRank + "\n" + freq2?.FrequencyRank + "\n" + freq3?.FrequencyRank);
                // }

                results.Add(result);
            }

            return results;
        }
    }
}