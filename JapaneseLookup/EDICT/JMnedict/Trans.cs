using System.Collections.Generic;

namespace JapaneseLookup.EDICT.JMnedict
{
    public class Trans
    {
        public List<string> NameTypeList { get; set; }

        public List<string> TransDetList { get; set; }
        // public List<string> XRefList { get; set; }

        public Trans()
        {
            NameTypeList = new List<string>();
            TransDetList = new List<string>();
            // XRefList = new List<string>();
        }
    }
}
