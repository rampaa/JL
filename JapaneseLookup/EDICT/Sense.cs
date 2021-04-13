using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class Sense
    {
        public List<string> StagKList { get; set; } // Meaning only valid for these kebs.
        public List<string> StagRList { get; set; } // Meaning only valid for these rebs.
        public List<string> XRefList { get; set; } // Related terms
        public List<string> AntList { get; set; } // Antonyms
        public List<string> Pos { get; set; } // e.g. "noun"
        public List<string> FieldList { get; set; } // e.g. "martial arts"
        public List<string> MiscList { get; set; } // e.g. "abbr"
        public List<string> Dial { get; set; } // e.g. ksb
        public List<string> Gloss { get; set; } // English meaning
        public List<string> SInfList { get; set; } // e.g. "often derog"

        public Sense()
        {
            StagKList = new List<string>();
            StagRList = new List<string>();
            XRefList = new List<string>();
            AntList = new List<string>();
            Pos = new List<string>();
            FieldList = new List<string>();
            MiscList = new List<string>();
            Dial = new List<string>();
            Gloss = new List<string>();
            SInfList = new List<string>();
        }
    }
}
