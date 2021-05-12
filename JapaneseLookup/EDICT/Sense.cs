using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    class Sense
    {
        public List<string> StagKList { get; set; } // Meaning only valid for these kebs.
        public List<string> StagRList { get; set; } // Meaning only valid for these rebs.
        public List<string> PosList { get; set; } // e.g. "noun"
        public List<string> FieldList { get; set; } // e.g. "martial arts"
        public List<string> MiscList { get; set; } // e.g. "abbr"
        public string SInf { get; set; } // e.g. "often derog"
        public List<string> DialList { get; set; } // e.g. ksb
        public List<string> GlossList { get; set; } // English meaning
        // public List<string> XRefList { get; set; } // Related terms
        // public List<string> AntList { get; set; } // Antonyms

        public Sense()
        {
            StagKList = new List<string>();
            StagRList = new List<string>();
            PosList = new List<string>();
            FieldList = new List<string>();
            MiscList = new List<string>();
            SInf = null;
            DialList = new List<string>();
            GlossList = new List<string>();
            // XRefList = new List<string>();
            // AntList = new List<string>();
        }
    }
}
