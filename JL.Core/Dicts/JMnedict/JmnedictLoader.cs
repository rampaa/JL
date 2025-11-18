using System.Collections.Frozen;
using System.Diagnostics;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Frontend;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (File.Exists(fullPath))
        {
            DictUtils.JmnedictEntities.Clear();

            // ReSharper disable once UseAwaitUsing
            using (FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_syncRead64KBufferFso))
            {
                // XmlTextReader is preferred over XmlReader here because XmlReader does not have the EntityHandling property
                // And we do need EntityHandling property because we want to get unexpanded entity names
                // The downside of using XmlTextReader is that it does not support async methods
                // And we cannot set some settings (e.g. MaxCharactersFromEntities)
                using XmlTextReader xmlTextReader = new(fileStream);
                xmlTextReader.DtdProcessing = DtdProcessing.Parse;
                xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
                xmlTextReader.EntityHandling = EntityHandling.ExpandCharEntities;

                while (xmlTextReader.ReadToFollowing("entry"))
                {
                    JmnedictRecordBuilder.AddToDictionary(ReadEntry(xmlTextReader), dict.Contents);
                }
            }

            dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
        }
        else
        {
            if (dict.Updating)
            {
                return;
            }

            dict.Updating = true;
            if (await FrontendManager.Frontend.ShowYesNoDialogAsync("Couldn't find JMnedict.xml. Would you like to download it now?",
                "Download JMnedict?").ConfigureAwait(false))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.JMnedict), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    try
                    {
                        await Load(dict).ConfigureAwait(false);
                    }
                    finally
                    {
                        dict.Updating = false;
                    }
                }
            }
            else
            {
                dict.Active = false;
                dict.Updating = false;
            }
        }
    }

    private static JmnedictEntry ReadEntry(XmlTextReader xmlReader)
    {
        int id = 0;
        List<string> kebList = [];
        List<string> rebList = [];
        List<Translation> translationList = [];

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
                        kebList.Add(ReadKEle(xmlReader).GetPooledString());
                        break;

                    case "r_ele":
                        rebList.Add(ReadREle(xmlReader).GetPooledString());
                        break;

                    case "trans":
                        translationList.Add(ReadTrans(xmlReader));
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

        return new JmnedictEntry(id, kebList, rebList, translationList);
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
        List<string> nameTypeList = [];
        List<string> transDetList = [];

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
                        nameTypeList.Add(ReadEntity(xmlReader));
                        break;

                    case "trans_det":
                        transDetList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
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

        return new Translation(nameTypeList, transDetList);
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
