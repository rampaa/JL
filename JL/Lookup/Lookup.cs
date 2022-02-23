using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JL.Deconjugation;
using JL.Dicts;
using JL.Dicts.CustomDict;
using JL.Dicts.EDICT.JMdict;
using JL.Dicts.EDICT.JMnedict;
using JL.Dicts.EDICT.KANJIDIC;
using JL.Dicts.EPWING;
using JL.Frequency;
using JL.PoS;
using JL.Utilities;

namespace JL.Lookup
{
    public static class Lookup
    {
        private static DateTime s_lastLookupTime;

        public static List<Dictionary<LookupResult, List<string>>> LookupText(string text)
        {
            DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
            if ((preciseTimeNow - s_lastLookupTime).Milliseconds < ConfigManager.LookupRate) return null;
            s_lastLookupTime = preciseTimeNow;

            if (ConfigManager.KanjiMode)
            {
                return KanjiResultBuilder(GetKanjidicResults(text, DictType.Kanjidic));
            }

            Dictionary<string, IntermediaryResult> jMdictResults = new();
            Dictionary<string, IntermediaryResult> jMnedictResults = new();
            List<Dictionary<string, IntermediaryResult>> epwingWordResultsList = new();
            Dictionary<string, IntermediaryResult> kanjiResult = new();
            Dictionary<string, IntermediaryResult> customWordResults = new();
            Dictionary<string, IntermediaryResult> customNameResults = new();

            List<string> textInHiraganaList = new();
            List<HashSet<Form>> deconjugationResultsList = new();

            bool needDeconjugation = Storage.Dicts[DictType.JMdict].Active
                || Storage.Dicts[DictType.CustomWordDictionary].Active
                || Storage.Dicts[DictType.Daijirin].Active
                || Storage.Dicts[DictType.Kenkyuusha].Active
                || Storage.Dicts[DictType.Daijisen].Active
                || Storage.Dicts[DictType.Gakken].Active
                || Storage.Dicts[DictType.Kotowaza].Active
                || Storage.Dicts[DictType.Koujien].Active
                || Storage.Dicts[DictType.Meikyou].Active;

            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);
                textInHiraganaList.Add(textInHiragana);

                if (needDeconjugation)
                {
                    deconjugationResultsList.Add(Deconjugator.Deconjugate(textInHiragana));
                }
            }

            foreach ((DictType dictType, Dict dict) in Storage.Dicts)
            {
                if (dict.Active)
                {
                    switch (dictType)
                    {
                        case DictType.JMdict:
                            jMdictResults = GetWordResults(text, textInHiraganaList, deconjugationResultsList, dictType);
                            break;
                        case DictType.JMnedict:
                            jMnedictResults = GetNameResults(text, textInHiraganaList, dictType);
                            break;
                        case DictType.Kanjidic:
                            kanjiResult = GetKanjidicResults(text, DictType.Kanjidic);
                            break;
                        case DictType.Kenkyuusha:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Daijirin:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Daijisen:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Koujien:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Meikyou:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Gakken:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Kotowaza:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.CustomWordDictionary:
                            customWordResults = GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType);
                            break;
                        case DictType.CustomNameDictionary:
                            customNameResults = GetNameResults(text, textInHiraganaList, dictType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                    }
                }
            }

            List<Dictionary<LookupResult, List<string>>> lookupResults = new();

            if (jMdictResults.Any())
                lookupResults.AddRange(JmdictResultBuilder(jMdictResults));

            if (epwingWordResultsList.Any())
            {
                foreach (Dictionary<string, IntermediaryResult> epwingWordResult in epwingWordResultsList)
                {
                    lookupResults.AddRange(EpwingResultBuilder(epwingWordResult));
                }
            }

            if (jMnedictResults.Any())
                lookupResults.AddRange(JmnedictResultBuilder(jMnedictResults));

            if (kanjiResult.Any())
                lookupResults.AddRange(KanjiResultBuilder(kanjiResult));

