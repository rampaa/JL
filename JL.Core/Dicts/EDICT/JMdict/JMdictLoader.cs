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
        KEle kEle = new();
        while (edictXml.Read())
        {
            if (edictXml.Name == "k_ele" && edictXml.NodeType == XmlNodeType.EndElement)
                break;

            if (edictXml.NodeType == XmlNodeType.Element)
            {
                switch (edictXml.Name)
                {
                    case "keb":
                        kEle.Keb = edictXml.ReadString();
                        break;

                    case "ke_inf":
                        kEle.KeInfList.Add(ReadEntity(edictXml)!);
                        break;

                        //case "ke_pri":
                        //    kEle.KePriList.Add(edictXml.ReadString());
                        //    break;
                }
            }
        }

        entry.KEleList.Add(kEle);
    }

    private static void ReadREle(XmlTextReader jMDictXML, JMdictEntry entry)
    {
        REle rEle = new();
        while (jMDictXML.Read())
        {
            if (jMDictXML.Name == "r_ele" && jMDictXML.NodeType == XmlNodeType.EndElement)
                break;

            if (jMDictXML.NodeType == XmlNodeType.Element)
            {
                switch (jMDictXML.Name)
                {
                    case "reb":
                        rEle.Reb = jMDictXML.ReadString();
                        break;

                    case "re_restr":
                        rEle.ReRestrList.Add(jMDictXML.ReadString());
                        break;

                    case "re_inf":
                        rEle.ReInfList.Add(ReadEntity(jMDictXML)!);
                        break;

                        //case "re_pri":
                        //    rEle.RePriList.Add(jMDictXML.ReadString());
                        //    break;
                }
            }
        }

        entry.REleList.Add(rEle);
    }

    private static void ReadSense(XmlTextReader jMDictXML, JMdictEntry entry)
    {
        Sense sense = new();
        while (jMDictXML.Read())
        {
            if (jMDictXML.Name == "sense" && jMDictXML.NodeType == XmlNodeType.EndElement)
                break;

            if (jMDictXML.NodeType == XmlNodeType.Element)
            {
                switch (jMDictXML.Name)
                {
                    case "stagk":
                        sense.StagKList.Add(jMDictXML.ReadString());
                        break;

                    case "stagr":
                        sense.StagRList.Add(jMDictXML.ReadString());
                        break;

                    case "pos":
                        sense.PosList.Add(ReadEntity(jMDictXML)!);
                        break;

                    case "field":
                        sense.FieldList.Add(ReadEntity(jMDictXML)!);
                        break;

                    case "misc":
                        sense.MiscList.Add(ReadEntity(jMDictXML)!);
                        break;

                    case "s_inf":
                        sense.SInf = jMDictXML.ReadString();
                        break;

                    case "dial":
                        sense.DialList.Add(ReadEntity(jMDictXML)!);
                        break;

                    case "gloss":
                        sense.GlossList.Add(jMDictXML.ReadString());
                        break;

                        //case "xref":
                        //    sense.XRefList.Add(jMDictXML.ReadString());
                        //    break;

                        //case "ant":
                        //    sense.AntList.Add(jMDictXML.ReadString());
                        //    break;
                }
            }
        }

        entry.SenseList.Add(sense);
    }

    private static string? ReadEntity(XmlTextReader jMDictXML)
    {
        jMDictXML.Read();
        if (jMDictXML.NodeType == XmlNodeType.EntityReference)
        {
            //jMDictXML.ResolveEntity();
            return jMDictXML.Name;
        }
        else return null;
    }
}
