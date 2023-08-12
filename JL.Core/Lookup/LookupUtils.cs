using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Dicts.EPWING.EpwingNazeka;
using JL.Core.Dicts.EPWING.EpwingYomichan;
using JL.Core.Dicts.YomichanKanji;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Core.WordClass;

namespace JL.Core.Lookup;

public static class LookupUtils
{
    private static DateTime s_lastLookupTime;

    public static List<LookupResult>? LookupText(string text)
    {
        DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
        if ((preciseTimeNow - s_lastLookupTime).TotalMilliseconds < CoreConfig.LookupRate)
        {
            return null;
        }

        s_lastLookupTime = preciseTimeNow;

        List<LookupResult> lookupResults = new();

        if (CoreConfig.KanjiMode)
        {
            _ = Parallel.ForEach(DictUtils.Dicts.Values.ToList(), dict =>
            {
                if (dict.Active)
                {
                    if (dict.Type is DictType.Kanjidic)
                    {
                        lookupResults.AddRange(BuildKanjidicResult(GetKanjiResults(text, dict)));
                    }

                    else if (dict.Type is DictType.KanjigenYomichan)
                    {
                        lookupResults.AddRange(BuildEpwingYomichanResult(GetKanjiResults(text, dict)));
                    }

                    else if (DictUtils.s_kanjiDictTypes.Contains(dict.Type))
                    {
                        if (DictUtils.YomichanDictTypes.Contains(dict.Type))
                        {
                            lookupResults.AddRange(BuildYomichanKanjiResult(GetKanjiResults(text, dict)));
                        }

                        else //if (DictUtils.NazekaDictTypes.Contains(dict.Type))
                        {
                            lookupResults.AddRange(BuildEpwingNazekaResult(GetKanjiResults(text, dict)));
                        }
                    }
                }
            });

            return lookupResults.Count > 0
                ? SortLookupResults(lookupResults)
                : null;
        }

        Dictionary<string, IntermediaryResult> jmdictResults = new();
        Dictionary<string, IntermediaryResult> jmnedictResults = new();
        ConcurrentQueue<Dictionary<string, IntermediaryResult>> epwingYomichanWordResultsList = new();
        ConcurrentQueue<Dictionary<string, IntermediaryResult>> epwingYomichanKanjiResultsList = new();
        ConcurrentQueue<Dictionary<string, IntermediaryResult>> epwingYomichanNameResultsList = new();
        ConcurrentQueue<Dictionary<string, IntermediaryResult>> epwingNazekaWordResultsList = new();
        ConcurrentQueue<Dictionary<string, IntermediaryResult>> epwingNazekaKanjiResultsList = new();
        ConcurrentQueue<Dictionary<string, IntermediaryResult>> epwingNazekaNameResultsList = new();
        Dictionary<string, IntermediaryResult> kanjidicResults = new();
        Dictionary<string, IntermediaryResult> customWordResults = new();
        Dictionary<string, IntermediaryResult> customNameResults = new();

        List<string> textList = new();
        List<string> textInHiraganaList = new();
        List<HashSet<Form>> deconjugationResultsList = new();

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[text.Length - i - 1]))
            {
                continue;
            }

            string currentText = text[..^i];

            textList.Add(currentText);

            string textInHiragana = JapaneseUtils.KatakanaToHiragana(currentText);
            textInHiraganaList.Add(textInHiragana);

            deconjugationResultsList.Add(Deconjugator.Deconjugate(textInHiragana));
        }

        _ = Parallel.ForEach(DictUtils.Dicts.Values.ToList(), dict =>
        {
            if (dict.Active)
            {
                switch (dict.Type)
                {
                    case DictType.JMdict:
                        jmdictResults = GetWordResults(textList, textInHiraganaList, deconjugationResultsList,
                            dict);
                        break;

                    case DictType.JMnedict:
                        jmnedictResults = GetNameResults(textList, textInHiraganaList, dict);
                        break;

                    case DictType.Kanjidic:
                        kanjidicResults = GetKanjiResults(text, dict);
                        break;

                    case DictType.KanjigenYomichan:
                        // Template-wise, Kanjigen is a word dictionary that's why its results are put into Yomichan Word Results
                        // Content-wise though it's a kanji dictionary, that's why GetKanjiResults is being used for the lookup
                        epwingYomichanWordResultsList.Enqueue(GetKanjiResults(text, dict));
                        break;

                    case DictType.CustomWordDictionary:
                        customWordResults = GetWordResults(textList, textInHiraganaList,
                            deconjugationResultsList, dict);
                        break;

                    case DictType.CustomNameDictionary:
                        customNameResults = GetNameResults(textList, textInHiraganaList, dict);
                        break;

                    case DictType.NonspecificKanjiYomichan:
                        epwingYomichanKanjiResultsList.Enqueue(GetKanjiResults(text, dict));
                        break;

                    case DictType.NonspecificNameYomichan:
                        epwingYomichanNameResultsList.Enqueue(GetNameResults(textList, textInHiraganaList, dict));
                        break;

                    case DictType.Kenkyuusha:
                    case DictType.Daijirin:
                    case DictType.Daijisen:
                    case DictType.Koujien:
                    case DictType.Meikyou:
                    case DictType.Gakken:
                    case DictType.Kotowaza:
                    case DictType.IwanamiYomichan:
                    case DictType.JitsuyouYomichan:
                    case DictType.ShinmeikaiYomichan:
                    case DictType.NikkokuYomichan:
                    case DictType.ShinjirinYomichan:
                    case DictType.OubunshaYomichan:
                    case DictType.ZokugoYomichan:
                    case DictType.WeblioKogoYomichan:
                    case DictType.GakkenYojijukugoYomichan:
                    case DictType.ShinmeikaiYojijukugoYomichan:
                    case DictType.KireiCakeYomichan:
                    case DictType.NonspecificWordYomichan:
                    case DictType.NonspecificYomichan:
                        epwingYomichanWordResultsList.Enqueue(GetWordResults(textList, textInHiraganaList,
                            deconjugationResultsList, dict));
                        break;

                    case DictType.NonspecificKanjiNazeka:
                        epwingNazekaKanjiResultsList.Enqueue(GetNameResults(textList, textInHiraganaList, dict));
                        break;

                    case DictType.NonspecificNameNazeka:
                        epwingNazekaNameResultsList.Enqueue(GetNameResults(textList, textInHiraganaList, dict));
                        break;

                    case DictType.DaijirinNazeka:
                    case DictType.KenkyuushaNazeka:
                    case DictType.ShinmeikaiNazeka:
                    case DictType.NonspecificWordNazeka:
                    case DictType.NonspecificNazeka:
                        epwingNazekaWordResultsList.Enqueue(GetWordResults(textList, textInHiraganaList, deconjugationResultsList, dict));
                        break;

                    case DictType.PitchAccentYomichan:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                }
            }
        });

        if (jmdictResults.Count > 0)
        {
            lookupResults.AddRange(BuildJmdictResult(jmdictResults));
        }

        if (jmnedictResults.Count > 0)
        {
            lookupResults.AddRange(BuildJmnedictResult(jmnedictResults));
        }

        if (kanjidicResults.Count > 0)
        {
            lookupResults.AddRange(BuildKanjidicResult(kanjidicResults));
        }

        if (customWordResults.Count > 0)
        {
            lookupResults.AddRange(BuildCustomWordResult(customWordResults));
        }

        if (customNameResults.Count > 0)
        {
            lookupResults.AddRange(BuildCustomNameResult(customNameResults));
        }

        foreach (Dictionary<string, IntermediaryResult> result in epwingYomichanWordResultsList)
        {
            lookupResults.AddRange(BuildEpwingYomichanResult(result));
        }

        foreach (Dictionary<string, IntermediaryResult> result in epwingYomichanKanjiResultsList)
        {
            lookupResults.AddRange(BuildYomichanKanjiResult(result));
        }

        foreach (Dictionary<string, IntermediaryResult> result in epwingYomichanNameResultsList)
        {
            lookupResults.AddRange(BuildEpwingYomichanResult(result));
        }

        foreach (Dictionary<string, IntermediaryResult> result in epwingNazekaWordResultsList)
        {
            lookupResults.AddRange(BuildEpwingNazekaResult(result));
        }

        foreach (Dictionary<string, IntermediaryResult> result in epwingNazekaNameResultsList)
        {
            lookupResults.AddRange(BuildEpwingNazekaResult(result));
        }

        foreach (Dictionary<string, IntermediaryResult> result in epwingNazekaKanjiResultsList)
        {
            lookupResults.AddRange(BuildEpwingNazekaResult(result));
        }

        return lookupResults.Count > 0
            ? SortLookupResults(lookupResults)
            : null;
    }

    private static List<LookupResult> SortLookupResults(IReadOnlyCollection<LookupResult> lookupResults)
    {
        string longestMatchedText = lookupResults.Aggregate(static (r1, r2) => r1.MatchedText.Length > r2.MatchedText.Length ? r1 : r2).MatchedText;

        return lookupResults
            .OrderByDescending(dict => longestMatchedText == dict.PrimarySpelling)
            .ThenByDescending(dict => dict.Readings?.Contains(longestMatchedText) ?? false)
            .ThenByDescending(static dict => dict.MatchedText.Length)
            .ThenByDescending(dict => longestMatchedText.Length >= dict.PrimarySpelling.Length && longestMatchedText[..dict.PrimarySpelling.Length] == dict.PrimarySpelling)
            .ThenByDescending(static dict => dict.PrimarySpelling.Length)
            .ThenBy(static dict => dict.Dict.Priority)
            .ThenBy(static dict => dict.Frequencies?.Count > 0 ? dict.Frequencies[0].Freq : int.MaxValue)
            .ThenBy(static dict =>
            {
                int index = dict.Readings?.IndexOf(dict.MatchedText) ?? -1;
                return index is not -1
                    ? index
                    : int.MaxValue;
            })
            .ToList();
    }

    private static (bool tryLongVowelConversion, int succAttempt) GetWordResultsHelper(Dict dict,
        Dictionary<string, IntermediaryResult> results,
        HashSet<Form> deconjugationList,
        string matchedText,
        string textInHiragana,
        int succAttempt)
    {
        Dictionary<string, IList<IDictRecord>> dictionary = dict.Contents;

        bool tryLongVowelConversion = true;

        if (dictionary.TryGetValue(textInHiragana, out IList<IDictRecord>? tempResult))
        {
            _ = results.TryAdd(textInHiragana,
                new IntermediaryResult(new List<IList<IDictRecord>> { tempResult }, null, matchedText, matchedText,
                    dict));
            tryLongVowelConversion = false;
        }

        if (succAttempt < 3)
        {
            foreach (Form deconjugationResult in deconjugationList)
            {
                string lastTag = "";
                if (deconjugationResult.Tags.Count > 0)
                {
                    lastTag = deconjugationResult.Tags.Last();
                }

                if (dictionary.TryGetValue(deconjugationResult.Text, out IList<IDictRecord>? dictResults))
                {
                    List<IDictRecord> resultsList = new();

                    switch (dict.Type)
                    {
                        case DictType.JMdict:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    JmdictRecord dictResult = (JmdictRecord)dictResults[i];

                                    if (deconjugationResult.Tags.Count is 0
                                        || dictResult.WordClasses.SelectMany(static pos => pos).Contains(lastTag))
                                    {
                                        resultsList.Add(dictResult);
                                    }
                                }
                            }
                            break;

                        case DictType.CustomWordDictionary:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    CustomWordRecord dictResult = (CustomWordRecord)dictResults[i];

                                    if (deconjugationResult.Tags.Count is 0 ||
                                        dictResult.WordClasses.Contains(lastTag))
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
                        case DictType.IwanamiYomichan:
                        case DictType.JitsuyouYomichan:
                        case DictType.ShinmeikaiYomichan:
                        case DictType.NikkokuYomichan:
                        case DictType.ShinjirinYomichan:
                        case DictType.OubunshaYomichan:
                        case DictType.ZokugoYomichan:
                        case DictType.WeblioKogoYomichan:
                        case DictType.GakkenYojijukugoYomichan:
                        case DictType.ShinmeikaiYojijukugoYomichan:
                        case DictType.KanjigenYomichan:
                        case DictType.KireiCakeYomichan:
                        case DictType.NonspecificWordYomichan:
                        case DictType.NonspecificKanjiYomichan:
                        case DictType.NonspecificNameYomichan:
                        case DictType.NonspecificYomichan:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    EpwingYomichanRecord dictResult = (EpwingYomichanRecord)dictResults[i];

                                    if (deconjugationResult.Tags.Count is 0 ||
                                        (dictResult.WordClasses?.Contains(lastTag) ?? false))
                                    {
                                        resultsList.Add(dictResult);
                                    }

                                    else if (DictUtils.WordClassDictionary.TryGetValue(deconjugationResult.Text,
                                                 out IList<JmdictWordClass>? jmdictWcResults))
                                    {
                                        for (int j = 0; j < jmdictWcResults.Count; j++)
                                        {
                                            JmdictWordClass jmdictWordClassResult = jmdictWcResults[j];

                                            if (dictResult.PrimarySpelling == jmdictWordClassResult.Spelling
                                                && (jmdictWordClassResult.Readings?.Contains(dictResult.Reading ?? string.Empty)
                                                    ?? string.IsNullOrEmpty(dictResult.Reading)))
                                            {
                                                if (jmdictWordClassResult.WordClasses.Contains(lastTag))
                                                {
                                                    resultsList.Add(dictResult);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case DictType.DaijirinNazeka:
                        case DictType.KenkyuushaNazeka:
                        case DictType.ShinmeikaiNazeka:
                        case DictType.NonspecificWordNazeka:
                        case DictType.NonspecificKanjiNazeka:
                        case DictType.NonspecificNameNazeka:
                        case DictType.NonspecificNazeka:
                            {
                                int dictResultsCount = dictResults.Count;
                                for (int i = 0; i < dictResultsCount; i++)
                                {
                                    EpwingNazekaRecord dictResult = (EpwingNazekaRecord)dictResults[i];

                                    if (deconjugationResult.Tags.Count is 0)
                                    {
                                        resultsList.Add(dictResult);
                                    }

                                    else if (DictUtils.WordClassDictionary.TryGetValue(deconjugationResult.Text,
                                                 out IList<JmdictWordClass>? jmdictWcResults))
                                    {
                                        for (int j = 0; j < jmdictWcResults.Count; j++)
                                        {
                                            JmdictWordClass jmdictWordClassResult = jmdictWcResults[j];

                                            if (dictResult.PrimarySpelling == jmdictWordClassResult.Spelling
                                                && (jmdictWordClassResult.Readings?.Contains(dictResult.Reading ?? "")
                                                    ?? string.IsNullOrEmpty(dictResult.Reading)))
                                            {
                                                if (jmdictWordClassResult.WordClasses.Contains(lastTag))
                                                {
                                                    resultsList.Add(dictResult);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case DictType.PitchAccentYomichan:
                        case DictType.JMnedict:
                        case DictType.Kanjidic:
                        case DictType.CustomNameDictionary:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                    }

                    if (resultsList.Count > 0)
                    {
                        if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult? r))
                        {
                            if (r.MatchedText == deconjugationResult.OriginalText)
                            {
                                int index = r.Results.FindIndex(rs => rs.SequenceEqual(resultsList));
                                if (index is not -1)
                                {
                                    //if (!r.Processes?[index].Any(p => p.SequenceEqual(deconjugationResult.Process)) ?? false)
                                    r.Processes?[index].Add(deconjugationResult.Process);
                                }
                                else
                                {
                                    r.Results.Add(resultsList);
                                    r.Processes?.Add(new List<List<string>> { deconjugationResult.Process });
                                }
                            }
                        }
                        else
                        {
                            results.Add(deconjugationResult.Text,
                                new IntermediaryResult(new List<IList<IDictRecord>> { resultsList },
                                    new List<List<List<string>>> { new() { deconjugationResult.Process } },
                                    matchedText,
                                    deconjugationResult.Text,
                                    dict)
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

    private static Dictionary<string, IntermediaryResult> GetWordResults(IReadOnlyList<string> textList,
        IReadOnlyList<string> textInHiraganaList, IReadOnlyList<HashSet<Form>> deconjugationResultsList, Dict dict)
    {
        Dictionary<string, IntermediaryResult> results = new();

        int succAttempt = 0;

        for (int i = 0; i < textList.Count; i++)
        {
            (bool tryLongVowelConversion, succAttempt) = GetWordResultsHelper(dict, results,
                deconjugationResultsList[i], textList[i], textInHiraganaList[i], succAttempt);

            if (tryLongVowelConversion && textInHiraganaList[i].Contains('ー', StringComparison.Ordinal) &&
                textInHiraganaList[i][0] is not 'ー')
            {
                List<string> textWithoutLongVowelMarkList = JapaneseUtils.LongVowelMarkToKana(textInHiraganaList[i]);

                for (int j = 0; j < textWithoutLongVowelMarkList.Count; j++)
                {
                    succAttempt = GetWordResultsHelper(dict, results, deconjugationResultsList[i],
                        textList[i], textWithoutLongVowelMarkList[j], succAttempt).succAttempt;
                }
            }
        }

        return results;
    }

    private static Dictionary<string, IntermediaryResult> GetNameResults(IReadOnlyList<string> textList,
        IReadOnlyList<string> textInHiraganaList, Dict dict)
    {
        Dictionary<string, IntermediaryResult> nameResults = new();

        for (int i = 0; i < textList.Count; i++)
        {
            if (dict.Contents
                .TryGetValue(textInHiraganaList[i], out IList<IDictRecord>? result))
            {
                nameResults.Add(textInHiraganaList[i],
                    new IntermediaryResult(new List<IList<IDictRecord>> { result }, null, textList[i], textList[i], dict));
            }
        }

        return nameResults;
    }

    private static Dictionary<string, IntermediaryResult> GetKanjiResults(string text, Dict dict)
    {
        Dictionary<string, IntermediaryResult> kanjiResults = new();

        string kanji = text.EnumerateRunes().FirstOrDefault().ToString();

        if (dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? result))
        {
            kanjiResults.Add(kanji,
                new IntermediaryResult(new List<IList<IDictRecord>> { result }, null, kanji, kanji, dict));
        }

        return kanjiResults;
    }

    private static ConcurrentQueue<LookupResult> BuildJmdictResult(
        Dictionary<string, IntermediaryResult> jmdictResults)
    {
        ConcurrentQueue<LookupResult> results = new();

        _ = Parallel.ForEach(jmdictResults.Values.ToList(), wordResult =>
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    JmdictRecord jMDictResult = (JmdictRecord)wordResult.Results[i][j];

                    string[]?[]? rLists = jMDictResult.ReadingsOrthographyInfo;
                    string[]?[]? aLists = jMDictResult.AlternativeSpellingsOrthographyInfo;
                    List<string?> rOrthographyInfoList = new();
                    List<string?> aOrthographyInfoList = new();

                    for (int k = 0; k < rLists?.Length; k++)
                    {
                        StringBuilder formattedROrthographyInfo = new();

                        string[]? rList = rLists[k];
                        if (rList?.Length > 0)
                        {
                            for (int l = 0; l < rList.Length; l++)
                            {
                                _ = formattedROrthographyInfo.Append(CultureInfo.InvariantCulture, $"{rList[l]}, ");
                            }

                            rOrthographyInfoList.Add(formattedROrthographyInfo.Remove(formattedROrthographyInfo.Length - 2, 2).ToString());
                        }
                        else
                        {
                            rOrthographyInfoList.Add(null);
                        }
                    }

                    for (int k = 0; k < aLists?.Length; k++)
                    {
                        StringBuilder formattedAOrthographyInfo = new();

                        string[]? aList = aLists[k];
                        if (aList?.Length > 0)
                        {
                            for (int l = 0; l < aList.Length; l++)
                            {
                                _ = formattedAOrthographyInfo.Append(CultureInfo.InvariantCulture, $"{aList[l]}, ");
                            }

                            aOrthographyInfoList.Add(formattedAOrthographyInfo.Remove(formattedAOrthographyInfo.Length - 2, 2).ToString());
                        }

                        else
                        {
                            aOrthographyInfoList.Add(null);
                        }
                    }

                    LookupResult result = new
                    (
                        primarySpelling: jMDictResult.PrimarySpelling,
                        readings: jMDictResult.Readings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        edictId: jMDictResult.Id,
                        alternativeSpellings: jMDictResult.AlternativeSpellings,
                        process: ProcessProcess(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(jMDictResult),
                        primarySpellingOrthographyInfoList: jMDictResult.PrimarySpellingOrthographyInfo,
                        readingsOrthographyInfoList: rOrthographyInfoList,
                        alternativeSpellingsOrthographyInfoList: aOrthographyInfoList,
                        dict: wordResult.Dict,
                        formattedDefinitions: jMDictResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Enqueue(result);
                }
            }
        });

        return results;
    }

    private static ConcurrentQueue<LookupResult> BuildJmnedictResult(
        Dictionary<string, IntermediaryResult> jmnedictResults)
    {
        ConcurrentQueue<LookupResult> results = new();

        _ = Parallel.ForEach(jmnedictResults.Values.ToList(), nameResult =>
        {
            int resultsListCount = nameResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = nameResult.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    JmnedictRecord jmnedictRecord = (JmnedictRecord)nameResult.Results[i][j];

                    LookupResult result = new
                    (
                        edictId: jmnedictRecord.Id,
                        primarySpelling: jmnedictRecord.PrimarySpelling,
                        alternativeSpellings: jmnedictRecord.AlternativeSpellings,
                        readings: jmnedictRecord.Readings,
                        matchedText: nameResult.MatchedText,
                        deconjugatedMatchedText: nameResult.DeconjugatedMatchedText,
                        dict: nameResult.Dict,
                        formattedDefinitions: jmnedictRecord.BuildFormattedDefinition(nameResult.Dict.Options)
                    );

                    results.Enqueue(result);
                }
            }
        });

        return results;
    }

    private static List<LookupResult> BuildKanjidicResult(Dictionary<string, IntermediaryResult> kanjiResults)
    {
        List<LookupResult> results = new();

        if (kanjiResults.Count is 0)
        {
            return results;
        }

        KeyValuePair<string, IntermediaryResult> dictResult = kanjiResults.First();

        List<IList<IDictRecord>> iResult = dictResult.Value.Results;
        KanjidicRecord kanjiRecord = (KanjidicRecord)iResult[0][0];

        List<string> allReadings = new();

        if (kanjiRecord.OnReadings is not null)
        {
            allReadings.AddRange(kanjiRecord.OnReadings);
        }

        if (kanjiRecord.KunReadings is not null)
        {
            allReadings.AddRange(kanjiRecord.KunReadings);
        }

        if (kanjiRecord.NanoriReadings is not null)
        {
            allReadings.AddRange(kanjiRecord.NanoriReadings);
        }

        IntermediaryResult intermediaryResult = kanjiResults.First().Value;

        LookupResult result = new
        (
            primarySpelling: dictResult.Key,
            readings: allReadings,
            onReadings: kanjiRecord.OnReadings,
            kunReadings: kanjiRecord.KunReadings,
            nanoriReadings: kanjiRecord.NanoriReadings,
            strokeCount: kanjiRecord.StrokeCount,
            kanjiGrade: kanjiRecord.Grade,
            kanjiComposition: DictUtils.s_kanjiCompositionDict.GetValueOrDefault(dictResult.Key),
            frequencies: GetKanjidicFrequencies(dictResult.Key, kanjiRecord.Frequency),
            matchedText: intermediaryResult.MatchedText,
            deconjugatedMatchedText: intermediaryResult.DeconjugatedMatchedText,
            dict: intermediaryResult.Dict,
            formattedDefinitions: kanjiRecord.BuildFormattedDefinition()
        );

        results.Add(result);
        return results;
    }

    private static ConcurrentQueue<LookupResult> BuildYomichanKanjiResult(
        Dictionary<string, IntermediaryResult> kanjiResults)
    {
        ConcurrentQueue<LookupResult> results = new();

        _ = Parallel.ForEach(kanjiResults.ToList(), kanjiResult =>
        {
            int resultsListCount = kanjiResult.Value.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = kanjiResult.Value.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    YomichanKanjiRecord yomichanKanjiDictResult = (YomichanKanjiRecord)kanjiResult.Value.Results[i][j];

                    List<string> allReadings = new();

                    if (yomichanKanjiDictResult.OnReadings is not null)
                    {
                        allReadings.AddRange(yomichanKanjiDictResult.OnReadings);
                    }

                    if (yomichanKanjiDictResult.KunReadings is not null)
                    {
                        allReadings.AddRange(yomichanKanjiDictResult.KunReadings);
                    }

                    LookupResult result = new
                    (
                        primarySpelling: kanjiResult.Key,
                        readings: allReadings,
                        onReadings: yomichanKanjiDictResult.OnReadings,
                        kunReadings: yomichanKanjiDictResult.KunReadings,
                        kanjiComposition: DictUtils.s_kanjiCompositionDict.GetValueOrDefault(kanjiResult.Key),
                        kanjiStats: yomichanKanjiDictResult.BuildFormattedStats(),
                        frequencies: GetYomichanKanjiFrequencies(kanjiResult.Key),
                        matchedText: kanjiResult.Value.MatchedText,
                        deconjugatedMatchedText: kanjiResult.Value.DeconjugatedMatchedText,
                        dict: kanjiResult.Value.Dict,
                        formattedDefinitions: yomichanKanjiDictResult.BuildFormattedDefinition(kanjiResult.Value.Dict.Options)
                    );
                    results.Enqueue(result);
                }
            }
        });

        return results;
    }

    private static ConcurrentQueue<LookupResult> BuildEpwingYomichanResult(
        Dictionary<string, IntermediaryResult> epwingResults)
    {
        ConcurrentQueue<LookupResult> results = new();

        _ = Parallel.ForEach(epwingResults.Values.ToList(), wordResult =>
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;
                for (int j = 0; j < resultCount; j++)
                {
                    EpwingYomichanRecord epwingResult = (EpwingYomichanRecord)wordResult.Results[i][j];

                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        process: ProcessProcess(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(epwingResult),
                        dict: wordResult.Dict,
                        readings: epwingResult.Reading is not null
                            ? new List<string> { epwingResult.Reading }
                            : null,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Enqueue(result);
                }
            }
        });

        return results;
    }

    private static ConcurrentQueue<LookupResult> BuildEpwingNazekaResult(
        Dictionary<string, IntermediaryResult> epwingNazekaResults)
    {
        ConcurrentQueue<LookupResult> results = new();

        _ = Parallel.ForEach(epwingNazekaResults.Values.ToList(), wordResult =>
        {
            int resultsListCount = wordResult.Results.Count;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = wordResult.Results[i].Count;
                for (int j = 0; j < resultCount; j++)
                {
                    EpwingNazekaRecord epwingResult = (EpwingNazekaRecord)wordResult.Results[i][j];

                    LookupResult result = new
                    (
                        primarySpelling: epwingResult.PrimarySpelling,
                        alternativeSpellings: epwingResult.AlternativeSpellings,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        process: ProcessProcess(wordResult.Processes?[i]),
                        frequencies: GetWordFrequencies(epwingResult),
                        dict: wordResult.Dict,
                        readings: epwingResult.Reading is not null
                            ? new List<string> { epwingResult.Reading }
                            : null,
                        formattedDefinitions: epwingResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Enqueue(result);
                }
            }
        });

        return results;
    }

    private static ConcurrentQueue<LookupResult> BuildCustomWordResult(
        Dictionary<string, IntermediaryResult> customWordResults)
    {
        ConcurrentQueue<LookupResult> results = new();

        _ = Parallel.ForEach(customWordResults.Values.ToList(), wordResult =>
        {
            int wordResultsListCount = wordResult.Results.Count;
            for (int i = 0; i < wordResultsListCount; i++)
            {
                int wordResultCount = wordResult.Results[i].Count;

                for (int j = 0; j < wordResultCount; j++)
                {
                    CustomWordRecord customWordDictResult = (CustomWordRecord)wordResult.Results[i][j];

                    List<LookupFrequencyResult> freqs = GetWordFrequencies(customWordDictResult);
                    for (int k = 0; k < freqs.Count; k++)
                    {
                        LookupFrequencyResult freqResult = freqs[k];

                        if (freqResult.Freq is int.MaxValue)
                        {
                            freqResult.Freq = -i;
                        }
                    }

                    LookupResult result = new
                    (
                        frequencies: freqs,
                        primarySpelling: customWordDictResult.PrimarySpelling,
                        matchedText: wordResult.MatchedText,
                        deconjugatedMatchedText: wordResult.DeconjugatedMatchedText,
                        process: customWordDictResult.HasUserDefinedWordClass
                            ? ProcessProcess(wordResult.Processes?[i])
                            : null,
                        dict: wordResult.Dict,
                        readings: customWordDictResult.Readings,
                        alternativeSpellings: customWordDictResult.AlternativeSpellings,
                        formattedDefinitions: customWordDictResult.BuildFormattedDefinition(wordResult.Dict.Options)
                    );

                    results.Enqueue(result);
                }
            }
        });

        return results;
    }

    private static ConcurrentQueue<LookupResult> BuildCustomNameResult(
        Dictionary<string, IntermediaryResult> customNameResults)
    {
        ConcurrentQueue<LookupResult> results = new();

        _ = Parallel.ForEach(customNameResults.ToList(), customNameResult =>
        {
            int resultsListCount = customNameResult.Value.Results.Count;
            int freq = 0;
            for (int i = 0; i < resultsListCount; i++)
            {
                int resultCount = customNameResult.Value.Results[i].Count;

                for (int j = 0; j < resultCount; j++)
                {
                    CustomNameRecord customNameDictResult = (CustomNameRecord)customNameResult.Value.Results[i][j];
                    LookupResult result = new
                    (
                        primarySpelling: customNameDictResult.PrimarySpelling,
                        matchedText: customNameResult.Value.MatchedText,
                        deconjugatedMatchedText: customNameResult.Value.DeconjugatedMatchedText,
                        frequencies: new List<LookupFrequencyResult> { new(customNameResult.Value.Dict.Name, -freq) },
                        dict: customNameResult.Value.Dict,
                        readings: new List<string> { customNameDictResult.Reading },
                        formattedDefinitions: customNameDictResult.BuildFormattedDefinition()
                    );

                    ++freq;
                    results.Enqueue(result);
                }
            }
        });

        return results;
    }

    private static List<LookupFrequencyResult> GetWordFrequencies(IGetFrequency record)
    {
        List<LookupFrequencyResult> freqsList = new();

        foreach (Freq freq in FreqUtils.FreqDicts.Values.OrderBy(static f => f.Priority).ToList())
        {
            if (freq is { Active: true, Type: not FreqType.YomichanKanji })
            {
                freqsList.Add(new LookupFrequencyResult(freq.Name, record.GetFrequency(freq)));
            }
        }

        return freqsList;
    }

    private static List<LookupFrequencyResult> GetYomichanKanjiFrequencies(string kanji)
    {
        List<LookupFrequencyResult> freqsList = new();

        Freq? kanjiFreq = FreqUtils.FreqDicts.Values.FirstOrDefault(static f => f.Type is FreqType.YomichanKanji);

        if (kanjiFreq?.Active ?? false)
        {
            if (kanjiFreq.Contents.TryGetValue(kanji, out IList<FrequencyRecord>? freqResultList))
            {
                int frequency = freqResultList.FirstOrDefault().Frequency;

                if (frequency is not 0)
                {
                    freqsList.Add(new LookupFrequencyResult(kanjiFreq.Name, frequency));
                }
            }
        }

        return freqsList;
    }

    private static List<LookupFrequencyResult> GetKanjidicFrequencies(string kanji, int frequency)
    {
        List<LookupFrequencyResult> freqsList = new();

        if (frequency is not 0)
        {
            freqsList.Add(new LookupFrequencyResult("Kanjidic Freq", frequency));
        }

        freqsList.AddRange(GetYomichanKanjiFrequencies(kanji));
        return freqsList;
    }

    private static string? ProcessProcess(IReadOnlyList<List<string>>? processList)
    {
        StringBuilder deconjugation = new();
        bool first = true;

        for (int i = 0; i < processList?.Count; i++)
        {
            List<string> form = processList[i];

            StringBuilder formText = new();
            int added = 0;

            for (int j = form.Count - 1; j >= 0; j--)
            {
                string info = form[j];

                if (info is "")
                {
                    continue;
                }

                if (info.StartsWith('(') && info.EndsWith(')') && j is not 0)
                {
                    continue;
                }

                if (added > 0)
                {
                    _ = formText.Append('→');
                }

                ++added;
                _ = formText.Append(info);
            }

            if (formText.Length is not 0)
            {
                _ = first
                    ? deconjugation.Append(CultureInfo.InvariantCulture, $"～{formText}")
                    : deconjugation.Append(CultureInfo.InvariantCulture, $"; {formText}");
            }

            first = false;
        }

        return deconjugation.Length is 0 ? null : deconjugation.ToString();
    }
}
