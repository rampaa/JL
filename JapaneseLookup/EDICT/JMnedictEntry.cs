using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    class JMnedictEntry : Entry
    {
        public List<Trans> Trans { get; set; }
        public JMnedictEntry() :base()
        {
            Trans = new List<Trans>();
        }
    }
}