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
    public static class MainWindowUtilities
    {
        public static readonly List<string> Backlog = new();
        public const string FakeFrequency = "1000000";
        private static DateTime _lastLookupTime;

        public static readonly Regex JapaneseRegex =
            new(@"[\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u4e00-\u9faf\u3400-\u4dbf]");

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        private static readonly List<string> JapanesePunctuation =
            new() { "。", "！", "？", "…", "―", "\n" };

        public static void MainWindowInitializer()
        {
            // init AnkiConnect so that it doesn't block later
            // Task.Run(AnkiConnect.GetDeckNames);
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
            var preciseTimeNow = new DateTime(Stopwatch.GetTimestamp());
            if ((preciseTimeNow - _lastLookupTime).Milliseconds < ConfigManager.LookupRate)
                return null;

            _lastLookupTime = preciseTimeNow;

            Dictionary<string, (List<JMdictResult> jMdictResults, List<string> processList, string foundForm)>
                wordResults = new();
            Dictionary<string, (List<JMnedictResult> jMdictResults, List<string> processList, string foundForm)>
                nameResults = new();

            int succAttempt = 0;
            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);

                bool tryLongVowelConversion = true;

                if (JMdictLoader.jMdictDictionary.TryGetValue(textInHiragana, out var tempResult))
                {
                    wordResults.TryAdd(textInHiragana, (tempResult, new List<string>(), text[..^i]));
                    tryLongVowelConversion = false;
                }

                if (ConfigManager.UseJMnedict)
                {
                    if (JMnedictLoader.jMnedictDictionary.TryGetValue(textInHiragana, out var tempNameResult))
                    {
                        nameResults.TryAdd(textInHiragana, (tempNameResult, new List<string>(), text[..^i]));
                    }
                }


                if (succAttempt < 3)
                {
                    var deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
                    foreach (var result in deconjugationResults)
                    {
                        if (wordResults.ContainsKey(result.Text))
                            continue;

                        if (JMdictLoader.jMdictDictionary.TryGetValue(result.Text, out var temp))
                        {
                            List<JMdictResult> resultsList = new();

                            foreach (var rslt in temp)
                            {
                                if (rslt.WordClasses.SelectMany(pos => pos).Intersect(result.Tags).Any())
                                {
                                    resultsList.Add(rslt);
                                }
                            }

                            if (resultsList.Any())
                            {
                                wordResults.Add(result.Text,
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
                        wordResults.Add(textInHiragana, (tmpResult, new List<string>(), text[..^i]));
                    }
                }
            }

            if (!wordResults.Any() && !nameResults.Any())
                return null;

            List<Dictionary<string, List<string>>> results = new();

            if (wordResults.Any())
                results.AddRange(WordResultBuilder(wordResults));

            if (nameResults.Any())
                results.AddRange(NameResultBuilder(nameResults));

            results = results
                .OrderByDescending(dict => dict["foundForm"][0].Length)
                .ThenBy(dict => Convert.ToInt32(dict["frequency"][0])).ToList();
            return results;
        }

        private static List<Dictionary<string, List<string>>> NameResultBuilder
            (Dictionary<string, (List<JMnedictResult> jMdictResults, List<string> processList, string foundForm)> nameResult)
        {
            var results = new List<Dictionary<string, List<string>>>();

            foreach (var wordResult in nameResult)
            {
                foreach (var jMDictResult in wordResult.Value.jMdictResults)
                {
                    var result = new Dictionary<string, List<string>>();

                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };
                    List<string> readings;

                    if (jMDictResult.Readings != null)
                        readings = jMDictResult.Readings.ToList();
                    else
                        readings = new List<string>();

                    var foundForm = new List<string> { wordResult.Value.foundForm };
                    var jmdictID = new List<string> { jMDictResult.Id };


                    List<string> alternativeSpellings;
                    if (jMDictResult.AlternativeSpellings != null)
                        alternativeSpellings = jMDictResult.AlternativeSpellings;
                    else
                        alternativeSpellings = new List<string>();

                    var process = wordResult.Value.processList;

                    var definitions = new List<string> { BuildNameDefinition(jMDictResult) };

                    result.Add("jmdictID", jmdictID);
                    result.Add("foundSpelling", foundSpelling);
                    result.Add("alternativeSpellings", alternativeSpellings);
                    result.Add("readings", readings);
                    result.Add("definitions", definitions);

                    // unused here but necessary for DisplayResults
                    result.Add("foundForm", foundForm);
                    result.Add("process", process);
                    result.Add("frequency", new List<string> { FakeFrequency });
                    result.Add("kanaSpellings", new List<string>());
                    result.Add("pOrthographyInfoList", new List<string>());
                    result.Add("aOrthographyInfoList", new List<string>());
                    result.Add("rOrthographyInfoList", new List<string>());

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<string, List<string>>> WordResultBuilder
            (Dictionary<string, (List<JMdictResult> jMdictResults, List<string> processList, string foundForm)> wordResults)
        {
            var results = new List<Dictionary<string, List<string>>>();

            foreach (var wordResult in wordResults)
            {
                foreach (var jMDictResult in wordResult.Value.jMdictResults)
                {
                    var result = new Dictionary<string, List<string>>();

                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };

                    List<string> kanaSpellings;
                    if (jMDictResult.KanaSpellings != null)
                        kanaSpellings = jMDictResult.KanaSpellings;
                    else
                        kanaSpellings = new List<string>();

                    var readings = jMDictResult.Readings.ToList();
                    var foundForm = new List<string> { wordResult.Value.foundForm };
                    var jmdictID = new List<string> { jMDictResult.Id };

                    List<string> alternativeSpellings;
                    if (jMDictResult.AlternativeSpellings != null)
                        alternativeSpellings = jMDictResult.AlternativeSpellings.ToList();
                    else
                        alternativeSpellings = new List<string>();
                    var process = wordResult.Value.processList;

                    List<string> frequency;
                    if (jMDictResult.FrequencyDict != null)
                    {
                        jMDictResult.FrequencyDict.TryGetValue(ConfigManager.FrequencyList, out var freq);
                        frequency = new List<string> { freq.ToString() };
                    }

                    else frequency = new List<string> { FakeFrequency };

                    var definitions = new List<string> { BuildWordDefinition(jMDictResult) };

                    var pOrthographyInfoList = jMDictResult.POrthographyInfoList ?? new List<string>();

                    var rList = jMDictResult.ROrthographyInfoList ?? new List<List<string>>();
                    var aList = jMDictResult.AOrthographyInfoList ?? new List<List<string>>();
                    var rOrthographyInfoList = new List<string>();
                    var aOrthographyInfoList = new List<string>();

                    foreach (var list in rList)
                    {
                        var final = "";
                        foreach (var str in list)
                        {
                            final += str + ", ";
                        }

                        final = final.TrimEnd(", ".ToCharArray());

                        rOrthographyInfoList.Add(final);
                    }

                    foreach (var list in aList)
                    {
                        var final = "";
                        foreach (var str in list)
                        {
                            final += str + ", ";
                        }

                        final = final.TrimEnd(", ".ToCharArray());

                        aOrthographyInfoList.Add(final);
                    }

                    result.Add("foundSpelling", foundSpelling);
                    result.Add("kanaSpellings", kanaSpellings);
                    result.Add("readings", readings);
                    result.Add("definitions", definitions);
                    result.Add("foundForm", foundForm);
                    result.Add("jmdictID", jmdictID);
                    result.Add("alternativeSpellings", alternativeSpellings);
                    result.Add("process", process);
                    result.Add("frequency", frequency);
                    result.Add("pOrthographyInfoList", pOrthographyInfoList);
                    result.Add("rOrthographyInfoList", rOrthographyInfoList);
                    result.Add("aOrthographyInfoList", aOrthographyInfoList);

                    results.Add(result);
                }
            }

            return results;
        }

        private static string BuildNameDefinition(JMnedictResult jMDictResult)
        {
            int count = 1;
            string defResult = "";

            if (jMDictResult.NameTypes != null &&
                (jMDictResult.NameTypes.Count > 1 || !jMDictResult.NameTypes.Contains("unclass")))
            {
                foreach (var nameType in jMDictResult.NameTypes)
                {
                    defResult += "(";
                    defResult += nameType;
                    defResult += ") ";
                }
            }

            for (int i = 0; i < jMDictResult.Definitions.Count; i++)
            {
                if (jMDictResult.Definitions.Any())
                {
                    if (jMDictResult.Definitions.Count > 0)
                        defResult += "(" + count + ") ";

                    defResult += string.Join("; ", jMDictResult.Definitions[i]) + " ";
                    ++count;
                }
            }

            return defResult;
        }

        private static string BuildWordDefinition(JMdictResult jMDictResult)
        {
            int count = 1;
            string defResult = "";
            for (int i = 0; i < jMDictResult.Definitions.Count; i++)
            {
                if (jMDictResult.WordClasses.Any() && jMDictResult.WordClasses[i].Any())
                {
                    defResult += "(";
                    defResult += string.Join(", ", jMDictResult.WordClasses[i]);
                    defResult += ") ";
                }

                if (jMDictResult.Definitions.Any())
                {
                    defResult += "(" + count + ") ";

                    if (jMDictResult.SpellingInfo.Any() && jMDictResult.SpellingInfo[i] != null)
                    {
                        defResult += "(";
                        defResult += jMDictResult.SpellingInfo[i];
                        defResult += ") ";
                    }

                    if (jMDictResult.MiscList.Any() && jMDictResult.MiscList[i].Any())
                    {
                        defResult += "(";
                        defResult += string.Join(", ", jMDictResult.MiscList[i]);
                        defResult += ") ";
                    }

                    defResult += string.Join("; ", jMDictResult.Definitions[i]) + " ";

                    if (jMDictResult.RRestrictions != null && jMDictResult.RRestrictions[i].Any()
                        || jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                    {
                        defResult += "(only applies to ";

                        if (jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                            defResult += string.Join("; ", jMDictResult.KRestrictions[i]);

                        if (jMDictResult.RRestrictions != null && jMDictResult.RRestrictions[i].Any())
                            defResult += string.Join("; ", jMDictResult.RRestrictions[i]);

                        defResult += ") ";
                    }

                    ++count;
                }
            }

            return defResult;
        }
    }
}