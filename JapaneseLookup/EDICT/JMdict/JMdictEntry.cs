using System.Collections.Generic;

namespace JapaneseLookup.EDICT.JMdict
{
    public class JMdictEntry
    {
        public string Id { get; set; }
        public List<KEle> KEleList { get; set; }
        public List<REle> REleList { get; set; }
        public List<Sense> SenseList { get; set; }
        public List<Trans> TransList { get; set; }
        public JMdictEntry()
        {
            KEleList = new List<KEle>();
            REleList = new List<REle>();
            SenseList = new List<Sense>();
            TransList = new List<Trans>();
        }
    }
}
