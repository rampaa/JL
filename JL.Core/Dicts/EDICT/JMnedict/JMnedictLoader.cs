using System.Xml;

namespace JL.Core.Dicts.EDICT.JMnedict;

public static class JMnedictLoader
{
    public static async Task Load(Dict dict)
    {
        if (File.Exists(dict.Path))
        {
            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)

            using XmlTextReader xmlTextReader = new(dict.Path)
            {
                DtdProcessing = DtdProcessing.Parse,
                WhitespaceHandling = WhitespaceHandling.None,
                EntityHandling = EntityHandling.ExpandCharEntities
            };

            while (xmlTextReader.ReadToFollowing("entry"))
            {
                ReadEntry(xmlTextReader, dict);
            }

            dict.Contents.TrimExcess();
        }

        else if (Storage.Frontend.ShowYesNoDialog("Couldn't find JMnedict.xml. Would you like to download it now?",
                     "Download JMnedict?"))
        {
            await ResourceUpdater.UpdateResource(dict.Path,
                Storage.JmnedictUrl,
                DictType.JMnedict.ToString(), false, false).ConfigureAwait(false);
            await Load(dict).ConfigureAwait(false);
        }

        else
        {
            dict.Active = false;
        }
    }

    private static void ReadEntry(XmlTextReader xmlReader, Dict dict)
    {
        JMnedictEntry entry = new();
        while (!xmlReader.EOF)
        {
            if (xmlReader.Name == "entry" && xmlReader.NodeType == XmlNodeType.EndElement)
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

                    case "trans":
                        ReadTrans(xmlReader, entry);
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

        JMnedictBuilder.BuildDictionary(entry, dict.Contents);
    }

    private static void ReadKEle(XmlTextReader xmlReader, JMnedictEntry entry)
    {
        xmlReader.ReadToFollowing("keb");
        entry.KebList.Add(xmlReader.ReadElementContentAsString());
        //xmlReader.ReadToFollowing("k_ele");
    }

    private static void ReadREle(XmlTextReader xmlReader, JMnedictEntry entry)
    {
        xmlReader.ReadToFollowing("reb");
        entry.RebList.Add(xmlReader.ReadElementContentAsString());
        //xmlReader.ReadToFollowing("r_ele");
    }

    private static void ReadTrans(XmlTextReader xmlReader, JMnedictEntry entry)
    {
        Trans trans = new();
        while (!xmlReader.EOF)
        {
            if (xmlReader.Name == "trans" && xmlReader.NodeType == XmlNodeType.EndElement)
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "name_type":
                        trans.NameTypeList.Add(ReadEntity(xmlReader)!);
                        break;

                    case "trans_det":
                        trans.TransDetList.Add(xmlReader.ReadElementContentAsString());
                        break;

                    //case "xref":
                    //    trans.XRefList.Add(xmlReader.ReadElementContentAsString());
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

        entry.TransList.Add(trans);
    }

    private static string? ReadEntity(XmlTextReader xmlReader)
    {
        string? entityName = null;

        xmlReader.Read();
        if (xmlReader.NodeType == XmlNodeType.EntityReference)
        {
            //xmlReader.ResolveEntity();
            entityName = xmlReader.Name;
            xmlReader.Read();
        }

        return entityName;
    }
}
