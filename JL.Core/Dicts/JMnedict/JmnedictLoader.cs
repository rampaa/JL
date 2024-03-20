using System.Collections.Frozen;
using System.Xml;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (File.Exists(fullPath))
        {
            // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
            // And we do need EntityHandling property because we want to get unexpanded entity names
            // The downside of using XmlTextReader is that it does not support async methods
            // And we cannot set some settings (e.g. MaxCharactersFromEntities)

            using (XmlTextReader xmlTextReader = new(fullPath))
            {
                xmlTextReader.DtdProcessing = DtdProcessing.Parse;
                xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
                xmlTextReader.EntityHandling = EntityHandling.ExpandCharEntities;

                while (xmlTextReader.ReadToFollowing("entry"))
                {
                    JmnedictRecordBuilder.AddToDictionary(ReadEntry(xmlTextReader), dict.Contents);
                }
            }

            foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
            {
                dict.Contents[key] = recordList.ToArray();
            }

            dict.Contents = dict.Contents.ToFrozenDictionary();
        }

        else if (Utils.Frontend.ShowYesNoDialog("Couldn't find JMnedict.xml. Would you like to download it now?",
                     "Download JMnedict?"))
        {
            bool downloaded = await DictUpdater.UpdateDict(fullPath,
                DictUtils.s_jmnedictUrl,
                DictType.JMnedict.ToString(), false, false).ConfigureAwait(false);

            if (downloaded)
            {
                await Load(dict).ConfigureAwait(false);
            }
        }

        else
        {
            dict.Active = false;
        }
    }

    private static JmnedictEntry ReadEntry(XmlTextReader xmlReader)
    {
        JmnedictEntry entry = new();
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
                        entry.KebList.Add(ReadKEle(xmlReader).GetPooledString());
                        break;

                    case "r_ele":
                        entry.RebList.Add(ReadREle(xmlReader).GetPooledString());
                        break;

                    case "trans":
                        entry.TranslationList.Add(ReadTrans(xmlReader));
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

    private static string ReadKEle(XmlTextReader xmlReader)
    {
        _ = xmlReader.ReadToFollowing("keb");
        return xmlReader.ReadElementContentAsString();
    }

    private static string ReadREle(XmlTextReader xmlReader)
    {
        _ = xmlReader.ReadToFollowing("reb");
        return xmlReader.ReadElementContentAsString();
    }

    private static Translation ReadTrans(XmlTextReader xmlReader)
    {
        Translation translation = new();
        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "trans", NodeType: XmlNodeType.EndElement })
            {
                break;
            }

            if (xmlReader.NodeType is XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "name_type":
                        translation.NameTypeList.Add(ReadEntity(xmlReader));
                        break;

                    case "trans_det":
                        translation.TransDetList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    //case "xref":
                    //    translation.XRefList.Add(xmlReader.ReadElementContentAsString());
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

        return translation;
    }

    private static string ReadEntity(XmlTextReader xmlReader)
    {
        _ = xmlReader.Read();
        string entityName = xmlReader.Name.GetPooledString();

        if (!DictUtils.JmnedictEntities.ContainsKey(entityName))
        {
            xmlReader.ResolveEntity();
            _ = xmlReader.Read();

            DictUtils.JmnedictEntities.Add(entityName, xmlReader.Value.GetPooledString());
        }

        _ = xmlReader.Read();

        return entityName;
    }
}
