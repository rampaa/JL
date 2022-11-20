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
                ReadEntry(xmlReader, dict);
            }

            dict.Contents.TrimExcess();
        }

        else if (Storage.Frontend.ShowYesNoDialog(
                     "Couldn't find JMdict.xml. Would you like to download it now?",
                     "Download JMdict?"))
        {
            await ResourceUpdater.UpdateResource(dict.Path,
                Storage.JmdictUrl,
                DictType.JMdict.ToString(), false, false).ConfigureAwait(false);
            await Load(dict).ConfigureAwait(false);
        }

        else
        {
            dict.Active = false;
        }
    }

    private static void ReadEntry(XmlReader xmlReader, Dict dict)
    {
        JmdictEntry entry = new();

        xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "entry", NodeType: XmlNodeType.EndElement })
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "ent_seq":
                        entry.Id = xmlReader.ReadElementContentAsInt();
                        break;

                    case "k_ele":
                        ReadKEle(xmlReader, entry);
                        break;

                    case "r_ele":
                        ReadREle(xmlReader, entry);
                        break;

                    case "sense":
                        ReadSense(xmlReader, entry);
                        break;

                    default:
                        xmlReader.Read();
                        break;
                }
            }
            else
            {
                xmlReader.Read();
            }
        }

        JmdictBuilder.BuildDictionary(entry, dict.Contents);
    }

    private static void ReadKEle(XmlReader xmlReader, JmdictEntry entry)
    {
        KanjiElement kanjiElement = new();

        xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "k_ele", NodeType: XmlNodeType.EndElement })
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
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
                        xmlReader.Read();
                        break;
                }
            }

            else
            {
                xmlReader.Read();
            }
        }

        entry.KanjiElements.Add(kanjiElement);
    }

    private static void ReadREle(XmlReader xmlReader, JmdictEntry entry)
    {
        ReadingElement readingElement = new();

        xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "r_ele", NodeType: XmlNodeType.EndElement })
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
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
                        xmlReader.Read();
                        break;
                }
            }

            else
            {
                xmlReader.Read();
            }
        }

        entry.ReadingElements.Add(readingElement);
    }

    private static void ReadSense(XmlReader xmlReader, JmdictEntry entry)
    {
        Sense sense = new();

        xmlReader.Read();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "sense", NodeType: XmlNodeType.EndElement })
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
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

                            if (glossType != null)
                                gloss = "(" + glossType + ".) ";
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

                        if (lang != null)
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
                                Utils.Logger.Error("{CultureInfoException}", ex.ToString());
                                s_canHandleCulture = false;
                            }
                        }

                        else
                        {
                            lang = "English";
                        }

                        bool isPart = xmlReader.GetAttribute("ls_type") == "part";
                        bool isWasei = xmlReader.GetAttribute("ls_wasei") != null;

                        string? originalWord = xmlReader.ReadElementContentAsString();
                        originalWord = originalWord != "" ? originalWord : null;

                        sense.LSourceList.Add(new LoanwordSource(lang, isPart, isWasei, originalWord));
                        break;

                    default:
                        xmlReader.Read();
                        break;
                }
            }

            else
            {
                xmlReader.Read();
            }
        }

        entry.SenseList.Add(sense);
    }

    private static string? ReadEntity(XmlReader xmlReader)
    {
        string? entityName = null;

        xmlReader.Read();

        if (xmlReader.NodeType == XmlNodeType.EntityReference)
        {
            entityName = xmlReader.Name;

            xmlReader.ResolveEntity();
            xmlReader.Read();

            Storage.JmdictEntities.TryAdd(entityName, xmlReader.Value);

            xmlReader.Read();
        }

        return entityName;
    }
}
