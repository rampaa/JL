using System.Globalization;
using System.Xml;

namespace JL.Core.Dicts.EDICT.JMdict;

public static class JMdictLoader
{
    public static async Task Load(Dict dict)
    {
        if (File.Exists(dict.Path))
        {
            using XmlTextReader edictXml = new(dict.Path)
            {
                DtdProcessing = DtdProcessing.Parse,
                WhitespaceHandling = WhitespaceHandling.None,
                EntityHandling = EntityHandling.ExpandCharEntities
            };

            while (edictXml.ReadToFollowing("entry"))
            {
                ReadEntry(edictXml, dict);
            }

            dict.Contents.TrimExcess();
        }

        else if (Storage.Frontend.ShowYesNoDialog(
                     "Couldn't find JMdict.xml. Would you like to download it now?",
                     ""))
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

    private static void ReadEntry(XmlTextReader edictXml, Dict dict)
    {
        JMdictEntry entry = new();
        while (edictXml.Read())
        {
            if (edictXml.Name == "entry" && edictXml.NodeType == XmlNodeType.EndElement)
                break;

            if (edictXml.NodeType == XmlNodeType.Element)
            {
                switch (edictXml.Name)
                {
                    case "ent_seq":
                        entry.Id = edictXml.ReadString();
                        break;

                    case "k_ele":
                        ReadKEle(edictXml, entry);
                        break;

                    case "r_ele":
                        ReadREle(edictXml, entry);
                        break;

                    case "sense":
                        ReadSense(edictXml, entry);
                        break;
                }
            }
        }

        JMdictBuilder.BuildDictionary(entry, dict.Contents);
    }

    private static void ReadKEle(XmlTextReader edictXml, JMdictEntry entry)
    {
        KanjiElement kanjiElement = new();
        while (edictXml.Read())
        {
            if (edictXml.Name == "k_ele" && edictXml.NodeType == XmlNodeType.EndElement)
                break;

            if (edictXml.NodeType == XmlNodeType.Element)
            {
                switch (edictXml.Name)
                {
                    case "keb":
                        kanjiElement.Keb = edictXml.ReadString();
                        break;

                    case "ke_inf":
                        kanjiElement.KeInfList.Add(ReadEntity(edictXml)!);
                        break;

                        //case "ke_pri":
                        //    kanjiElement.KePriList.Add(edictXml.ReadString());
                        //    break;
                }
            }
        }

        entry.KanjiElements.Add(kanjiElement);
    }

    private static void ReadREle(XmlTextReader jmdictXml, JMdictEntry entry)
    {
        ReadingElement readingElement = new();
        while (jmdictXml.Read())
        {
            if (jmdictXml.Name == "r_ele" && jmdictXml.NodeType == XmlNodeType.EndElement)
                break;

            if (jmdictXml.NodeType == XmlNodeType.Element)
            {
                switch (jmdictXml.Name)
                {
                    case "reb":
                        readingElement.Reb = jmdictXml.ReadString();
                        break;

                    case "re_restr":
                        readingElement.ReRestrList.Add(jmdictXml.ReadString());
                        break;

                    case "re_inf":
                        readingElement.ReInfList.Add(ReadEntity(jmdictXml)!);
                        break;

                        //case "re_pri":
                        //    readingElement.RePriList.Add(jMDictXML.ReadString());
                        //    break;
                }
            }
        }

        entry.ReadingElements.Add(readingElement);
    }

    private static void ReadSense(XmlTextReader jmdictXml, JMdictEntry entry)
    {
        Sense sense = new();
        while (jmdictXml.Read())
        {
            if (jmdictXml.Name == "sense" && jmdictXml.NodeType == XmlNodeType.EndElement)
                break;

            if (jmdictXml.NodeType == XmlNodeType.Element)
            {
                switch (jmdictXml.Name)
                {
                    case "stagk":
                        sense.StagKList.Add(jmdictXml.ReadString());
                        break;

                    case "stagr":
                        sense.StagRList.Add(jmdictXml.ReadString());
                        break;

                    case "pos":
                        sense.PosList.Add(ReadEntity(jmdictXml)!);
                        break;

                    case "field":
                        sense.FieldList.Add(ReadEntity(jmdictXml)!);
                        break;

                    case "misc":
                        sense.MiscList.Add(ReadEntity(jmdictXml)!);
                        break;

                    case "s_inf":
                        sense.SInf = jmdictXml.ReadString();
                        break;

                    case "dial":
                        sense.DialList.Add(ReadEntity(jmdictXml)!);
                        break;

                    case "gloss":
                        string gloss = "";

                        if (jmdictXml.HasAttributes)
                        {
                            string? glossType = jmdictXml.GetAttribute("g_type");

                            if (glossType != null)
                                gloss = "(" + glossType + ".) ";
                        }

                        gloss += jmdictXml.ReadString();

                        sense.GlossList.Add(gloss);
                        break;

                    case "xref":
                        sense.XRefList.Add(jmdictXml.ReadString());
                        break;

                    case "ant":
                        sense.AntList.Add(jmdictXml.ReadString());
                        break;

                    case "lsource":
                        string? lang = jmdictXml.GetAttribute("xml:lang");

                        if (lang != null)
                        {
                            if (Utilities.Utils.Iso6392BTo2T.TryGetValue(lang, out string? langCode))
                            {
                                lang = langCode;
                            }

                            if (Utilities.Utils.Iso6392ToLanguageNameForWindows7.TryGetValue(lang, out string? langName))
                            {
                                lang = langName;
                            }

                            else
                            {
                                lang = new CultureInfo(lang).EnglishName;
                            }
                        }

                        else
                        {
                            lang = "English";
                        }

                        bool isPart = jmdictXml.GetAttribute("ls_type") == "part";
                        bool isWasei = jmdictXml.GetAttribute("ls_wasei") != null;

                        string? originalWord = jmdictXml.ReadString();
                        originalWord = originalWord != "" ? originalWord : null;

                        sense.LSourceList.Add(new LSource(lang, isPart, isWasei, originalWord));
                        break;
                }
            }
        }

        entry.SenseList.Add(sense);
    }

    private static string? ReadEntity(XmlTextReader jmdictXml)
    {
        jmdictXml.Read();
        if (jmdictXml.NodeType == XmlNodeType.EntityReference)
        {
            //jMDictXML.ResolveEntity();
            return jmdictXml.Name;
        }
        else return null;
    }
}
