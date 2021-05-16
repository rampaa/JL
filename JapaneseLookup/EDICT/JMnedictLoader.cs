using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using static JapaneseLookup.MainWindowUtilities;

namespace JapaneseLookup.EDICT
{
    class JMnedictLoader
    {
        public static Dictionary<string, List<JMnedictResult>> jMnedictDictionary = new();
        public static void Load()
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/JMnedict.xml")))
            {
                using XmlTextReader edictXml = new(Path.Join(ConfigManager.ApplicationPath, "Resources/JMnedict.xml"));

                edictXml.DtdProcessing = DtdProcessing.Parse;
                edictXml.WhitespaceHandling = WhitespaceHandling.None;
                edictXml.EntityHandling = EntityHandling.ExpandCharEntities;
                while (edictXml.ReadToFollowing("entry"))
                {
                    ReadEntry(edictXml);
                }
            }
            else
            {
                MessageBox.Show("Couldn't find JMnedict.xml. Please download it by clicking the \"Update JMnedict\" button.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
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
            JMNeDictBuilder.BuildDictionary(entry, jMnedictDictionary);
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
