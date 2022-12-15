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
                JmnedictRecordBuilder.AddToDictionary(ReadEntry(xmlTextReader), dict.Contents);
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

    private static JmnedictEntry ReadEntry(XmlReader xmlReader)
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
                        entry.KebList.Add(ReadKEle(xmlReader));
                        break;

                    case "r_ele":
                        entry.RebList.Add(ReadREle(xmlReader));
                        break;

                    case "trans":
                        entry.TranslationList.Add(ReadTrans(xmlReader));
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

        return entry;
    }

    private static string ReadKEle(XmlReader xmlReader)
    {
        xmlReader.ReadToFollowing("keb");
        return xmlReader.ReadElementContentAsString();
        //xmlReader.ReadToFollowing("k_ele");
    }

    private static string ReadREle(XmlReader xmlReader)
    {
        xmlReader.ReadToFollowing("reb");
        return xmlReader.ReadElementContentAsString();
        //xmlReader.ReadToFollowing("r_ele");
    }

    private static Translation ReadTrans(XmlReader xmlReader)
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

        return translation;
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
