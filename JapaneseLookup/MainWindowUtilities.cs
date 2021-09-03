using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using JapaneseLookup.Deconjugation;
using JapaneseLookup.EDICT;
using JapaneseLookup.GUI;
using JapaneseLookup.KANJIDIC;
using JapaneseLookup.EPWING;

namespace JapaneseLookup
{
    public static class MainWindowUtilities
    {
        public static readonly List<string> Backlog = new();
        public const string FakeFrequency = "1000000";
        private static DateTime _lastLookupTime;

        public static readonly Regex JapaneseRegex =
            new(
                @"[\u2e80-\u2eff\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\u31c0-\u31ef\u31f0-\u31ff\u3200-\u32ff\u3300-\u33ff\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\ufe30-\ufe4f\uff00-\uffef]|[\ud82c-\ud82c][\udc00-\udcff]|[\ud840-\ud869][\udc00-\udedf]|[\ud869-\ud86d][\udf00-\udf3f]|[\ud86e-\ud873][\udc20-\udeaf]|[\ud873-\ud87a][\udeb0-\udfef]|[\ud87e-\ude1f][\udc00-\ude1f]|[\ud880-\ud884][\udc00-\udf4f]");

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        private static readonly List<string> JapanesePunctuation =
            new() { "。", "！", "？", "…", "―", ".", "＆", "、", "「", "」", "『", "』", "（", "）", "\n" };

        public static void MainWindowInitializer()
        {
            // init AnkiConnect so that it doesn't block later
            // Task.Run(AnkiConnect.GetDeckNames);
        }

        public static int FindWordBoundary(string text, int position)
        {
            int endPosition = -1;

            foreach (string punctuation in JapanesePunctuation)
            {
                int tempIndex = text.IndexOf(punctuation, position, StringComparison.Ordinal);

                if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                    endPosition = tempIndex;
            }

            if (endPosition == -1)
                endPosition = text.Length;

            return endPosition;
        }

