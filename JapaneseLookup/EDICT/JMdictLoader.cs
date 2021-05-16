using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using static JapaneseLookup.MainWindowUtilities;

namespace JapaneseLookup.EDICT
{
    class JMdictLoader
    {
        public static Dictionary<string, List<JMdictResult>> jMdictDictionary = new();
        public static void Load()
        {
            if(File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/JMdict.xml")))
            {
                using XmlTextReader edictXml = new(Path.Join(ConfigManager.ApplicationPath, "Resources/JMdict.xml"));

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
                MessageBox.Show("Couldn't find JMdict.xml. Please download it by clicking the \"Update JMdict\" button.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
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
            JMDictBuilder.BuildDictionary(entry, jMdictDictionary);
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
                            kEle.KeInfList.Add(ReadEntity(edictXml));
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
                            rEle.ReInfList.Add(ReadEntity(jMDictXML));
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
                            sense.PosList.Add(ReadEntity(jMDictXML));
                            break;

                        case "field":
                            sense.FieldList.Add(ReadEntity(jMDictXML));
                            break;

                        case "misc":
                            sense.MiscList.Add(ReadEntity(jMDictXML));
                            break;

                        case "s_inf":
                            sense.SInf = jMDictXML.ReadString();
                            break;

                        case "dial":
                            sense.DialList.Add(ReadEntity(jMDictXML));
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
