using System.Globalization;
using System.Xml;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.JMdict;

public static class JmdictLoader
{
    private static bool s_canHandleCulture = true;

    public static async Task Load(Dict dict)
    {
        if (File.Exists(dict.Path))
        {
            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)

            using XmlReader xmlReader = new XmlTextReader(dict.Path)
            {
                DtdProcessing = DtdProcessing.Parse,
                WhitespaceHandling = WhitespaceHandling.None,
                EntityHandling = EntityHandling.ExpandCharEntities
            };

            while (xmlReader.ReadToFollowing("entry"))
            {
                JmdictRecordBuilder.AddToDictionary(ReadEntry(xmlReader), dict.Contents);
            }

            dict.Contents.TrimExcess();
        }

        else if (Storage.Frontend.ShowYesNoDialog(
                     "Couldn't find JMdict.xml. Would you like to download it now?",
                     "Download JMdict?"))
        {
            _ = await ResourceUpdater.UpdateResource(dict.Path,
                Storage.JmdictUrl,
                DictType.JMdict.ToString(), false, false).ConfigureAwait(false);
            await Load(dict).ConfigureAwait(false);
        }

        else
        {
            dict.Active = false;
        }
    }

    private static JmdictEntry ReadEntry(XmlReader xmlReader)
    {
        JmdictEntry entry = new();

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
                        entry.Id = xmlReader.ReadElementContentAsInt();
                        break;

                    case "k_ele":
                        entry.KanjiElements.Add(ReadKanjiElement(xmlReader));
                        break;

                    case "r_ele":
                        entry.ReadingElements.Add(ReadReadingElement(xmlReader));
                        break;

                    case "sense":
                        entry.SenseList.Add(ReadSense(xmlReader));
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

        return entry;
    }

    private static KanjiElement ReadKanjiElement(XmlReader xmlReader)
    {
        KanjiElement kanjiElement = new();

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
                        kanjiElement.Keb = xmlReader.ReadElementContentAsString();
                        break;

                    case "ke_inf":
                        kanjiElement.KeInfList.Add(ReadEntity(xmlReader)!);
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

        return kanjiElement;
    }

    private static ReadingElement ReadReadingElement(XmlReader xmlReader)
    {
        ReadingElement readingElement = new();

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
                        readingElement.Reb = xmlReader.ReadElementContentAsString();
                        break;

                    case "re_restr":
                        readingElement.ReRestrList.Add(xmlReader.ReadElementContentAsString());
                        break;

                    case "re_inf":
                        readingElement.ReInfList.Add(ReadEntity(xmlReader)!);
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

        return readingElement;
    }

    private static Sense ReadSense(XmlReader xmlReader)
    {
        Sense sense = new();

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
                        sense.StagKList.Add(xmlReader.ReadElementContentAsString());
                        break;

                    case "stagr":
                        sense.StagRList.Add(xmlReader.ReadElementContentAsString());
                        break;

                    case "pos":
                        sense.PosList.Add(ReadEntity(xmlReader)!);
                        break;

                    case "field":
                        sense.FieldList.Add(ReadEntity(xmlReader)!);
                        break;

                    case "misc":
                        sense.MiscList.Add(ReadEntity(xmlReader)!);
                        break;

                    case "s_inf":
                        sense.SInf = xmlReader.ReadElementContentAsString();
                        break;

                    case "dial":
                        sense.DialList.Add(ReadEntity(xmlReader)!);
                        break;

                    case "gloss":
                        string gloss = "";

                        if (xmlReader.HasAttributes)
                        {
                            string? glossType = xmlReader.GetAttribute("g_type");

                            if (glossType is not null)
                            {
                                gloss = "(" + glossType + ".) ";
                            }
                        }

                        gloss += xmlReader.ReadElementContentAsString();

                        sense.GlossList.Add(gloss);
                        break;

                    case "xref":
                        sense.XRefList.Add(xmlReader.ReadElementContentAsString());
                        break;

                    case "ant":
                        sense.AntList.Add(xmlReader.ReadElementContentAsString());
                        break;

                    case "lsource":
                        string? lang = xmlReader.GetAttribute("xml:lang");

                        if (lang is not null)
                        {
                            try
                            {
                                if (s_canHandleCulture)
                                {
                                    if (Utils.Iso6392BTo2T.TryGetValue(lang, out string? langCode))
                                    {
                                        lang = langCode;
                                    }

                                    lang = new CultureInfo(lang).EnglishName;
                                }
                            }

                            catch (Exception ex)
                            {
                                Utils.Logger.Error(ex, "Underlying OS cannot process the culture info");
                                s_canHandleCulture = false;
                            }
                        }

                        else
                        {
                            lang = "English";
                        }

                        bool isPart = xmlReader.GetAttribute("ls_type") is "part";
                        bool isWasei = xmlReader.GetAttribute("ls_wasei") is not null;

                        string? originalWord = xmlReader.ReadElementContentAsString();
                        originalWord = originalWord is not "" ? originalWord : null;

                        sense.LSourceList.Add(new LoanwordSource(lang, isPart, isWasei, originalWord));
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

        return sense;
    }

    private static string? ReadEntity(XmlReader xmlReader)
    {
        string? entityName = null;

        _ = xmlReader.Read();

        if (xmlReader.NodeType is XmlNodeType.EntityReference)
        {
            entityName = xmlReader.Name;

            xmlReader.ResolveEntity();
            _ = xmlReader.Read();

            _ = Storage.JmdictEntities.TryAdd(entityName, xmlReader.Value);

            _ = xmlReader.Read();
        }

        return entityName;
    }
}
