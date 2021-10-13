using System.Collections.Generic;

namespace JapaneseLookup.EDICT.JMdict
{
    public class KEle
    {
        public string Keb { get; set; } //e.g. 娘 
        public List<string> KeInfList { get; set; } //e.g. Ateji. Can a keb have more than one keInf?
        // public List<string> KePriList { get; set; } // e.g. gai1

        public KEle()
        {
            Keb = null;
            KeInfList = new List<string>();
            // KePriList = new List<string>();
        }
    }
}
