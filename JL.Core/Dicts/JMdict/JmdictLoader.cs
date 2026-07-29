using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;
using JL.Core.WordClass;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictLoader
{
    private static bool s_canHandleCulture = true;

    // 2022/05/11: 394949, 2022/08/15: 398303, 2023/04/22: 403739, 2023/12/16: 419334, 2024/02/22: 421519
    public const int Size = 421519;

    private static readonly FrozenDictionary<string, string> s_iso6392BToEnglishNames = new KeyValuePair<string, string>[]
    {
        new("afr", "Afrikaans"),
        new("ain", "Ainu"),
        new("alb", "Albanian"),
        new("alg", "Algonquian languages"),
        new("amh", "Amharic"),
        new("ara", "Arabic"),
        new("arm", "Armenian"),
        new("arn", "Mapuche"),
        new("aze", "Azerbaijani"),
        new("baq", "Basque"),
        new("ben", "Bengali"),
        new("bnt", "Bantu languages"),
        new("bre", "Breton"),
        new("bul", "Bulgarian"),
        new("bur", "Burmese"),
        new("chi", "Chinese"),
        new("chn", "Chinook Jargon"),
        new("cze", "Czech"),
        new("dan", "Danish"),
        new("div", "Dhivehi"),
        new("dut", "Dutch"),
        new("eng", "English"),
        new("epo", "Esperanto"),
        new("est", "Estonian"),
        new("fil", "Filipino"),
        new("fin", "Finnish"),
        new("fre", "French"),
        new("geo", "Georgian"),
        new("ger", "German"),
        new("glg", "Galician"),
        new("grc", "Ancient Greek"),
        new("gre", "Greek"),
        new("haw", "Hawaiian"),
        new("heb", "Hebrew"),
        new("hin", "Hindi"),
        new("hun", "Hungarian"),
        new("ice", "Icelandic"),
        new("ind", "Indonesian"),
        new("ita", "Italian"),
        new("kaz", "Kazakh"),
        new("khm", "Central Khmer"),
        new("kir", "Kyrgyz"),
        new("kor", "Korean"),
        new("kur", "Kurdish"),
        new("lao", "Lao"),
        new("lat", "Latin"),
        new("lit", "Lithuanian"),
        new("mac", "Macedonian"),
        new("mal", "Malayalam"),
        new("mao", "Maori"),
        new("may", "Malay"),
        new("mlg", "Malagasy"),
        new("mnc", "Manchu"),
        new("mol", "Moldavian"),
        new("mon", "Mongolian"),
        new("nep", "Nepali"),
        new("nor", "Norwegian"),
        new("per", "Persian"),
        new("pol", "Polish"),
        new("por", "Portuguese"),
        new("rum", "Romanian"),
        new("rus", "Russian"),
        new("san", "Sanskrit"),
        new("scr", "Serbo-Croatian"),
        new("slo", "Slovak"),
        new("slv", "Slovenian"),
        new("smo", "Samoan"),
        new("som", "Somali"),
        new("sot", "Southern Sotho"),
        new("spa", "Spanish"),
        new("swa", "Swahili"),
        new("swe", "Swedish"),
        new("tah", "Tahitian"),
        new("tam", "Tamil"),
        new("tgk", "Tajik"),
        new("tgl", "Tagalog"),
        new("tha", "Thai"),
        new("tib", "Tibetan"),
        new("tuk", "Turkmen"),
        new("tur", "Turkish"),
        new("ukr", "Ukrainian"),
        new("urd", "Urdu"),
        new("uzb", "Uzbek"),
        new("vie", "Vietnamese"),
        new("wel", "Welsh"),
        new("yid", "Yiddish")
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (File.Exists(fullPath))
        {
            DictUtils.JmdictEntities.Clear();

            // ReSharper disable once UseAwaitUsing
            using (FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_syncRead64KBufferFso))
            {
                // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
                // And we do need EntityHandling property because we want to get unexpanded entity names
                // The downside of using XmlTextReader is that it does not support async methods
                // And we cannot set some settings (e.g. MaxCharactersFromEntities)
                using XmlTextReader xmlReader = new(fileStream);
                xmlReader.DtdProcessing = DtdProcessing.Parse;
                xmlReader.WhitespaceHandling = WhitespaceHandling.None;
                xmlReader.EntityHandling = EntityHandling.ExpandCharEntities;

                ProperNameEntriesOption? properNamesEntriesOption = dict.Options.ProperNameEntries;
                Debug.Assert(properNamesEntriesOption is not null);
                bool includeProperNames = properNamesEntriesOption.Value;

                GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
                Debug.Assert(generateMazegakiOption is not null);
                bool generateMazegaki = generateMazegakiOption.Value;

                GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
                Debug.Assert(generateFusejiVariantsOption is not null);
                bool generateFusejiVariants = generateFusejiVariantsOption.Value;

                int maxSearchKeyLengthForFusejiGeneration;
                int maxTotalFuseji;
                int maxConsecutiveFuseji;
                if (generateFusejiVariants)
                {
                    Debug.Assert(dict.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
                    maxSearchKeyLengthForFusejiGeneration = dict.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

                    Debug.Assert(dict.Options.MaxTotalFusejiCount is not null);
                    maxTotalFuseji = dict.Options.MaxTotalFusejiCount.Value;

                    Debug.Assert(dict.Options.MaxConsecutiveFusejiCount is not null);
                    maxConsecutiveFuseji = dict.Options.MaxConsecutiveFusejiCount.Value;
                }
                else
                {
                    maxSearchKeyLengthForFusejiGeneration = 0;
                    maxTotalFuseji = 0;
                    maxConsecutiveFuseji = 0;
                }

                IDictionary<string, IList<IDictRecord>> jmdictDictionary = dict.Contents;
                while (xmlReader.ReadToFollowing("entry"))
                {
                    Dictionary<string, JmdictRecord>? recordDictionary = JmdictRecordBuilder.GetRecordsFromEntry(ReadEntry(xmlReader), includeProperNames);
                    if (recordDictionary is not null)
                    {
                        foreach ((string key, JmdictRecord record) in recordDictionary)
                        {
                            if (jmdictDictionary.TryGetValue(key, out IList<IDictRecord>? records))
                            {
                                records.Add(record);
                            }
                            else
                            {
                                jmdictDictionary[key] = [record];
                            }

                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(key, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    if (!recordDictionary.ContainsKey(fusejiVariant))
                                    {
                                        _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, jmdictDictionary);
                                    }
                                }
                            }

                            if (generateMazegaki && record.Readings is not null)
                            {
                                foreach (string reading in record.Readings)
                                {
                                    string readingInHiragana = JapaneseUtils.NormalizeText(reading);
                                    if (readingInHiragana != key)
                                    {
                                        foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(key, readingInHiragana))
                                        {
                                            if (!recordDictionary.ContainsKey(mazegaki))
                                            {
                                                if (DictUtils.AddRecordToDictionary(mazegaki, record, jmdictDictionary))
                                                {
                                                    if (generateFusejiVariants)
                                                    {
                                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                        {
                                                            if (!recordDictionary.ContainsKey(fusejiVariant))
                                                            {
                                                                _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, jmdictDictionary);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
        }
        else
        {
            if (dict.Updating)
            {
                return;
            }

            dict.Updating = true;
            if (await FrontendManager.Frontend.ShowYesNoDialogAsync(
                "Couldn't find JMdict.xml. Would you like to download it now?",
                "Download JMdict?").ConfigureAwait(false))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.JMdict), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    try
                    {
                        await Load(dict).ConfigureAwait(false);
                        await JmdictWordClassUtils.Serialize().ConfigureAwait(false);
                        await JmdictWordClassUtils.Load().ConfigureAwait(false);
                    }
                    finally
                    {
                        dict.Updating = false;
                    }
                }
                else
                {
                    dict.Updating = false;
                }
            }
            else
            {
                dict.Active = false;
                dict.Updating = false;
            }
        }
    }

    public static JmdictEntry ReadEntry(XmlTextReader xmlReader)
    {
        int id = 0;
        List<KanjiElement> kanjiElements = [];
        List<ReadingElement> readingElements = [];
        List<Sense> senseList = [];
        List<LoanwordSource>? lSourceList = null;
        List<string>? infoList = null;

        _ = xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "entry", NodeType: XmlNodeType.EndElement })
            {
                break;
            }

            if (xmlReader.NodeType is XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "ent_seq":
                    {
                        id = xmlReader.ReadElementContentAsInt();
                        break;
                    }

                    case "k_ele":
                    {
                        kanjiElements.Add(ReadKanjiElement(xmlReader));
                        break;
                    }

                    case "r_ele":
                    {
                        readingElements.Add(ReadReadingElement(xmlReader));
                        break;
                    }

                    case "sense":
                    {
                        senseList.Add(ReadSense(xmlReader));
                        break;
                    }

                    case "info":
                    {
                        infoList ??= [];

                        //string info = "";
                        //if (xmlReader.HasAttributes)
                        //{
                        //    string? infoType = xmlReader.GetAttribute("inf_type");
                        //    if (infoType is not null)
                        //    {
                        //        // Currently, the only info type in JMdict is "note".
                        //        // We should revisit this in the future to see if the $"{infoType}: " format looks good
                        //        if (infoType is not "note")
                        //        {
                        //            info = $"{infoType}: ";
                        //        }
                        //    }
                        //}

                        //info += xmlReader.ReadElementContentAsString();
                        infoList.Add(xmlReader.ReadElementContentAsString());
                        break;
                    }

                    case "lsource":
                    {
                        string? lang = xmlReader.GetAttribute("xml:lang");

                        if (lang is not null)
                        {
                            if (s_iso6392BToEnglishNames.TryGetValue(lang, out string? englishName))
                            {
                                lang = englishName;
                            }

                            else if (s_canHandleCulture)
                            {
                                LoggerManager.Logger.Error("JMdict: English name of {Lang} is missing!", lang);

                                try
                                {
                                    lang = CultureInfo.GetCultureInfo(lang).EnglishName;
                                }
                                catch (CultureNotFoundException ex)
                                {
                                    LoggerManager.Logger.Error(ex, "Underlying OS cannot process the culture info for {LanguageCode}", lang);
                                    s_canHandleCulture = false;
                                }
                            }
                        }

                        else
                        {
                            lang = "English";
                        }

                        bool isPart = xmlReader.GetAttribute("ls_type") is "part";
                        bool isWasei = xmlReader.GetAttribute("ls_wasei") is not null;

                        string? originalWord = xmlReader.ReadElementContentAsString();
                        originalWord = originalWord.Length > 0 ? originalWord : null;

                        lSourceList ??= [];
                        lSourceList.Add(new LoanwordSource(lang.GetPooledString(), isPart, isWasei, originalWord));
                        break;
                    }

                    default:
                        _ = xmlReader.Read();
                        break;
                }
            }
            else
            {
                _ = xmlReader.Read();
            }
        }

        return new JmdictEntry(id, kanjiElements, readingElements, senseList, lSourceList?.ToArray(), infoList?.ToArray());
    }

    private static KanjiElement ReadKanjiElement(XmlTextReader xmlReader)
    {
        string keb = "";
        List<string>? keInfList = null;

        _ = xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "k_ele", NodeType: XmlNodeType.EndElement })
            {
                break;
            }

            if (xmlReader.NodeType is XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "keb":
                        keb = xmlReader.ReadElementContentAsString().GetPooledString();
                        break;

                    case "ke_inf":
                        keInfList ??= [];
                        keInfList.Add(ReadEntity(xmlReader));
                        break;

                    //case "ke_pri":
                    //    kanjiElement.KePriList.Add(xmlReader.ReadElementContentAsString());
                    //    break;

                    default:
                        _ = xmlReader.Read();
                        break;
                }
            }

            else
            {
                _ = xmlReader.Read();
            }
        }

        return new KanjiElement(keb, keInfList?.ToArray());
    }

    private static ReadingElement ReadReadingElement(XmlTextReader xmlReader)
    {
        string reb = "";
        List<string>? reRestrList = null;
        List<string>? reInfList = null;

        _ = xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "r_ele", NodeType: XmlNodeType.EndElement })
            {
                break;
            }

            if (xmlReader.NodeType is XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "reb":
                        reb = xmlReader.ReadElementContentAsString().GetPooledString();
                        break;

                    case "re_restr":
                        reRestrList ??= [];
                        reRestrList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    case "re_inf":
                        reInfList ??= [];
                        reInfList.Add(ReadEntity(xmlReader));
                        break;

                    //case "re_pri":
                    //    readingElement.RePriList.Add(xmlReader.ReadElementContentAsString());
                    //    break;

                    default:
                        _ = xmlReader.Read();
                        break;
                }
            }

            else
            {
                _ = xmlReader.Read();
            }
        }

        return new ReadingElement(reb, reRestrList, reInfList?.ToArray());
    }

    private static Sense ReadSense(XmlTextReader xmlReader)
    {
        List<string> glossList = [];
        List<string> posList = [];
        string? sInf = null;
        List<string>? stagKList = null;
        List<string>? stagRList = null;
        List<string>? fieldList = null;
        List<string>? miscList = null;
        List<string>? dialList = null;
        List<string>? xRefList = null;

        _ = xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "sense", NodeType: XmlNodeType.EndElement })
            {
                break;
            }

            if (xmlReader.NodeType is XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "stagk":
                    {
                        stagKList ??= [];
                        stagKList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;
                    }

                    case "stagr":
                    {
                        stagRList ??= [];
                        stagRList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;
                    }

                    case "pos":
                    {
                        posList.Add(ReadEntity(xmlReader));
                        break;
                    }

                    case "field":
                    {
                        fieldList ??= [];
                        fieldList.Add(ReadEntity(xmlReader));
                        break;
                    }

                    case "misc":
                    {
                        miscList ??= [];
                        miscList.Add(ReadEntity(xmlReader));
                        break;
                    }

                    case "s_inf":
                    {
                        sInf = xmlReader.ReadElementContentAsString();
                        break;
                    }

                    case "dial":
                    {
                        dialList ??= [];
                        dialList.Add(ReadEntity(xmlReader));
                        break;
                    }

                    case "gloss":
                    {
                        string gloss = "";
                        if (xmlReader.HasAttributes)
                        {
                            string? glossType = xmlReader.GetAttribute("g_type");
                            if (glossType is not null)
                            {
                                gloss = $"({glossType}.) ";
                            }
                        }

                        gloss += xmlReader.ReadElementContentAsString();

                        glossList.Add(gloss);
                        break;
                    }

                    case "xref":
                    {
                        xRefList ??= [];

                        string crossReference = "";
                        if (xmlReader.HasAttributes)
                        {
                            string? crossReferenceType = xmlReader.GetAttribute("type");
                            if (crossReferenceType is not null)
                            {
                                crossReference = crossReferenceType switch
                                {
                                    "see" => "see: ",
                                    "ant" => "antonym: ",
                                    "syn" => "synonym: ",
                                    _ => $"{crossReferenceType}: "
                                };
                            }
                        }

                        crossReference += xmlReader.ReadElementContentAsString();

                        xRefList.Add(crossReference);
                        break;
                    }

                    default:
                    {
                        _ = xmlReader.Read();
                        break;
                    }
                }
            }

            else
            {
                _ = xmlReader.Read();
            }
        }

        return new Sense(glossList.ToArray(), posList.TrimToArray(), sInf, stagKList?.ToArray(), stagRList?.ToArray(), fieldList?.ToArray(), miscList?.ToArray(), dialList?.ToArray(), xRefList?.ToArray());
    }

    private static string ReadEntity(XmlTextReader xmlReader)
    {
        _ = xmlReader.Read();

        string entityName = xmlReader.Name.GetPooledString();

        if (!DictUtils.JmdictEntities.ContainsKey(entityName))
        {
            xmlReader.ResolveEntity();
            _ = xmlReader.Read();

            DictUtils.JmdictEntities.Add(entityName, xmlReader.Value.GetPooledString());
        }

        _ = xmlReader.Read();

        return entityName;
    }
}
