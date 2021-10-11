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

        public static List<Dictionary<LookupResult, List<string>>> Lookup(string text)
        {
            var preciseTimeNow = new DateTime(Stopwatch.GetTimestamp());
            if ((preciseTimeNow - _lastLookupTime).Milliseconds < ConfigManager.LookupRate) return null;
            _lastLookupTime = preciseTimeNow;

            // TODO: refactor these tuples into something else
            var wordResults =
                new Dictionary<string,
                    (List<JMdictResult> jMdictResults, List<string> processList, string foundForm,
                    DictType dictType)>();
            var nameResults =
                new Dictionary<string,
                    (List<JMnedictResult> jMnedictResults, List<string> processList, string foundForm,
                    DictType dictType)>();
            var epwingWordResults =
                new List<Dictionary<string,
                    (List<EpwingResult> epwingResults, List<string> processList, string foundForm,
                    DictType dictType)>>();
            var kanjiResult =
                new Dictionary<string,
                    (List<List<KanjiResult>>, List<string>, string, DictType)>();
           var customWordResults =
                new Dictionary<string,
                    (List<CustomWordEntry> customWordResults, List<string> processList, string foundForm,
                    DictType dictType)>();
           var customNamedResults =
                new Dictionary<string,
                    (List<CustomNameEntry> customNameResults, List<string> processList, string foundForm,
                    DictType dictType)>();

            if (ConfigManager.KanjiMode)
                if (Dicts.dicts[DictType.Kanjidic]?.Contents.Any() ?? false)
                {
                    return KanjiResultBuilder(GetKanjidicResults(text, DictType.Kanjidic));
                }

            List<string> textInHiraganaList = new();
            List<HashSet<Form>> deconjugationResultsList = new();

            for (int i = 0; i < text.Length; i++)
            {
                var textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);
                textInHiraganaList.Add(textInHiragana);
                deconjugationResultsList.Add(Deconjugator.Deconjugate(textInHiragana));
            }

            foreach ((DictType dictType, Dict dict) in Dicts.dicts)
            {
                switch (dictType)
                {
                    case DictType.JMdict:
                        wordResults = GetJMdictResults(text, textInHiraganaList, deconjugationResultsList,
                            DictType.JMdict);
                        break;
                    case DictType.JMnedict:
                        nameResults = GetJMnedictResults(text, textInHiraganaList, dictType);
                        break;
                    case DictType.Kanjidic:
                        // handled above and below
                        break;
                    case DictType.UnknownEpwing:
                        epwingWordResults.Add(GetEpwingResults(text, textInHiraganaList, deconjugationResultsList,
                            dict.Contents, dictType));
                        break;
                    case DictType.Daijirin:
                        epwingWordResults.Add(GetDaijirinResults(text, textInHiraganaList, deconjugationResultsList,
                            dict.Contents, dictType));
                        break;
                    case DictType.Daijisen:
                        // TODO
                        epwingWordResults.Add(GetDaijirinResults(text, textInHiraganaList, deconjugationResultsList,
                            dict.Contents, dictType));
                        break;
                    case DictType.Koujien:
                        // TODO
                        epwingWordResults.Add(GetDaijirinResults(text, textInHiraganaList, deconjugationResultsList,
                            dict.Contents, dictType));
                        break;
                    case DictType.Meikyou:
                        // TODO
                        epwingWordResults.Add(GetDaijirinResults(text, textInHiraganaList, deconjugationResultsList,
                            dict.Contents, dictType));
                        break;
                    case DictType.CustomWordDictionary:
                        customWordResults = GetCustomWordResults(text, textInHiraganaList, deconjugationResultsList,
                            DictType.CustomWordDictionary);
                        break;
                    case DictType.CustomNameDictionary:
                        customNamedResults = GetCustomNameResults(text, textInHiraganaList, dictType);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (!wordResults.Any() && !nameResults.Any() &&
                (epwingWordResults.Any() && !epwingWordResults.First().Any()))
            {
                if (Dicts.dicts[DictType.Kanjidic]?.Contents.Any() ?? false)
                {
                    kanjiResult = GetKanjidicResults(text, DictType.Kanjidic);
                }
            }

            List<Dictionary<LookupResult, List<string>>> lookupResults = new();

            if (wordResults.Any())
                lookupResults.AddRange(WordResultBuilder(wordResults));

            if (epwingWordResults.Any())
                foreach (var epwingWordResult in epwingWordResults)
                {
                    // if (epwingWordResult.Any())
                    lookupResults.AddRange(EpwingWordResultBuilder(epwingWordResult));
                }

            if (nameResults.Any())
                lookupResults.AddRange(NameResultBuilder(nameResults));

            if (kanjiResult.Any())
                lookupResults.AddRange(KanjiResultBuilder(kanjiResult));

            if (customWordResults.Any())
                lookupResults.AddRange(CustomWordResultBuilder(customWordResults));

            if (customNamedResults.Any())
                lookupResults.AddRange(CustomNameResultBuilder(customNamedResults));

            lookupResults = SortLookupResults(lookupResults);
            return lookupResults;
        }

        private static List<Dictionary<LookupResult, List<string>>>
            SortLookupResults(List<Dictionary<LookupResult, List<string>>> lookupResults)
        {
            return lookupResults
                .OrderByDescending(dict => dict[LookupResult.FoundForm][0].Length)
                .ThenBy(dict =>
                {
                    Enum.TryParse(dict[LookupResult.DictType][0], out DictType dictType);
                    return Dicts.dicts[dictType].Priority;
                })
                .ThenBy(dict => Convert.ToInt32(dict[LookupResult.Frequency][0]))
                .ToList();
        }

        private static
            Dictionary<string,
                (List<JMdictResult> jMdictResults, List<string> processList, string foundForm, DictType dictType)>
            GetJMdictResults(string text, List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList,
                DictType dictType)
        {
            var wordResults =
                new Dictionary<string, (List<JMdictResult> jMdictResults, List<string> processList, string foundForm,
                    DictType dictType)>();

            int succAttempt = 0;

            for (int i = 0; i < text.Length; i++)
            {
                bool tryLongVowelConversion = true;

                if (Dicts.dicts[DictType.JMdict].Contents.TryGetValue(textInHiraganaList[i], out var tempResult1))
                {
                    var tempResult = tempResult1.Cast<JMdictResult>().ToList();
                    wordResults.TryAdd(textInHiraganaList[i],
                        (tempResult, new List<string>(), text[..^i], dictType));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    foreach (var result in deconjugationResultsList[i])
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
                                    (resultsList, result.Process, text[..result.OriginalText.Length],
                                        dictType));
                                ++succAttempt;
                                tryLongVowelConversion = false;
                            }
                        }
                    }
                }

                if (tryLongVowelConversion && textInHiraganaList[i].Contains("ー") && textInHiraganaList[i][0] != 'ー')
                {
                    string textWithoutLongVowelMark = Kana.LongVowelMarkConverter(textInHiraganaList[i]);
                    if (Dicts.dicts[DictType.JMdict].Contents.TryGetValue(textWithoutLongVowelMark, out var tmpResult1))
                    {
                        var tmpResult = tmpResult1.Cast<JMdictResult>().ToList();
                        wordResults.Add(textInHiraganaList[i],
                            (tmpResult, new List<string>(), text[..^i], dictType));
                    }
                }
            }

            return wordResults;
        }

        private static
            Dictionary<string,
                (List<JMnedictResult> jMnedictResults, List<string> processList, string foundForm, DictType dictType)>
            GetJMnedictResults(string text, List<string> textInHiraganaList, DictType dictType)
        {
            var nameResults =
                new Dictionary<string, (List<JMnedictResult> jMnedictResults, List<string> processList, string foundForm
                    , DictType dictType)>();

            for (int i = 0; i < text.Length; i++)
            {
                if (Dicts.dicts[DictType.JMnedict].Contents.TryGetValue(textInHiraganaList[i], out var tempNameResult1))
                {
                    var tempNameResult = tempNameResult1.Cast<JMnedictResult>().ToList();
                    nameResults.TryAdd(textInHiraganaList[i],
                        (tempNameResult, new List<string>(), text[..^i], dictType));
                }
            }

            return nameResults;
        }

        private static Dictionary<string, (List<List<KanjiResult>>, List<string>, string, DictType)>
            GetKanjidicResults(string text, DictType dictType)
        {
            var kanjiResult =
                new Dictionary<string, (List<List<KanjiResult>>, List<string>, string, DictType)>();

            if (Dicts.dicts[DictType.Kanjidic].Contents.TryGetValue(
                text.UnicodeIterator().DefaultIfEmpty(string.Empty).First(), out List<IResult> kResult1))
            {
                var kResult = kResult1.Cast<KanjiResult>().ToList();
                kanjiResult.Add(text.UnicodeIterator().First(),
                    (new List<List<KanjiResult>> { kResult }, new List<string>(), text.UnicodeIterator().First(),
                        dictType));
            }

            return kanjiResult;
        }

        private static
            Dictionary<string,
                (List<EpwingResult> epwingResults, List<string> processList, string foundForm, DictType dictType)>
            GetDaijirinResults(string text, List<string> textInHiraganaList,
                List<HashSet<Form>> deconjugationResultsList, Dictionary<string, List<IResult>> dict, DictType dictType)
        {
            var daijirinWordResults =
                new Dictionary<string, (List<EpwingResult> epwingResults, List<string> processList, string foundForm,
                    DictType dictType)>();

            int succAttempt = 0;
            for (int i = 0; i < text.Length; i++)
            {
                bool tryLongVowelConversion = true;

                if (dict.TryGetValue(textInHiraganaList[i], out var hiraganaTempResult1))
                {
                    var hiraganaTempResult = hiraganaTempResult1.Cast<EpwingResult>().ToList();
                    daijirinWordResults.TryAdd(textInHiraganaList[i],
                        (hiraganaTempResult, new List<string>(), text[..^i], dictType));
                    tryLongVowelConversion = false;
                }

                //todo
                if (dict.TryGetValue(text, out var textTempResult1))
                {
                    var textTempResult = textTempResult1.Cast<EpwingResult>().ToList();
                    daijirinWordResults.TryAdd(text,
                        (textTempResult, new List<string>(), text[..^i], dictType));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    foreach (var result in deconjugationResultsList[i])
                    {
                        if (daijirinWordResults.ContainsKey(result.Text))
                            continue;

                        if (dict.TryGetValue(result.Text, out var temp))
                        {
                            List<EpwingResult> resultsList = new();

                            foreach (var rslt1 in temp)
                            {
                                var rslt = (EpwingResult) rslt1;

                                // if (rslt.WordClasses.SelectMany(pos => pos.Except(blacklistedTags))
                                //     .Intersect(result.Tags).Any())
                                // {
                                resultsList.Add(rslt);
                                // }
                            }

                            if (resultsList.Any())
                            {
                                daijirinWordResults.Add(result.Text,
                                    (resultsList, result.Process, text[..result.OriginalText.Length],
                                        dictType));
                                ++succAttempt;
                                tryLongVowelConversion = false;
                            }
                        }
                    }
                }

                if (tryLongVowelConversion && textInHiraganaList[i].Contains("ー") && textInHiraganaList[i][0] != 'ー')
                {
                    string textWithoutLongVowelMark = Kana.LongVowelMarkConverter(textInHiraganaList[i]);
                    if (dict.TryGetValue(textWithoutLongVowelMark, out var tmpResult1))
                    {
                        var tmpResult = tmpResult1.Cast<EpwingResult>().ToList();
                        daijirinWordResults.Add(textInHiraganaList[i],
                            (tmpResult, new List<string>(), text[..^i], dictType));
                    }
                }
            }

            return daijirinWordResults;
        }

        private static
            Dictionary<string,
                (List<EpwingResult> epwingResults, List<string> processList, string foundForm, DictType dictType)>
            GetEpwingResults(string text, List<string> textInHiraganaList,
                List<HashSet<Form>> deconjugationResultsList, Dictionary<string, List<IResult>> dict, DictType dictType)
        {
            var daijirinWordResults =
                new Dictionary<string, (List<EpwingResult> epwingResults, List<string> processList, string foundForm,
                    DictType dictType)>();

            int succAttempt = 0;
            for (int i = 0; i < text.Length; i++)
            {
                bool tryLongVowelConversion = true;

                if (dict.TryGetValue(textInHiraganaList[i], out var hiraganaTempResult1))
                {
                    var hiraganaTempResult = hiraganaTempResult1.Cast<EpwingResult>().ToList();
                    daijirinWordResults.TryAdd(textInHiraganaList[i],
                        (hiraganaTempResult, new List<string>(), text[..^i], dictType));
                    tryLongVowelConversion = false;
                }

                //todo
                if (dict.TryGetValue(text, out var textTempResult1))
                {
                    var textTempResult = textTempResult1.Cast<EpwingResult>().ToList();
                    daijirinWordResults.TryAdd(text,
                        (textTempResult, new List<string>(), text[..^i], dictType));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    foreach (var result in deconjugationResultsList[i])
                    {
                        if (daijirinWordResults.ContainsKey(result.Text))
                            continue;

                        if (dict.TryGetValue(result.Text, out var temp))
                        {
                            List<EpwingResult> resultsList = new();

                            foreach (var rslt1 in temp)
                            {
                                var rslt = (EpwingResult) rslt1;

                                // if (rslt.WordClasses.SelectMany(pos => pos.Except(blacklistedTags))
                                //     .Intersect(result.Tags).Any())
                                // {
                                resultsList.Add(rslt);
                                // }
                            }

                            if (resultsList.Any())
                            {
                                daijirinWordResults.Add(result.Text,
                                    (resultsList, result.Process, text[..result.OriginalText.Length],
                                        dictType));
                                ++succAttempt;
                                tryLongVowelConversion = false;
                            }
                        }
                    }
                }

                if (tryLongVowelConversion && textInHiraganaList[i].Contains("ー") && textInHiraganaList[i][0] != 'ー')
                {
                    string textWithoutLongVowelMark = Kana.LongVowelMarkConverter(textInHiraganaList[i]);
                    if (dict.TryGetValue(textWithoutLongVowelMark, out var tmpResult1))
                    {
                        var tmpResult = tmpResult1.Cast<EpwingResult>().ToList();
                        daijirinWordResults.Add(textInHiraganaList[i],
                            (tmpResult, new List<string>(), text[..^i], dictType));
                    }
                }
            }

            return daijirinWordResults;
        }

        private static
    Dictionary<string,
        (List<CustomWordEntry> customWordResults, List<string> processList, string foundForm, DictType dictType)>
    GetCustomWordResults(string text, List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList,
        DictType dictType)
        {
            var customWordResults =
                new Dictionary<string, (List<CustomWordEntry> customWordResults, List<string> processList, string foundForm,
                    DictType dictType)>();

            int succAttempt = 0;

            for (int i = 0; i < text.Length; i++)
            {
                bool tryLongVowelConversion = true;

                if (Dicts.dicts[DictType.CustomWordDictionary].Contents.TryGetValue(textInHiraganaList[i], out var tempResult1))
                {
                    var tempResult = tempResult1.Cast<CustomWordEntry>().ToList();
                    customWordResults.TryAdd(textInHiraganaList[i],
                        (tempResult, new List<string>(), text[..^i], dictType));
                    tryLongVowelConversion = false;
                }

                if (succAttempt < 3)
                {
                    foreach (var result in deconjugationResultsList[i])
                    {
                        if (customWordResults.ContainsKey(result.Text))
                            continue;

                        if (Dicts.dicts[DictType.CustomWordDictionary].Contents.TryGetValue(result.Text, out var temp))
                        {
                            List<CustomWordEntry> resultsList = new();

                            foreach (var rslt1 in temp)
                            {
                                var rslt = (CustomWordEntry)rslt1;
                                if (rslt.WordClasses.Intersect(result.Tags).Any())
                                {
                                    resultsList.Add(rslt);
                                }
                            }

                            if (resultsList.Any())
                            {
                                customWordResults.Add(result.Text,
                                    (resultsList, result.Process, text[..result.OriginalText.Length],
                                        dictType));
                                ++succAttempt;
                                tryLongVowelConversion = false;
                            }
                        }
                    }
                }

                if (tryLongVowelConversion && textInHiraganaList[i].Contains("ー") && textInHiraganaList[i][0] != 'ー')
                {
                    string textWithoutLongVowelMark = Kana.LongVowelMarkConverter(textInHiraganaList[i]);
                    if (Dicts.dicts[DictType.CustomWordDictionary].Contents.TryGetValue(textWithoutLongVowelMark, out var tmpResult1))
                    {
                        var tmpResult = tmpResult1.Cast<CustomWordEntry>().ToList();
                        customWordResults.Add(textInHiraganaList[i],
                            (tmpResult, new List<string>(), text[..^i], dictType));
                    }
                }
            }

            return customWordResults;
        }

        private static
            Dictionary<string,
                (List<CustomNameEntry> customNameResults, List<string> processList, string foundForm, DictType dictType)>
            GetCustomNameResults(string text, List<string> textInHiraganaList, DictType dictType)
        {
            var customNameResults =
                new Dictionary<string, (List<CustomNameEntry> customNameResults, List<string> processList, string foundForm
                    , DictType dictType)>();

            for (int i = 0; i < text.Length; i++)
            {
                if (Dicts.dicts[DictType.CustomNameDictionary].Contents.TryGetValue(textInHiraganaList[i], out var tempNameResult1))
                {
                    var tempNameResult = tempNameResult1.Cast<CustomNameEntry>().ToList();
                    customNameResults.TryAdd(textInHiraganaList[i],
                        (tempNameResult, new List<string>(), text[..^i], dictType));
                }
            }

            return customNameResults;
        }

        private static List<Dictionary<LookupResult, List<string>>> WordResultBuilder
        (Dictionary<string,
                (List<JMdictResult> jMdictResults, List<string> processList, string foundForm, DictType dictType)>
            wordResults)
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
                        if (freq == 0)
                            frequency = new List<string> { FakeFrequency };
                        else
                            frequency = new List<string> { freq.ToString() };
                    }

                    else frequency = new List<string> { FakeFrequency };

                    var dictType = new List<string> { wordResult.Value.dictType.ToString() };

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
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> NameResultBuilder
        (Dictionary<string,
                (List<JMnedictResult> jMnedictResults, List<string> processList, string foundForm, DictType dictType)>
            nameResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (var nameResult in nameResults)
            {
                foreach (var jMnedictResult in nameResult.Value.jMnedictResults)
                {
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { jMnedictResult.PrimarySpelling };

                    var readings = jMnedictResult.Readings != null
                        ? jMnedictResult.Readings.ToList()
                        : new List<string>();

                    var foundForm = new List<string> { nameResult.Value.foundForm };

                    var edictID = new List<string> { jMnedictResult.Id };

                    var dictType = new List<string> { nameResult.Value.dictType.ToString() };

                    var alternativeSpellings = jMnedictResult.AlternativeSpellings ?? new List<string>();

                    var definitions = new List<string> { BuildNameDefinition(jMnedictResult) };

                    result.Add(LookupResult.EdictID, edictID);
                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);

                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Frequency, new List<string> { FakeFrequency });
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> KanjiResultBuilder
        (Dictionary<string,
                (List<List<KanjiResult>> kanjiResult, List<string> processList, string foundForm, DictType dictType)>
            kanjiResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();
            var result = new Dictionary<LookupResult, List<string>>();

            if (!kanjiResults.Any())
                return results;

            KanjiResult kanjiResult = kanjiResults.First().Value.kanjiResult.First().First();

            var dictType = new List<string> { kanjiResults.First().Value.dictType.ToString() };

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
            result.Add(LookupResult.DictType, dictType);

            results.Add(result);
            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> EpwingWordResultBuilder
        (Dictionary<string,
                (List<EpwingResult> epwingResults, List<string> processList, string foundForm, DictType dictType)>
            wordResults)
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
                    var dictType = new List<string> { wordResult.Value.dictType.ToString() };

                    var definitions = new List<string> { BuildEpwingWordDefinition(jMDictResult) };

                    // TODO: Should be filtered while loading the dict ideally (+ it's daijirin specific?)
                    if (definitions.First().Contains("→英和"))
                        continue;

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> CustomWordResultBuilder
(Dictionary<string,
        (List<CustomWordEntry> customWordResults, List<string> processList, string foundForm, DictType dictType)>
    customWordResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (var wordResult in customWordResults)
            {
                foreach (var customWordDictResults in wordResult.Value.customWordResults)
                {
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { customWordDictResults.PrimarySpelling };

                    var readings = customWordDictResults.Readings.ToList();
                    var foundForm = new List<string> { wordResult.Value.foundForm };

                    List<string> alternativeSpellings;
                    if (customWordDictResults.AlternativeSpellings != null)
                        alternativeSpellings = customWordDictResults.AlternativeSpellings.ToList();
                    else
                        alternativeSpellings = new List<string>();
                    var process = wordResult.Value.processList;

                    List<string> frequency = new List<string> { FakeFrequency };

                    var dictType = new List<string> { wordResult.Value.dictType.ToString() };

                    var definitions = new List<string> { BuildCustomWordDefinition(customWordDictResults) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
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

        private static string BuildCustomWordDefinition(CustomWordEntry customWordResult)
        {
            int count = 1;
            string defResult = "";

            if (customWordResult.WordClasses.Any())
            {
                string tempWordClass;
                if (customWordResult.WordClasses.Contains("adj-i"))
                    tempWordClass = "adjective";
                else if (customWordResult.WordClasses.Contains("v1"))
                    tempWordClass = "verb";
                else if (customWordResult.WordClasses.Contains("noun"))
                    tempWordClass = "noun";
                else
                    tempWordClass = "other";

                defResult += "(" + tempWordClass + ") ";
            }

            for (int i = 0; i < customWordResult.Definitions.Count; i++)
            {
                if (customWordResult.Definitions.Any())
                {
                    defResult += "(" + count + ") ";

                    defResult += string.Join("; ", customWordResult.Definitions[i]) + " ";

                    var separator = ConfigManager.NewlineBetweenDefinitions ? "\n" : "";
                    defResult += separator;

                    ++count;
                }
            }

            defResult = defResult.Trim('\n');
            return defResult;
        }
        private static string BuildCustomNameDefinition(CustomNameEntry customNameDictResult)
        {
            string defResult = "(" + customNameDictResult.NameType + ") " + customNameDictResult.Reading;

            return defResult;
        }

        private static List<Dictionary<LookupResult, List<string>>> CustomNameResultBuilder
(Dictionary<string,
        (List<CustomNameEntry> customNameResults, List<string> processList, string foundForm, DictType dictType)>
    customNameResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (var customNameResult in customNameResults)
            {
                foreach (var customNameDictResult in customNameResult.Value.customNameResults)
                {
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { customNameDictResult.PrimarySpelling };

                    var readings = new List<string> { customNameDictResult.Reading };

                    var foundForm = new List<string> { customNameResult.Value.foundForm };

                    var dictType = new List<string> { customNameResult.Value.dictType.ToString() };

                    var definitions = new List<string> { BuildCustomNameDefinition(customNameDictResult) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);

                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Frequency, new List<string> { FakeFrequency });
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
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
    }
}