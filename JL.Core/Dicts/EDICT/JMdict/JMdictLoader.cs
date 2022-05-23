using System.Xml;

namespace JL.Core.Dicts.EDICT.JMdict;

public static class JMdictLoader
{
    public static async Task Load(string dictPath)
    {
        if (File.Exists(dictPath))
        {
            using XmlTextReader edictXml = new(dictPath)
            {
                DtdProcessing = DtdProcessing.Parse,
                WhitespaceHandling = WhitespaceHandling.None,
                EntityHandling = EntityHandling.ExpandCharEntities
            };

            while (edictXml.ReadToFollowing("entry"))
            {
                ReadEntry(edictXml);
            }

            Storage.Dicts[DictType.JMdict].Contents.TrimExcess();
        }

        else if (Storage.Frontend.ShowYesNoDialog(
                     "Couldn't find JMdict.xml. Would you like to download it now?",
                     ""))
        {
            await ResourceUpdater.UpdateResource(Storage.Dicts[DictType.JMdict].Path,
                Storage.JmdictUrl,
                DictType.JMdict.ToString(), false, false).ConfigureAwait(false);
            await Load(Storage.Dicts[DictType.JMdict].Path).ConfigureAwait(false);
        }

        else
        {
            Storage.Dicts[DictType.JMdict].Active = false;
        }
    }

    private static void ReadEntry(XmlTextReader edictXml)
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

        JMdictBuilder.BuildDictionary(entry, Storage.Dicts[DictType.JMdict].Contents);
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