        public static List<Dictionary<LookupResult, List<string>>> Lookup(string text)
        {
            var preciseTimeNow = new DateTime(Stopwatch.GetTimestamp());
            if ((preciseTimeNow - _lastLookupTime).Milliseconds < ConfigManager.LookupRate) return null;
            _lastLookupTime = preciseTimeNow;

            var wordResults =
                new Dictionary<string,
                    (List<JMdictResult> jMdictResults, List<string> processList, string foundForm)>();
            var nameResults =
                new Dictionary<string,
                    (List<JMnedictResult> jMnedictResults, List<string> processList, string foundForm)>();
            var epwingWordResults =
                new List<Dictionary<string,
                    (List<EpwingResult> epwingResults, List<string> processList, string foundForm)>>();
            var kanjiResult =
                new Dictionary<string,
                    (List<List<KanjiResult>>, List<string>, string)>();

            if (ConfigManager.KanjiMode)
                if (Dicts.dicts[DictType.Kanjidic]?.Contents.Any() ?? false)
                {
                    return KanjiResultBuilder(GetKanjidicResults(text));
                }

            foreach ((DictType dictType, Dict dict) in Dicts.dicts)
            {
                switch (dictType)
                {
                    case DictType.JMdict:
                        wordResults = GetJMdictResults(text);
                        break;
                    case DictType.JMnedict:
                        nameResults = GetJMnedictResults(text);
                        break;
                    case DictType.Kanjidic:
                        // handled above and below
                        break;
                    case DictType.UnknownEpwing:
                        // TODO
                        epwingWordResults.Add(GetDaijirinResults(text, dict.Contents));
                        break;
                    case DictType.Daijirin:
                        epwingWordResults.Add(GetDaijirinResults(text, dict.Contents));
                        break;
                    case DictType.Daijisen:
                        // TODO
                        epwingWordResults.Add(GetDaijirinResults(text, dict.Contents));
                        break;
                    case DictType.Kojien:
                        // TODO
                        epwingWordResults.Add(GetDaijirinResults(text, dict.Contents));
                        break;
                    case DictType.Meikyou:
                        // TODO
                        epwingWordResults.Add(GetDaijirinResults(text, dict.Contents));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (!wordResults.Any() && !nameResults.Any() && (!epwingWordResults.First()?.Any() ?? false))
            {
                if (Dicts.dicts[DictType.Kanjidic]?.Contents.Any() ?? false)
                {
                    kanjiResult = GetKanjidicResults(text);
                }
            }

            List<Dictionary<LookupResult, List<string>>> lookupResults = new();

            if (wordResults.Any())
                lookupResults.AddRange(WordResultBuilder(wordResults));

            if (epwingWordResults.Any())
                foreach (var epwingWordResult in epwingWordResults)
                {
                    lookupResults.AddRange(EpwingWordResultBuilder(epwingWordResult));
                }

            if (nameResults.Any())
                lookupResults.AddRange(NameResultBuilder(nameResults));

            if (kanjiResult.Any())
                lookupResults.AddRange(KanjiResultBuilder(kanjiResult));

            lookupResults = lookupResults
                .OrderByDescending(dict => dict[LookupResult.FoundForm][0].Length)
                .ThenBy(dict => Convert.ToInt32(dict[LookupResult.Frequency][0])).ToList();
            return lookupResults;
        }

        private static Dictionary<string, (List<JMdictResult> jMdictResults, List<string> Process, string)>
            GetJMdictResults(string text)
        {
            var wordResults =
                new Dictionary<string,
                    (List<JMdictResult> resultsList, List<string> Process, string)>();

            int succAttempt = 0;
            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);

                bool tryLongVowelConversion = true;

                if (Dicts.dicts[DictType.JMdict].Contents.TryGetValue(textInHiragana, out var tempResult1))
                {
                    var tempResult = tempResult1.Cast<JMdictResult>().ToList();
                    wordResults.TryAdd(textInHiragana, (tempResult, new List<string>(), text[..^i]));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    var deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
                    foreach (var result in deconjugationResults)
                    {
                        if (wordResults.ContainsKey(result.Text))
                            continue;

                        if (Dicts.dicts[DictType.JMdict].Contents.TryGetValue(result.Text, out var temp))
                        {
                            List<JMdictResult> resultsList = new();

                            foreach (var rslt1 in temp)
                            {
                                var rslt = (JMdictResult) rslt1;
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
                    if (Dicts.dicts[DictType.JMdict].Contents.TryGetValue(textWithoutLongVowelMark, out var tmpResult1))
                    {
                        var tmpResult = tmpResult1.Cast<JMdictResult>().ToList();
                        wordResults.Add(textInHiragana, (tmpResult, new List<string>(), text[..^i]));
                    }
                }
            }

            return wordResults;
        }

        private static Dictionary<string,
                (List<JMnedictResult> jMnedictResults, List<string> processList, string foundForm)>
            GetJMnedictResults(string text)
        {
            var nameResults =
                new Dictionary<string,
                    (List<JMnedictResult> jMnedictResults, List<string> processList, string foundForm)>();

            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);

                if (Dicts.dicts[DictType.JMnedict].Contents.TryGetValue(textInHiragana, out var tempNameResult1))
                {
                    var tempNameResult = tempNameResult1.Cast<JMnedictResult>().ToList();
                    nameResults.TryAdd(textInHiragana, (tempNameResult, new List<string>(), text[..^i]));
                }
            }

            return nameResults;
        }

        private static Dictionary<string, (List<List<KanjiResult>>, List<string>, string)> GetKanjidicResults(
            string text)
        {
            var kanjiResult =
                new Dictionary<string,
                    (List<List<KanjiResult>>, List<string>, string)>();

            if (Dicts.dicts[DictType.Kanjidic].Contents.TryGetValue(
                text.UnicodeIterator().DefaultIfEmpty(string.Empty).First(), out List<IResult> kResult1))
            {
                var kResult = kResult1.Cast<KanjiResult>().ToList();
                kanjiResult.Add(text.UnicodeIterator().First(),
                    (new List<List<KanjiResult>> { kResult }, new List<string>(), text.UnicodeIterator().First()));
            }

            return kanjiResult;
        }

        private static Dictionary<string,
                (List<EpwingResult> daijirinResults, List<string> processList, string foundForm)>
            GetDaijirinResults(string text, Dictionary<string, List<IResult>> dict)
        {
            var daijirinWordResults =
                new Dictionary<string,
                    (List<EpwingResult> daijirinResults, List<string> processList, string foundForm)>();

            int succAttempt = 0;
            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);

                bool tryLongVowelConversion = true;

                if (dict.TryGetValue(textInHiragana, out var hiraganaTempResult1))
                {
                    var hiraganaTempResult = hiraganaTempResult1.Cast<EpwingResult>().ToList();
                    daijirinWordResults.TryAdd(textInHiragana, (hiraganaTempResult, new List<string>(), text[..^i]));
                    tryLongVowelConversion = false;
                }

                //todo
                if (dict.TryGetValue(text, out var textTempResult1))
                {
                    var textTempResult = textTempResult1.Cast<EpwingResult>().ToList();
                    daijirinWordResults.TryAdd(text, (textTempResult, new List<string>(), text[..^i]));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    var deconjugationResults = Deconjugator.Deconjugate(textInHiragana);
                    foreach (var result in deconjugationResults)
                    {
                        if (daijirinWordResults.ContainsKey(result.Text))
                            continue;

                        if (dict.TryGetValue(result.Text, out var temp))
                        {
                            List<EpwingResult> resultsList = new();

                            foreach (var rslt1 in temp)
                            {
                                var rslt = (EpwingResult) rslt1;
                                // if (rslt.WordClasses.SelectMany(pos => pos).Intersect(result.Tags).Any())
                                // {
                                resultsList.Add(rslt);
                                // }
                            }

                            if (resultsList.Any())
                            {
                                daijirinWordResults.Add(result.Text,
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
                    if (dict.TryGetValue(textWithoutLongVowelMark, out var tmpResult1))
                    {
                        var tmpResult = tmpResult1.Cast<EpwingResult>().ToList();
                        daijirinWordResults.Add(textInHiragana, (tmpResult, new List<string>(), text[..^i]));
                    }
                }
            }

            return daijirinWordResults;
        }

        private static List<Dictionary<LookupResult, List<string>>> KanjiResultBuilder
            (Dictionary<string, (List<List<KanjiResult>> kanjiResult, List<string> processList, string foundForm)> kanjiResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();
            var result = new Dictionary<LookupResult, List<string>>();

            // peak code
            KanjiResult kanjiResult = kanjiResults.First().Value.kanjiResult.First().First();

            result.Add(LookupResult.FoundSpelling, new List<string> { kanjiResults.First().Key });
            result.Add(LookupResult.Definitions, kanjiResult.Meanings);
            result.Add(LookupResult.OnReadings, kanjiResult.OnReadings);
            result.Add(LookupResult.KunReadings, kanjiResult.KunReadings);
            result.Add(LookupResult.Nanori, kanjiResult.Nanori);
            result.Add(LookupResult.StrokeCount, new List<string> { kanjiResult.StrokeCount.ToString() });
            result.Add(LookupResult.Grade, new List<string> { kanjiResult.Grade.ToString() });
            result.Add(LookupResult.Composition, new List<string> { kanjiResult.Composition });
            result.Add(LookupResult.Frequency, new List<string> { kanjiResult.Frequency.ToString() });

            var foundForm = new List<string> { kanjiResults.First().Value.foundForm };
            result.Add(LookupResult.FoundForm, foundForm);

            results.Add(result);
            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> NameResultBuilder
            (Dictionary<string, (List<JMnedictResult> jMdictResults, List<string> processList, string foundForm)> nameResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (var nameResult in nameResults)
            {
                foreach (var jMDictResult in nameResult.Value.jMdictResults)
                {
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };

                    var readings = jMDictResult.Readings != null ? jMDictResult.Readings.ToList() : new List<string>();

                    var foundForm = new List<string> { nameResult.Value.foundForm };

                    var edictID = new List<string> { jMDictResult.Id };

                    var alternativeSpellings = jMDictResult.AlternativeSpellings ?? new List<string>();

                    var definitions = new List<string> { BuildNameDefinition(jMDictResult) };

                    result.Add(LookupResult.EdictID, edictID);
                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);

                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Frequency, new List<string> { FakeFrequency });

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> WordResultBuilder
            (Dictionary<string, (List<JMdictResult> jMdictResults, List<string> processList, string foundForm)> wordResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (var wordResult in wordResults)
            {
                foreach (var jMDictResult in wordResult.Value.jMdictResults)
                {
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };

                    var kanaSpellings = jMDictResult.KanaSpellings ?? new List<string>();

                    var readings = jMDictResult.Readings.ToList();
                    var foundForm = new List<string> { wordResult.Value.foundForm };
                    var edictID = new List<string> { jMDictResult.Id };

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

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.KanaSpellings, kanaSpellings);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.EdictID, edictID);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);
                    result.Add(LookupResult.POrthographyInfoList, pOrthographyInfoList);
                    result.Add(LookupResult.ROrthographyInfoList, rOrthographyInfoList);
                    result.Add(LookupResult.AOrthographyInfoList, aOrthographyInfoList);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> EpwingWordResultBuilder
            (Dictionary<string, (List<EpwingResult> epwingResults, List<string> processList, string foundForm)> wordResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (var wordResult in wordResults)
            {
                foreach (var jMDictResult in wordResult.Value.epwingResults)
                {
                    var result = new Dictionary<LookupResult, List<string>>();
                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };
                    var readings = jMDictResult.Readings.ToList();
                    var foundForm = new List<string> { wordResult.Value.foundForm };
                    var process = wordResult.Value.processList;
                    List<string> frequency;
                    // if (jMDictResult.FrequencyDict != null)
                    // {
                    //     jMDictResult.FrequencyDict.TryGetValue(ConfigManager.FrequencyList, out var freq);
                    //     frequency = new List<string> { freq.ToString() };
                    // }
                    //
                    // else frequency = new List<string> { FakeFrequency };
                    frequency = new List<string> { FakeFrequency };

                    var definitions = new List<string> { BuildEpwingWordDefinition(jMDictResult) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);

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

                    var separator = ConfigManager.NewlineBetweenDefinitions ? "\n" : "";
                    defResult += separator;

                    ++count;
                }
            }

            defResult = defResult.Trim('\n');
            return defResult;
        }

        private static string BuildEpwingWordDefinition(EpwingResult jMDictResult)
        {
            //todo
            // int count = 1;
            string defResult = "";
            for (int i = 0; i < jMDictResult.Definitions.Count; i++)
            {
                // if (jMDictResult.WordClasses.Any() && jMDictResult.WordClasses[i].Any())
                // {
                //     defResult += "(";
                //     defResult += string.Join(", ", jMDictResult.WordClasses[i]);
                //     defResult += ") ";
                // }

                if (jMDictResult.Definitions.Any())
                {
                    // defResult += "(" + count + ") ";

                    var separator = ConfigManager.NewlineBetweenDefinitions ? "\n" : "; ";
                    defResult += string.Join(separator, jMDictResult.Definitions[i]);

                    // ++count;
                }
            }

            defResult = defResult.Trim('\n');
            return defResult;
        }

        public static IEnumerable<string> UnicodeIterator(this string s)
        {
            for (int i = 0; i < s.Length; ++i)
            {
                yield return char.ConvertFromUtf32(char.ConvertToUtf32(s, i));
                if (char.IsHighSurrogate(s, i))
                    i++;
            }
        }

        public static void ShowAddNameWindow()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            var addNameWindowInstance = AddNameWindow.Instance;
            addNameWindowInstance.SpellingTextBox.Text = mainWindow.MainTextBox.SelectedText;
            addNameWindowInstance.ShowDialog();
        }

        public static void ShowAddWordWindow()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            var addWordWindowInstance = AddWordWindow.Instance;
            addWordWindowInstance.SpellingsTextBox.Text = mainWindow.MainTextBox.SelectedText;
            addWordWindowInstance.ShowDialog();
        }

        public static void ShowPreferencesWindow()
        {
            ConfigManager.LoadPreferences(PreferencesWindow.Instance);
            PreferencesWindow.Instance.ShowDialog();
        }

        public static void SearchWithBrowser()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            if (mainWindow.MainTextBox.SelectedText.Length > 0)
                Process.Start(new ProcessStartInfo("cmd",
                        $"/c start https://www.google.com/search?q={mainWindow.MainTextBox.SelectedText}^&hl=ja")
                    { CreateNoWindow = true });
        }
    }
}