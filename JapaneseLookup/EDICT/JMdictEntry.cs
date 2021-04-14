using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    class JMdictEntry : Entry
    {
        public List<Sense> SenseList { get; set; }
        public JMdictEntry() : base()
        {
            SenseList = new List<Sense>();
        }
    }
}
