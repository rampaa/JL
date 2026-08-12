using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Frontend;
using JL.Core.Japanese;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictLoader
{
    // 2022/05/11: 608833, 2022/08/15: 609117, 2023/04/22: 609055, 2023/12/16: 609238, 2024/02/22: 609265
    public const int Size = 620000;

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

                Debug.Assert(dict.Contents is Dictionary<string, IList<IDictRecord>>);
                Dictionary<string, IList<IDictRecord>> contents = (Dictionary<string, IList<IDictRecord>>)dict.Contents;
                while (xmlTextReader.ReadToFollowing("entry"))
                {
                    Dictionary<string, JmnedictRecord> recordDictionary = GetRecordsFromEntry(ReadEntry(xmlTextReader));
                    foreach ((string key, JmnedictRecord jmnedictRecord) in recordDictionary)
                    {
                        ref IList<IDictRecord>? tempRecordList = ref CollectionsMarshal.GetValueRefOrAddDefault(contents, key, out bool exists);
                        if (exists)
                        {
                            Debug.Assert(tempRecordList is not null);
                            tempRecordList.Add(jmnedictRecord);
                        }
                        else
                        {
                            tempRecordList = [jmnedictRecord];
                        }

                        if (key.Length > dict.MaxSearchKeyLength)
                        {
                            dict.MaxSearchKeyLength = key.Length;
                        }
                    }
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
                else
                {
                    dict.Updating = false;
                }
            }
            else
            {
                dict.Active = false;
                dict.Updating = false;
            }
        }
    }

    public static JmnedictEntry ReadEntry(XmlTextReader xmlReader)
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

        return new JmnedictEntry(id, kebList, rebList.ToArray(), translationList);
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

        return new Translation(transDetList.ToArray(), nameTypeList.TrimToArray());
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

    public static Dictionary<string, JmnedictRecord> GetRecordsFromEntry(in JmnedictEntry entry)
    {
        ReadOnlySpan<string> kebListSpan = entry.KebList.AsReadOnlySpan();
        ReadOnlySpan<Translation> translationListSpan = entry.TranslationList.AsReadOnlySpan();

        int kebListSpanLength = kebListSpan.Length;
        int translationListSpanLength = translationListSpan.Length;

        Debug.Assert(translationListSpanLength > 0);

        string[][] definitionsArray = new string[translationListSpanLength][];
        string[]?[] nameTypesArray = new string[translationListSpanLength][];
        // string[]?[] relatedTermsArray = new string[translationListCount][];

        for (int j = 0; j < translationListSpanLength; j++)
        {
            ref readonly Translation translation = ref translationListSpan[j];

            definitionsArray[j] = translation.TransDetArray;
            nameTypesArray[j] = translation.NameTypeArray;
            // relatedTermsArray[j] = translation.XRefList.TrimListToArray();
        }

        Dictionary<string, JmnedictRecord> recordDictionary;
        if (kebListSpanLength > 0)
        {
            recordDictionary = new Dictionary<string, JmnedictRecord>(kebListSpanLength, StringComparer.Ordinal);
            for (int i = 0; i < kebListSpan.Length; i++)
            {
                string keb = kebListSpan[i];
                string key = JapaneseUtils.NormalizeText(keb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                JmnedictRecord record = new(entry.Id, keb, entry.KebList.RemoveAtToArray(i), entry.RebArray, definitionsArray, nameTypesArray.TrimNullableArray());
                // record.RelatedTerms = relatedTermsArray;

                recordDictionary.Add(key, record);
            }
        }
        else
        {
            recordDictionary = new Dictionary<string, JmnedictRecord>(entry.RebArray.Length, StringComparer.Ordinal);
            for (int i = 0; i < entry.RebArray.Length; i++)
            {
                string reb = entry.RebArray[i];
                string key = JapaneseUtils.NormalizeText(reb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                JmnedictRecord record = new(entry.Id, reb, entry.RebArray.RemoveAt(i), null, definitionsArray, nameTypesArray.TrimNullableArray());
                // record.RelatedTerms = relatedTermsArray;

                recordDictionary.Add(key, record);
            }
        }

        return recordDictionary;
    }
}
