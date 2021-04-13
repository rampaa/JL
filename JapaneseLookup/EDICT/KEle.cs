using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class KEle
    {
        public string Keb { get; set; } //e.g. 娘 
        public List<string> KeInfList { get; set; } //e.g. Ateji. Can a keb have more than one keInf?
        public List<string> KePriList { get; set; } // e.g. gai1

        public KEle() : this(null) { }

        public KEle(string keb)
        {
            Keb = keb;
            KeInfList = new List<string>();
            KePriList = new List<string>();
        }
    }
}
