using System.Collections.Generic;

namespace JL.Dicts.EDICT.JMdict
{
    public class REle
    {
        public string Reb { get; set; } // Reading in kana. e.g. むすめ

        public List<string>
            ReRestrList { get; set; } // ReRestrList = Keb. The reading is only valid for this specific keb.

        public List<string> ReInfList { get; set; } // e.g. gikun

        // public bool ReNokanji { get; set; } // Is kana insufficiant to notate the right spelling?
        // public List<string> RePriList { get; set; } // e.g. ichi1
        public REle()
        {
            Reb = null;
            ReRestrList = new List<string>();
            ReInfList = new List<string>();
            // ReNokanji = false;
            // RePriList = new List<string>();
        }
    }
}