            if (customWordResults.Any())
                lookupResults.AddRange(CustomWordResultBuilder(customWordResults));

            if (customNameResults.Any())
                lookupResults.AddRange(CustomNameResultBuilder(customNameResults));

            lookupResults = SortLookupResults(lookupResults);
            return lookupResults;
        }

        private static List<Dictionary<LookupResult, List<string>>> SortLookupResults(
            List<Dictionary<LookupResult, List<string>>> lookupResults)
        {
            return lookupResults
                .OrderByDescending(dict => dict[LookupResult.FoundForm][0].Length)
                .ThenBy(dict => Enum.TryParse(dict[LookupResult.DictType][0], out DictType dictType)
                    ? Storage.Dicts[dictType].Priority
                    : int.MaxValue)
                .ThenBy(dict => Convert.ToInt32(dict[LookupResult.Frequency][0]))
                .ToList();
        }

        private static (bool tryLongVowelConversion, int succAttempt) GetWordResultsHelper(DictType dictType,
            Dictionary<string, IntermediaryResult> results,
            HashSet<Form> deconjugationList,
            string text,
            int i,
            string textInHiragana,
            int succAttempt)
        {
            Dictionary<string, List<IResult>> dictionary = Storage.Dicts[dictType].Contents;

            bool tryLongVowelConversion = true;

            if (dictionary.TryGetValue(textInHiragana, out List<IResult> tempResult))
            {
                results.TryAdd(textInHiragana,
                    new IntermediaryResult(tempResult, new List<List<string>> { new() }, text[..^i],
                        dictType));
                tryLongVowelConversion = false;
            }

            if (succAttempt < 3)
            {
                foreach (Form deconjugationResult in deconjugationList)
                {
                    string lastTag = "";
                    if (deconjugationResult.Tags.Count > 0)
                        lastTag = deconjugationResult.Tags.Last();

                    if (dictionary.TryGetValue(deconjugationResult.Text, out List<IResult> dictResults))
                    {
                        List<IResult> resultsList = new();

                        switch (dictType)
                        {
                            case DictType.JMdict:
                                {
                                    foreach (JMdictResult dictResult in dictResults.ToList())
                                    {
                                        if (deconjugationResult.Tags.Count == 0 || dictResult.WordClasses.SelectMany(pos => pos).Contains(lastTag))
                                        {
                                            resultsList.Add(dictResult);
                                        }
                                    }
                                }
                                break;

                            case DictType.CustomWordDictionary:
                                {
                                    foreach (CustomWordEntry dictResult in dictResults.ToList())
                                    {
                                        if (deconjugationResult.Tags.Count == 0 || dictResult.WordClasses.Contains(lastTag))
                                        {
                                            resultsList.Add(dictResult);
                                        }
                                    }
                                }
                                break;

                            case DictType.Daijirin:
                            case DictType.Daijisen:
                            case DictType.Gakken:
                            case DictType.Kenkyuusha:
                            case DictType.Kotowaza:
                            case DictType.Koujien:
                            case DictType.Meikyou:
                                {
                                    foreach (EpwingResult dictResult in dictResults.Cast<EpwingResult>())
                                    {
                                        bool noMatchingEntryInJmdictWc = true;

                                        if (deconjugationResult.Tags.Count == 0 || dictResult.WordClasses.Contains(lastTag))
                                        {
                                            resultsList.Add(dictResult);
                                        }
                                        else if (Storage.WcDict.TryGetValue(deconjugationResult.Text, out List<JmdictWc> jmdictWcResults))
                                        {
                                            foreach (JmdictWc jmdictWcResult in jmdictWcResults)
                                            {
                                                if (dictResult.PrimarySpelling == jmdictWcResult.Spelling
                                                    && (jmdictWcResult.Readings?.Contains(dictResult.Reading)
                                                        ?? string.IsNullOrEmpty(dictResult.Reading)))
                                                {
                                                    noMatchingEntryInJmdictWc = false;
                                                    if (deconjugationResult.Tags.Count == 0 ||
                                                        jmdictWcResult.WordClasses.Contains(lastTag))
                                                    {
                                                        resultsList.Add(dictResult);
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        if (deconjugationResult.Tags.Count != 0 && !dictResult.WordClasses.Any() &&
                                            noMatchingEntryInJmdictWc)
                                        {
                                            resultsList.Add(dictResult);
                                        }
                                    }
                                }
                                break;
                        }

                        if (resultsList.Any())
                        {
                            if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult r))
                            {
                                if (r.FoundForm == deconjugationResult.OriginalText)
                                    r.ProcessList.Add(deconjugationResult.Process);
                            }
                            else
                            {
                                results.Add(deconjugationResult.Text,
                                    new IntermediaryResult(resultsList, new List<List<string>> { deconjugationResult.Process },
                                        text[..deconjugationResult.OriginalText.Length],
                                        dictType)
                                );
                            }

                            ++succAttempt;
                            tryLongVowelConversion = false;
                        }
                    }
                }
            }

            return (tryLongVowelConversion, succAttempt);
        }

        private static Dictionary<string, IntermediaryResult> GetWordResults(string text,
            List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList, DictType dictType)
        {
            Dictionary<string, IntermediaryResult> results = new();

            int succAttempt = 0;

            for (int i = 0; i < text.Length; i++)
            {
                (bool tryLongVowelConversion, succAttempt) = GetWordResultsHelper(dictType, results,
                    deconjugationResultsList[i], text, i, textInHiraganaList[i], succAttempt);

                if (tryLongVowelConversion && textInHiraganaList[i].Contains('ー') &&
                    textInHiraganaList[i][0] != 'ー')
                {
                    List<string> textWithoutLongVowelMarkList = Kana.LongVowelMarkConverter(textInHiraganaList[i]);
                    foreach (string textWithoutLongVowelMark in textWithoutLongVowelMarkList)
                    {
                        succAttempt = GetWordResultsHelper(dictType, results, deconjugationResultsList[i],
                            text, i, textWithoutLongVowelMark, succAttempt).succAttempt;
                    }
                }
            }

            return results;
        }

        private static Dictionary<string, IntermediaryResult> GetNameResults(string text,
            List<string> textInHiraganaList, DictType dictType)
        {
            Dictionary<string, IntermediaryResult> nameResults = new();

            for (int i = 0; i < text.Length; i++)
            {
                if (Storage.Dicts[dictType].Contents
                    .TryGetValue(textInHiraganaList[i], out List<IResult> result))
                {
                    nameResults.TryAdd(textInHiraganaList[i],
                        new IntermediaryResult(result, new List<List<string>>(), text[..^i], dictType));
                }
            }

            return nameResults;
        }

        private static Dictionary<string, IntermediaryResult> GetKanjidicResults(string text, DictType dictType)
        {
            Dictionary<string, IntermediaryResult> kanjiResults = new();

            if (Storage.Dicts[DictType.Kanjidic].Contents.TryGetValue(
                text.UnicodeIterator().DefaultIfEmpty(string.Empty).First(), out List<IResult> result))
            {
                kanjiResults.Add(text.UnicodeIterator().First(),
                    new IntermediaryResult(result, new List<List<string>>(), text.UnicodeIterator().First(),
                        dictType));
            }

            return kanjiResults;
        }

        private static List<Dictionary<LookupResult, List<string>>> JmdictResultBuilder(
            Dictionary<string, IntermediaryResult> jmdictResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (KeyValuePair<string, IntermediaryResult> wordResult in jmdictResults)
            {
                foreach (IResult iResult in wordResult.Value.ResultsList)
                {
                    var jMDictResult = (JMdictResult)iResult;
                    Dictionary<LookupResult, List<string>> result = new();

                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };

                    //var kanaSpellings = jMDictResult.KanaSpellings ?? new List<string>();

                    List<string> readings = jMDictResult.Readings ?? new();

                    var foundForm = new List<string> { wordResult.Value.FoundForm };

                    var edictID = new List<string> { jMDictResult.Id };

                    List<string> alternativeSpellings = jMDictResult.AlternativeSpellings ?? new();

                    List<string> process = ProcessProcess(wordResult.Value);

                    List<string> frequency = GetJMDictFreq(jMDictResult);

                    var dictType = new List<string> { wordResult.Value.DictType.ToString() };

                    var definitions = new List<string> { BuildJmdictDefinition(jMDictResult) };

                    List<string> pOrthographyInfoList = jMDictResult.POrthographyInfoList ?? new();

                    List<List<string>> rLists = jMDictResult.ROrthographyInfoList ?? new();
                    List<List<string>> aLists = jMDictResult.AOrthographyInfoList ?? new();
                    List<string> rOrthographyInfoList = new();
                    List<string> aOrthographyInfoList = new();

                    foreach (List<string> rList in rLists)
                    {
                        StringBuilder formatedROrthographyInfo = new();
                        foreach (string rOrthographyInfo in rList)
                        {
                            formatedROrthographyInfo.Append(rOrthographyInfo);
                            formatedROrthographyInfo.Append(", ");
                        }

                        rOrthographyInfoList.Add(formatedROrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                    }

                    foreach (List<string> aList in aLists)
                    {
                        StringBuilder formatedAOrthographyInfo = new();
                        foreach (string str in aList)
                        {
                            formatedAOrthographyInfo.Append(str);
                            formatedAOrthographyInfo.Append(", ");
                        }

                        aOrthographyInfoList.Add(formatedAOrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                    }

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    //result.Add(LookupResult.KanaSpellings, kanaSpellings);
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

        private static List<Dictionary<LookupResult, List<string>>> JmnedictResultBuilder(
            Dictionary<string, IntermediaryResult> jmnedictResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (KeyValuePair<string, IntermediaryResult> nameResult in jmnedictResults)
            {
                foreach (IResult iResult in nameResult.Value.ResultsList)
                {
                    var jMnedictResult = (JMnedictResult)iResult;
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { jMnedictResult.PrimarySpelling };

                    List<string> readings = jMnedictResult.Readings ?? new List<string>();

                    var foundForm = new List<string> { nameResult.Value.FoundForm };

                    var edictID = new List<string> { jMnedictResult.Id };

                    var dictType = new List<string> { nameResult.Value.DictType.ToString() };

                    List<string> alternativeSpellings = jMnedictResult.AlternativeSpellings ?? new List<string>();

                    var definitions = new List<string> { BuildJmnedictDefinition(jMnedictResult) };

                    result.Add(LookupResult.EdictID, edictID);
                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);

                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Frequency, new List<string> { MainWindowUtilities.FakeFrequency });
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> KanjiResultBuilder(
            Dictionary<string, IntermediaryResult> kanjiResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();
            var result = new Dictionary<LookupResult, List<string>>();

            if (!kanjiResults.Any())
                return results;

            List<IResult> iResult = kanjiResults.First().Value.ResultsList;
            KanjiResult kanjiResult = (KanjiResult)iResult.First();

            var dictType = new List<string> { kanjiResults.First().Value.DictType.ToString() };

            result.Add(LookupResult.FoundSpelling, new List<string> { kanjiResults.First().Key });
            result.Add(LookupResult.Definitions, kanjiResult.Meanings);
            result.Add(LookupResult.OnReadings, kanjiResult.OnReadings);
            result.Add(LookupResult.KunReadings, kanjiResult.KunReadings);
            result.Add(LookupResult.Nanori, kanjiResult.Nanori);
            result.Add(LookupResult.StrokeCount, new List<string> { kanjiResult.StrokeCount.ToString() });
            result.Add(LookupResult.Grade, new List<string> { kanjiResult.Grade.ToString() });
            result.Add(LookupResult.Composition, new List<string> { kanjiResult.Composition });
            result.Add(LookupResult.Frequency, new List<string> { kanjiResult.Frequency.ToString() });

            var foundForm = new List<string> { kanjiResults.First().Value.FoundForm };
            result.Add(LookupResult.FoundForm, foundForm);
            result.Add(LookupResult.DictType, dictType);

            results.Add(result);
            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> EpwingResultBuilder(
            Dictionary<string, IntermediaryResult> epwingResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (KeyValuePair<string, IntermediaryResult> wordResult in epwingResults)
            {
                foreach (IResult iResult in wordResult.Value.ResultsList)
                {
                    var epwingResult = (EpwingResult)iResult;
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { epwingResult.PrimarySpelling };

                    var reading = new List<string> { epwingResult.Reading };

                    var foundForm = new List<string> { wordResult.Value.FoundForm };

                    List<string> process = ProcessProcess(wordResult.Value);

                    List<string> frequency = GetEpwingFreq(epwingResult);

                    var dictType = new List<string> { wordResult.Value.DictType.ToString() };

                    var definitions = new List<string> { BuildEpwingDefinition(epwingResult) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, reading);
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
        private static List<Dictionary<LookupResult, List<string>>> CustomWordResultBuilder(
            Dictionary<string, IntermediaryResult> customWordResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (KeyValuePair<string, IntermediaryResult> wordResult in customWordResults)
            {
                int wordResultCount = wordResult.Value.ResultsList.Count;
                for (int i = 0; i < wordResultCount; i++)
                {
                    var customWordDictResult = (CustomWordEntry)wordResult.Value.ResultsList[i];
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { customWordDictResult.PrimarySpelling };

                    List<string> readings = customWordDictResult.Readings != null
                        ? customWordDictResult.Readings.ToList()
                        : new List<string>();

                    var foundForm = new List<string> { wordResult.Value.FoundForm };

                    List<string> alternativeSpellings;

                    if (customWordDictResult.AlternativeSpellings != null)
                        alternativeSpellings = customWordDictResult.AlternativeSpellings.ToList();
                    else
                        alternativeSpellings = new();

                    List<string> process = ProcessProcess(wordResult.Value);

                    List<string> frequency = GetCustomWordFreq(customWordDictResult);
                    if (frequency.First() == MainWindowUtilities.FakeFrequency)
                        frequency = new List<string> { (wordResultCount - i).ToString() };

                    var dictType = new List<string> { wordResult.Value.DictType.ToString() };

                    var definitions = new List<string> { BuildCustomWordDefinition(customWordDictResult) };

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

        private static List<Dictionary<LookupResult, List<string>>> CustomNameResultBuilder(
            Dictionary<string, IntermediaryResult> customNameResults)
        {
            var results = new List<Dictionary<LookupResult, List<string>>>();

            foreach (KeyValuePair<string, IntermediaryResult> customNameResult in customNameResults)
            {
                int resultCount = customNameResult.Value.ResultsList.Count;
                for (int i = 0; i < resultCount; i++)
                {
                    var customNameDictResult = (CustomNameEntry)customNameResult.Value.ResultsList[i];
                    var result = new Dictionary<LookupResult, List<string>>();

                    var foundSpelling = new List<string> { customNameDictResult.PrimarySpelling };

                    var readings = new List<string> { customNameDictResult.Reading };

                    var foundForm = new List<string> { customNameResult.Value.FoundForm };

                    var dictType = new List<string> { customNameResult.Value.DictType.ToString() };

                    var definitions = new List<string> { BuildCustomNameDefinition(customNameDictResult) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);

                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Frequency, new List<string> { (resultCount - i).ToString() });
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<string> GetJMDictFreq(JMdictResult jMDictResult)
        {
            List<string> frequency = new() { MainWindowUtilities.FakeFrequency };

            int freqValue = int.MaxValue;

            Storage.FreqDicts.TryGetValue(ConfigManager.FrequencyListName, out Dictionary<string, List<FrequencyEntry>> freqDict);

            if (freqDict == null)
                return frequency;

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(jMDictResult.PrimarySpelling),
                out List<FrequencyEntry> freqResults))
            {
                foreach (FrequencyEntry freqResult in freqResults)
                {
                    if ((jMDictResult.Readings != null && jMDictResult.Readings.Contains(freqResult.Spelling))
                        || (jMDictResult.Readings == null && jMDictResult.PrimarySpelling == freqResult.Spelling))
                    //|| (jMnedictResult.KanaSpellings != null && jMnedictResult.KanaSpellings.Contains(freqResult.Spelling))
                    {
                        if (freqValue > freqResult.Frequency)
                        {
                            freqValue = freqResult.Frequency;
                            frequency = new List<string> { freqResult.Frequency.ToString() };
                        }
                    }
                }

                if (freqValue == int.MaxValue && jMDictResult.AlternativeSpellings != null)
                {
                    foreach (string alternativeSpelling in jMDictResult.AlternativeSpellings)
                    {
                        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(alternativeSpelling),
                            out List<FrequencyEntry> alternativeSpellingFreqResults))
                        {
                            foreach (FrequencyEntry alternativeSpellingFreqResult in alternativeSpellingFreqResults)
                            {
                                if (jMDictResult.Readings != null &&
                                    jMDictResult.Readings.Contains(alternativeSpellingFreqResult.Spelling))
                                {
                                    if (freqValue > alternativeSpellingFreqResult.Frequency)
                                    {
                                        freqValue = alternativeSpellingFreqResult.Frequency;
                                        frequency = new List<string>
                                        {
                                            alternativeSpellingFreqResult.Frequency.ToString()
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            else if (jMDictResult.Readings != null)
            {
                foreach (string reading in jMDictResult.Readings)
                {
                    if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading), out List<FrequencyEntry> readingFreqResults))
                    {
                        foreach (FrequencyEntry readingFreqResult in readingFreqResults)
                        {
                            if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
                                || (jMDictResult.AlternativeSpellings != null &&
                                    jMDictResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                            //|| (jMDictResult.KanaSpellings != null && jMDictResult.KanaSpellings.Contains(readingFreqResults.Spelling))
                            {
                                if (freqValue > readingFreqResult.Frequency)
                                {
                                    freqValue = readingFreqResult.Frequency;
                                    frequency = new List<string> { readingFreqResult.Frequency.ToString() };
                                }
                            }
                        }
                    }
                }
            }

            return frequency;
        }

        private static List<string> GetEpwingFreq(EpwingResult epwingResult)
        {
            List<string> frequency = new() { MainWindowUtilities.FakeFrequency };

            int freqValue = int.MaxValue;

            Storage.FreqDicts.TryGetValue(ConfigManager.FrequencyListName, out Dictionary<string, List<FrequencyEntry>> freqDict);

            if (freqDict == null)
                return frequency;

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingResult.PrimarySpelling),
                out List<FrequencyEntry> freqResults))
            {
                foreach (FrequencyEntry freqResult in freqResults)
                {
                    if (epwingResult.Reading == freqResult.Spelling
                        || (string.IsNullOrEmpty(epwingResult.Reading) &&
                            epwingResult.PrimarySpelling == freqResult.Spelling))
                    {
                        if (freqValue > freqResult.Frequency)
                        {
                            freqValue = freqResult.Frequency;
                            frequency = new List<string> { freqResult.Frequency.ToString() };
                        }
                    }
                }
            }

            else if (!string.IsNullOrEmpty(epwingResult.Reading)
                     && freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingResult.Reading),
                         out List<FrequencyEntry> readingFreqResults))
            {
                foreach (FrequencyEntry readingFreqResult in readingFreqResults)
                {
                    if (epwingResult.Reading == readingFreqResult.Spelling && Kana.IsKatakana(epwingResult.Reading))
                    {
                        if (freqValue > readingFreqResult.Frequency)
                        {
                            freqValue = readingFreqResult.Frequency;
                            frequency = new List<string> { readingFreqResult.Frequency.ToString() };
                        }
                    }
                }
            }

            return frequency;
        }

        private static List<string> GetCustomWordFreq(CustomWordEntry customWordResult)
        {
            List<string> frequency = new() { MainWindowUtilities.FakeFrequency };

            int freqValue = int.MaxValue;

            Storage.FreqDicts.TryGetValue(ConfigManager.FrequencyListName, out Dictionary<string, List<FrequencyEntry>> freqDict);

            if (freqDict == null)
                return frequency;

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(customWordResult.PrimarySpelling),
                out List<FrequencyEntry> freqResults))
            {
                foreach (FrequencyEntry freqResult in freqResults)
                {
                    if (customWordResult.Readings != null && customWordResult.Readings.Contains(freqResult.Spelling)
                        || (customWordResult.Readings == null &&
                            customWordResult.PrimarySpelling == freqResult.Spelling))
                    {
                        if (freqValue > freqResult.Frequency)
                        {
                            freqValue = freqResult.Frequency;
                            frequency = new List<string> { freqResult.Frequency.ToString() };
                        }
                    }
                }

                if (freqValue == int.MaxValue && customWordResult.AlternativeSpellings != null)
                {
                    foreach (string alternativeSpelling in customWordResult.AlternativeSpellings)
                    {
                        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(alternativeSpelling),
                            out List<FrequencyEntry> alternativeSpellingFreqResults))
                        {
                            foreach (FrequencyEntry alternativeSpellingFreqResult in alternativeSpellingFreqResults)
                            {
                                if (customWordResult.Readings != null &&
                                    customWordResult.Readings.Contains(alternativeSpellingFreqResult.Spelling)
                                )
                                {
                                    if (freqValue > alternativeSpellingFreqResult.Frequency)
                                    {
                                        freqValue = alternativeSpellingFreqResult.Frequency;
                                        frequency = new List<string>
                                        {
                                            alternativeSpellingFreqResult.Frequency.ToString()
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            else
            {
                if (customWordResult.Readings != null)
                    foreach (string reading in customWordResult.Readings)
                    {
                        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading), out List<FrequencyEntry> readingFreqResults))
                        {
                            foreach (FrequencyEntry readingFreqResult in readingFreqResults)
                            {
                                if ((reading == readingFreqResult.Spelling && Kana.IsKatakana(reading))
                                    || (customWordResult.AlternativeSpellings != null &&
                                        customWordResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                                //|| (customWordResult.KanaSpellings != null && customWordResult.KanaSpellings.Contains(readingFreqResults.Spelling))
                                {
                                    if (freqValue > readingFreqResult.Frequency)
                                    {
                                        freqValue = readingFreqResult.Frequency;
                                        frequency = new List<string> { readingFreqResult.Frequency.ToString() };
                                    }
                                }
                            }
                        }
                    }
            }

            return frequency;
        }


        private static string BuildJmdictDefinition(JMdictResult jMDictResult)
        {
            string separator = ConfigManager.NewlineBetweenDefinitions ? "\n" : "";
            int count = 1;
            var defResult = new StringBuilder();
            for (int i = 0; i < jMDictResult.Definitions.Count; i++)
            {
                if (jMDictResult.WordClasses.Any() && jMDictResult.WordClasses[i].Any())
                {
                    defResult.Append('(');
                    defResult.Append(string.Join(", ", jMDictResult.WordClasses[i]));
                    defResult.Append(") ");
                }

                if (jMDictResult.Definitions.Any())
                {
                    defResult.Append($"({count}) ");

                    if (jMDictResult.SpellingInfo.Any() && jMDictResult.SpellingInfo[i] != null)
                    {
                        defResult.Append('(');
                        defResult.Append(jMDictResult.SpellingInfo[i]);
                        defResult.Append(") ");
                    }

                    if (jMDictResult.MiscList.Any() && jMDictResult.MiscList[i].Any())
                    {
                        defResult.Append('(');
                        defResult.Append(string.Join(", ", jMDictResult.MiscList[i]));
                        defResult.Append(") ");
                    }

                    defResult.Append(string.Join("; ", jMDictResult.Definitions[i]) + " ");

                    if (jMDictResult.RRestrictions != null && jMDictResult.RRestrictions[i].Any()
                        || jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                    {
                        defResult.Append("(only applies to ");

                        if (jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                        {
                            defResult.Append(string.Join("; ", jMDictResult.KRestrictions[i]));
                        }

                        if (jMDictResult.RRestrictions != null && jMDictResult.RRestrictions[i].Any())
                        {
                            if (jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                                defResult.Append("; ");

                            defResult.Append(string.Join("; ", jMDictResult.RRestrictions[i]));
                        }

                        defResult.Append(") ");
                    }

                    defResult.Append(separator);

                    ++count;
                }
            }

            return defResult.ToString().Trim('\n');
        }

        private static string BuildJmnedictDefinition(JMnedictResult jMnedictResult)
        {
            int count = 1;
            var defResult = new StringBuilder();

            if (jMnedictResult.NameTypes != null &&
                (jMnedictResult.NameTypes.Count > 1 || !jMnedictResult.NameTypes.Contains("unclass")))
            {
                foreach (string nameType in jMnedictResult.NameTypes)
                {
                    defResult.Append('(');
                    defResult.Append(nameType);
                    defResult.Append(") ");
                }
            }

            for (int i = 0; i < jMnedictResult.Definitions.Count; i++)
            {
                if (jMnedictResult.Definitions.Any())
                {
                    if (jMnedictResult.Definitions.Count > 0)
                        defResult.Append($"({count}) ");

                    defResult.Append($"{string.Join("; ", jMnedictResult.Definitions[i])} ");
                    ++count;
                }
            }

            return defResult.ToString();
        }

        private static string BuildEpwingDefinition(EpwingResult epwingResult)
        {
            var defResult = new StringBuilder();
            foreach (string definitionPart in epwingResult.Definitions)
            {
                // var separator = ConfigManager.NewlineBetweenDefinitions ? "\n" : "; ";
                const string separator = "\n";
                defResult.Append(definitionPart + separator);
            }

            return defResult.ToString().Trim('\n');
        }

        private static string BuildCustomWordDefinition(CustomWordEntry customWordResult)
        {
            string separator = ConfigManager.NewlineBetweenDefinitions ? "\n" : "";
            int count = 1;
            var defResult = new StringBuilder();

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

                defResult.Append($"({tempWordClass}) ");
            }

            for (int i = 0; i < customWordResult.Definitions.Count; i++)
            {
                if (customWordResult.Definitions.Any())
                {
                    defResult.Append($"({count}) ");
                    defResult.Append(string.Join("; ", customWordResult.Definitions[i]) + " ");
                    defResult.Append(separator);

                    ++count;
                }
            }

            return defResult.ToString().Trim('\n');
        }

        private static string BuildCustomNameDefinition(CustomNameEntry customNameDictResult)
        {
            string defResult = $"({customNameDictResult.NameType.ToLower()}) {customNameDictResult.Reading}";

            return defResult;
        }

        public static List<string> ProcessProcess(IntermediaryResult intermediaryResult)
        {
            StringBuilder deconj = new();
            bool first = true;

            foreach (List<string> form in intermediaryResult.ProcessList)
            {
                StringBuilder formText = new();
                int added = 0;

                for (int i = form.Count - 1; i >= 0; i--)
                {
                    string info = form[i];

                    if (info == "")
                        continue;

                    if (info.StartsWith('(') && info.EndsWith(')') && i != 0)
                        continue;

                    if (added > 0)
                        formText.Append('→');

                    added++;
                    formText.Append(info);
                }

                if (formText.Length != 0)
                {
                    if (first)
                        deconj.Append('～');
                    else
                        deconj.Append("; ");

                    deconj.Append(formText);
                }

                first = false;
            }

            return deconj.Length == 0 ? new List<string>() : new List<string> { deconj.ToString() };
        }
    }
}
