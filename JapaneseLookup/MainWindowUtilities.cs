using JapaneseLookup.Anki;
using JapaneseLookup.Deconjugation;
using JapaneseLookup.EDICT;
using JapaneseLookup.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace JapaneseLookup
{
    internal static class MainWindowUtilities
    {
        public static bool ready = false;
        public static string Backlog = "";
        public const string FakeFrequency = "1000000";

        public static readonly Regex JapaneseRegex =
            new(@"[\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u4e00-\u9faf\u3400-\u4dbf]");

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        private static readonly List<string> JapanesePunctuation = new(new[]
            {"。", "！", "？", "…", "―", "\n"});

        public static void MainWindowInitializer()
        {
            Task<Dictionary<string, List<List<JsonElement>>>> taskFreqLoaderVN = Task.Run(() =>
                FrequencyLoader.LoadJSON(Path.Join(ConfigManager.ApplicationPath, "Resources/freqlist_vns.json")));
            Task<Dictionary<string, List<List<JsonElement>>>> taskFreqLoaderNovel = Task.Run(() =>
                FrequencyLoader.LoadJSON(Path.Join(ConfigManager.ApplicationPath, "Resources/freqlist_novels.json")));
            Task<Dictionary<string, List<List<JsonElement>>>> taskFreqLoaderNarou = Task.Run(() =>
                FrequencyLoader.LoadJSON(Path.Join(ConfigManager.ApplicationPath, "Resources/freqlist_narou.json")));
            Task.Run(JMdictLoader.Loader).ContinueWith(_ =>
            {
                //Task.WaitAll(taskFreqLoaderVN, taskFreqLoaderNovel, taskFreqLoaderNarou);
                FrequencyLoader.AddToJMdict("VN", taskFreqLoaderVN.Result);
                FrequencyLoader.AddToJMdict("Novel", taskFreqLoaderNovel.Result);
                FrequencyLoader.AddToJMdict("Narou", taskFreqLoaderNarou.Result);
                ready = true;
            });

            // init AnkiConnect so that it doesn't block later
            Task.Run(AnkiConnect.GetDeckNames);
        }

        public static (string sentence, int endPosition) FindSentence(string text, int position)
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

        public static List<Dictionary<string, List<string>>> LookUp(string text)
        {
            Dictionary<string, (List<Results> jMdictResults, List<string> processList, string foundForm)> llresults =
                new();
            int succAttempt = 0;
            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);

                bool tryLongVowelConversion = true;

                if (JMdictLoader.jMdictDictionary.TryGetValue(textInHiragana, out var tempResult))
                {
                    llresults.TryAdd(textInHiragana, (tempResult, new List<string>(), text[..^i]));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    var deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
                    foreach (var result in deconjugationResults)
                    {
                        if (llresults.ContainsKey(result.Text))
                            continue;

                        if (JMdictLoader.jMdictDictionary.TryGetValue(result.Text, out var temp))
                        {
                            List<Results> resultsList = new();

                            foreach (var rslt in temp)
                            {
                                if (rslt.WordClasses.SelectMany(pos => pos).Intersect(result.Tags).Any())
                                {
                                    resultsList.Add(rslt);
                                }
                            }

                            if (resultsList.Any())
                            {
                                llresults.Add(result.Text,
                                    (resultsList, result.Process, text[..result.OriginalText.Length]));
                                ++succAttempt;
                                tryLongVowelConversion = false;
                            }
                        }
                    }
                }

                if (tryLongVowelConversion && textInHiragana.Contains("ー") && textInHiragana[0] != 'ー')
                {
                    string textWithoutLongVowelMark = Kana.LongVowelMarkConverter(textInHiragana);
                    if (JMdictLoader.jMdictDictionary.TryGetValue(textWithoutLongVowelMark, out var tmpResult))
                    {
                        llresults.Add(textInHiragana, (tmpResult, new List<string>(), text[..^i]));
                    }
                }
            }

            if (!llresults.Any())
                return null;

            var results = ResultBuilder(llresults);

            results = results
                .OrderByDescending(dict => dict["foundForm"][0].Length)
                .ThenBy(dict => Convert.ToInt32(dict["frequency"][0])).ToList();
            return results;
        }

        private static List<Dictionary<string, List<string>>> ResultBuilder
            (Dictionary<string, (List<Results> jMdictResults, List<string> processList, string foundForm)> llresults)
        {
            var results = new List<Dictionary<string, List<string>>>();

            foreach (var rsts in llresults)
            {
                foreach (var jMDictResult in rsts.Value.jMdictResults)
                {
                    var result = new Dictionary<string, List<string>>();

                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };
                    var kanaSpellings = jMDictResult.KanaSpellings;
                    var readings = jMDictResult.Readings.ToList();
                    var definitions = new List<string> { DefinitionTextBuilder(jMDictResult) };
                    var foundForm = new List<string> { rsts.Value.foundForm };
                    var jmdictID = new List<string> { jMDictResult.Id };
                    var alternativeSpellings = jMDictResult.AlternativeSpellings.ToList();
                    var process = rsts.Value.processList;

                    jMDictResult.FrequencyDict.TryGetValue(ConfigManager.FrequencyList, out var freqList);
                    var maybeFreq = freqList?.FrequencyRank;
                    var frequency = new List<string> { maybeFreq == null ? FakeFrequency : maybeFreq.ToString() };

                    result.Add("foundSpelling", foundSpelling);
                    result.Add("kanaSpellings", kanaSpellings);
                    result.Add("readings", readings);
                    result.Add("definitions", definitions);
                    result.Add("foundForm", foundForm);
                    result.Add("jmdictID", jmdictID);
                    result.Add("alternativeSpellings", alternativeSpellings);
                    result.Add("process", process);
                    result.Add("frequency", frequency);

                    results.Add(result);
                }
            }

            return results;
        }

        private static string DefinitionTextBuilder(Results jMDictResult)
        {
            int count = 1;
            string defResult = "";
            for (int i = 0; i < jMDictResult.DefinitionsList.Count; i++)
            {
                if (jMDictResult.WordClasses[i].Any())
                {
                    defResult += "(";
                    defResult += string.Join(", ", jMDictResult.WordClasses[i]);
                    defResult += ") ";
                }

                if (jMDictResult.DefinitionsList.Any())
                {
                    defResult += "(" + count + ") ";

                    if (jMDictResult.SpellingInfo[i] != null)
                    {
                        defResult += "(";
                        defResult += jMDictResult.SpellingInfo[i];
                        defResult += ") ";
                    }

                    if (jMDictResult.MiscList[i].Any())
                    {
                        defResult += "(";
                        defResult += string.Join(", ", jMDictResult.MiscList[i]);
                        defResult += ") ";
                    }

                    defResult += string.Join("; ", jMDictResult.DefinitionsList[i].Definitions) + " ";

                    if (jMDictResult.DefinitionsList[i].RRestrictions.Any() ||
                        jMDictResult.DefinitionsList[i].KRestrictions.Any())
                    {
                        defResult += "(only applies to ";

                        if (jMDictResult.DefinitionsList[i].KRestrictions.Any())
                            defResult += string.Join("; ", jMDictResult.DefinitionsList[i].KRestrictions);

                        if (jMDictResult.DefinitionsList[i].RRestrictions.Any())
                            defResult += string.Join("; ", jMDictResult.DefinitionsList[i].RRestrictions);

                        defResult += ") ";
                    }

                    //defResult += "\n";
                    ++count;
                }
            }

            return defResult;
        }
    }
}