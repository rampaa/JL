using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    class REle
    {
        public string Reb { get; set; } // Reading in kana. e.g. むすめ
        // public bool ReNokanji { get; set; } // Is kana insufficiant to notate the right spelling?
        public List<string> ReRestrList { get; set; } // ReRestrList = Keb. The reading is only valid for this specific keb.
        public List<string> ReInfList { get; set; } // e.g. gikun
        public List<string> RePriList { get; set; } // e.g. ichi1

        public REle() : this(null) { }
        public REle(string reb)
        {
            Reb = reb;
            // ReNokanji = false;
            ReRestrList = new List<string>();
            ReInfList = new List<string>();
            RePriList = new List<string>();
        }
    }
}
