using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace JapaneseLookup
{
    class JMdictLoader
    {
        private static List<JMdictEntry> jMdict;
        public static void Loader()
        {
            jMdict = new List<JMdictEntry>();
            using XmlTextReader jMDictXML = new("../net5.0-windows/Resources/JMdict.xml");
            jMDictXML.DtdProcessing = DtdProcessing.Parse;
            jMDictXML.WhitespaceHandling = WhitespaceHandling.None;
            jMDictXML.EntityHandling = EntityHandling.ExpandCharEntities;
            while (jMDictXML.ReadToFollowing("entry"))
            {
                EntryReader(jMDictXML);
            }
        }
        private static void EntryReader(XmlTextReader jMDictXML)
        {

        }

    }
}
