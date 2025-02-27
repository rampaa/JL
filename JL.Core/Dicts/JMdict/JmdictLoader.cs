using System.Collections.Frozen;
using System.Globalization;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.WordClass;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictLoader
{
    private static bool s_canHandleCulture = true;

    private static readonly FrozenDictionary<string, string> s_iso6392BToEnglishNames = new KeyValuePair<string, string>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreakssme
        new("afr", "Afrikaans"), new("ain", "Ainu"), new("alb", "Albanian"),
        new("alg", "Algonquian languages"), new("amh", "Amharic"), new("ara", "Arabic"),
        new("arm", "Armenian"), new("arn", "Mapuche"), new("baq", "Basque"),
        new("bnt", "Bantu languages"), new("bre", "Breton"), new("bul", "Bulgarian"),
        new("bur", "Burmese"), new("chi", "Chinese"), new("chn", "Chinook Jargon"),
        new("cze", "Czech"), new("dan", "Danish"), new("dut", "Dutch"),
        new("eng", "English"), new("epo", "Esperanto"), new("est", "Estonian"),
        new("fil", "Filipino"), new("fin", "Finnish"), new("fre", "French"),
        new("geo", "Georgian"), new("ger", "German"), new("glg", "Galician"),
        new("grc", "Ancient Greek"), new("gre", "Greek"), new("haw", "Hawaiian"),
        new("heb", "Hebrew"), new("hin", "Hindi"), new("hun", "Hungarian"),
        new("ice", "Icelandic"), new("ind", "Indonesian"), new("ita", "Italian"),
        new("khm", "Khmer"), new("kor", "Korean"), new("kur", "Kurdish"),
        new("lat", "Latin"), new("lit", "Lithuanian"), new("mac", "mkd"),
        new("mal", "Malayalam"), new("mao", "Maori"), new("may", "Malay"),
        new("mnc", "Manchu"), new("mol", "Moldovan"), new("mon", "Mongolian"),
        new("nor", "Norwegian"), new("per", "Persian"), new("pol", "Polish"),
        new("por", "Portuguese"), new("rum", "Romanian"), new("rus", "Russian"),
        new("san", "Sanskrit"), new("scr", "Serbo-Croatian"), new("slo", "Slovak"),
        new("slv", "Slovenian"), new("som", "Somali"), new("spa", "Spanish"),
        new("swa", "Kiswahili"), new("swe", "Swedish"), new("tah", "Tahitian"),
        new("tam", "Tamil"), new("tgl", "Tagalog"), new("tha", "Thai"),
        new("tib", "Tibetan"), new("tur", "Turkish"), new("ukr", "Ukrainian"),
        new("urd", "Urdu"), new("uzb", "Uzbek"), new("vie", "Vietnamese"),
        new("wel", "Welsh"), new("yid", "Yiddish")
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (File.Exists(fullPath))
        {
            DictUtils.JmdictEntities.Clear();

            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)

            using (XmlTextReader xmlReader = new(fullPath))
            {
                xmlReader.DtdProcessing = DtdProcessing.Parse;
                xmlReader.WhitespaceHandling = WhitespaceHandling.None;
                xmlReader.EntityHandling = EntityHandling.ExpandCharEntities;

                while (xmlReader.ReadToFollowing("entry"))
                {
                    JmdictRecordBuilder.AddToDictionary(ReadEntry(xmlReader), dict.Contents);
                }
            }

            foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
            {
                dict.Contents[key] = recordList.ToArray();
            }

            dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
        }

        else
        {
            if (DictUtils.UpdatingJmdict)
            {
                return;
            }

            DictUtils.UpdatingJmdict = true;
            if (Utils.Frontend.ShowYesNoDialog(
                "Couldn't find JMdict.xml. Would you like to download it now?",
                "Download JMdict?"))
            {
                bool downloaded = await DictUpdater.DownloadDict(fullPath,
                    DictUtils.s_jmdictUrl,
                    DictType.JMdict.ToString(), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    await Load(dict).ConfigureAwait(false);

                    await JmdictWordClassUtils.Serialize().ConfigureAwait(false);
                    await JmdictWordClassUtils.Load().ConfigureAwait(false);
                }
            }
            else
            {
                dict.Active = false;
            }

            DictUtils.UpdatingJmdict = false;
        }
    }

    private static JmdictEntry ReadEntry(XmlTextReader xmlReader)
    {
        int id = 0;
        List<KanjiElement> kanjiElements = [];
        List<ReadingElement> readingElements = [];
        List<Sense> senseList = [];

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
                        id = xmlReader.ReadElementContentAsInt();
                        break;

                    case "k_ele":
                        kanjiElements.Add(ReadKanjiElement(xmlReader));
                        break;

                    case "r_ele":
                        readingElements.Add(ReadReadingElement(xmlReader));
                        break;

                    case "sense":
                        senseList.Add(ReadSense(xmlReader));
                        break;

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

        return new JmdictEntry(id, kanjiElements, readingElements, senseList);
    }

    private static KanjiElement ReadKanjiElement(XmlTextReader xmlReader)
    {
        string keb = "";
        List<string> keInfList = [];

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

        return new KanjiElement(keb, keInfList);
    }

    private static ReadingElement ReadReadingElement(XmlTextReader xmlReader)
    {
        string reb = "";
        List<string> reRestrList = [];
        List<string> reInfList = [];

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
                        reRestrList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    case "re_inf":
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

        return new ReadingElement(reb, reRestrList, reInfList);
    }

    private static Sense ReadSense(XmlTextReader xmlReader)
    {
        string? sInf = null;
        List<string> stagKList = [];
        List<string> stagRList = [];
        List<string> posList = [];
        List<string> fieldList = [];
        List<string> miscList = [];
        List<string> dialList = [];
        List<string> glossList = [];
        List<string> xRefList = [];
        List<string> antList = [];
        List<LoanwordSource> lSourceList = [];

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
                        stagKList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    case "stagr":
                        stagRList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    case "pos":
                        posList.Add(ReadEntity(xmlReader));
                        break;

                    case "field":
                        fieldList.Add(ReadEntity(xmlReader));
                        break;

                    case "misc":
                        miscList.Add(ReadEntity(xmlReader));
                        break;

                    case "s_inf":
                        sInf = xmlReader.ReadElementContentAsString();
                        break;

                    case "dial":
                        dialList.Add(ReadEntity(xmlReader));
                        break;

                    case "gloss":
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

                    case "xref":
                        xRefList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    case "ant":
                        antList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    case "lsource":
                        string? lang = xmlReader.GetAttribute("xml:lang");

                        if (lang is not null)
                        {
                            if (s_iso6392BToEnglishNames.TryGetValue(lang, out string? englishName))
                            {
                                lang = englishName;
                            }

                            else if (s_canHandleCulture)
                            {
                                Utils.Logger.Error($"JMdict: English name of {lang} is missing!");

                                try
                                {
                                    lang = CultureInfo.GetCultureInfo(lang).EnglishName;
                                }
                                catch (CultureNotFoundException ex)
                                {
                                    Utils.Logger.Error(ex, "Underlying OS cannot process the culture info");
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

                        lSourceList.Add(new LoanwordSource(lang.GetPooledString(), isPart, isWasei, originalWord));
                        break;

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

        return new Sense(sInf, stagKList, stagRList, posList, fieldList, miscList, dialList, glossList, xRefList, antList, lSourceList);
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
