using System.Xml;

namespace JL.Core.Dicts.EDICT.JMnedict;

public static class JmnedictLoader
{
    public static async Task Load(Dict dict)
    {
        if (File.Exists(dict.Path))
        {
            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)

            using XmlReader xmlTextReader = new XmlTextReader(dict.Path)
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

    private static void ReadEntry(XmlReader xmlReader, Dict dict)
    {
        JmnedictEntry entry = new();
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

        JmnedictBuilder.BuildDictionary(entry, dict.Contents);
    }

    private static void ReadKEle(XmlReader xmlReader, JmnedictEntry entry)
    {
        xmlReader.ReadToFollowing("keb");
        entry.KebList.Add(xmlReader.ReadElementContentAsString());
        //xmlReader.ReadToFollowing("k_ele");
    }

    private static void ReadREle(XmlReader xmlReader, JmnedictEntry entry)
    {
        xmlReader.ReadToFollowing("reb");
        entry.RebList.Add(xmlReader.ReadElementContentAsString());
        //xmlReader.ReadToFollowing("r_ele");
    }

    private static void ReadTrans(XmlReader xmlReader, JmnedictEntry entry)
    {
        Translation translation = new();
        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "trans", NodeType: XmlNodeType.EndElement })
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "name_type":
                        translation.NameTypeList.Add(ReadEntity(xmlReader)!);
                        break;

                    case "trans_det":
                        translation.TransDetList.Add(xmlReader.ReadElementContentAsString());
                        break;

                    //case "xref":
                    //    translation.XRefList.Add(xmlReader.ReadElementContentAsString());
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

        entry.TransList.Add(translation);
    }

    private static string? ReadEntity(XmlReader xmlReader)
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
