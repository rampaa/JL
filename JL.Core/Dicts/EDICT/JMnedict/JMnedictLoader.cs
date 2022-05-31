using System.Xml;

namespace JL.Core.Dicts.EDICT.JMnedict;

public static class JMnedictLoader
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

        else if (Storage.Frontend.ShowYesNoDialog("Couldn't find JMnedict.xml. Would you like to download it now?",
                     ""))
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

    private static void ReadEntry(XmlTextReader edictXml, Dict dict)
    {
        JMnedictEntry entry = new();
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

                    case "trans":
                        ReadTrans(edictXml, entry);
                        break;
                }
            }
        }

        JMnedictBuilder.BuildDictionary(entry, dict.Contents);
    }

    private static void ReadKEle(XmlTextReader jmnedictXml, JMnedictEntry entry)
    {
        jmnedictXml.ReadToFollowing("keb");
        entry.KebList.Add(jmnedictXml.ReadString());
        //jmnedictXml.ReadToFollowing("k_ele");
    }

    private static void ReadREle(XmlTextReader jmnedictXml, JMnedictEntry entry)
    {
        jmnedictXml.ReadToFollowing("reb");
        entry.RebList.Add(jmnedictXml.ReadString());
        //jmnedictXml.ReadToFollowing("r_ele");
    }

    private static void ReadTrans(XmlTextReader jmnedictXml, JMnedictEntry entry)
    {
        Trans trans = new();
        while (jmnedictXml.Read())
        {
            if (jmnedictXml.Name == "trans" && jmnedictXml.NodeType == XmlNodeType.EndElement)
                break;

            if (jmnedictXml.NodeType == XmlNodeType.Element)
            {
                switch (jmnedictXml.Name)
                {
                    case "name_type":
                        trans.NameTypeList.Add(ReadEntity(jmnedictXml)!);
                        break;

                    case "trans_det":
                        trans.TransDetList.Add(jmnedictXml.ReadString());
                        break;

                        //case "xref":
                        //    trans.XRefList.Add(jmnedictXml.ReadString());
                        //    break;
                }
            }
        }

        entry.TransList.Add(trans);
    }

    private static string? ReadEntity(XmlTextReader jmnedictXml)
    {
        jmnedictXml.Read();
        if (jmnedictXml.NodeType == XmlNodeType.EntityReference)
        {
            //jmnedictXml.ResolveEntity();
            return jmnedictXml.Name;
        }

        return null;
    }
}
