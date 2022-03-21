using System.Xml;

namespace JL.Core.Dicts.EDICT.JMnedict
{
    public static class JMnedictLoader
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

                Storage.Dicts[DictType.JMnedict].Contents.TrimExcess();
            }

            else if (Storage.Frontend.ShowYesNoDialog("Couldn't find JMnedict.xml. Would you like to download it now?",
                         ""))
            {
                await ResourceUpdater.UpdateResource(Storage.Dicts[DictType.JMnedict].Path,
                    new Uri("http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz"),
                    DictType.JMnedict.ToString(), false, false).ConfigureAwait(false);
                await Load(Storage.Dicts[DictType.JMnedict].Path).ConfigureAwait(false);
            }

            else
            {
                Storage.Dicts[DictType.JMnedict].Active = false;
            }
        }

        private static void ReadEntry(XmlTextReader edictXml)
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

            JMnedictBuilder.BuildDictionary(entry, Storage.Dicts[DictType.JMnedict].Contents);
        }

        private static void ReadKEle(XmlTextReader jMneDictXML, JMnedictEntry entry)
        {
            jMneDictXML.ReadToFollowing("keb");
            entry.KebList.Add(jMneDictXML.ReadString());
            //jMneDictXML.ReadToFollowing("k_ele");
        }

        private static void ReadREle(XmlTextReader jMneDictXML, JMnedictEntry entry)
        {
            jMneDictXML.ReadToFollowing("reb");
            entry.RebList.Add(jMneDictXML.ReadString());
            //jMneDictXML.ReadToFollowing("r_ele");
        }

        private static void ReadTrans(XmlTextReader jMneDictXML, JMnedictEntry entry)
        {
            Trans trans = new();
            while (jMneDictXML.Read())
            {
                if (jMneDictXML.Name == "trans" && jMneDictXML.NodeType == XmlNodeType.EndElement)
                    break;

                if (jMneDictXML.NodeType == XmlNodeType.Element)
                {
                    switch (jMneDictXML.Name)
                    {
                        case "name_type":
                            trans.NameTypeList.Add(ReadEntity(jMneDictXML));
                            break;

                        case "trans_det":
                            trans.TransDetList.Add(jMneDictXML.ReadString());
                            break;

                            //case "xref":
                            //    trans.XRefList.Add(jMneDictXML.ReadString());
                            //    break;
                    }
                }
            }

            entry.TransList.Add(trans);
        }

        private static string ReadEntity(XmlTextReader jMDictXML)
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
}
