using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class Trans
    {
        public List<string> NameTypeList { get; set; }
        public List<string> XRefList { get; set; }
        public List<string> TransDetList { get; set; }

        public Trans()
        {
            NameTypeList = new List<string>();
            XRefList = new List<string>();
            TransDetList = new List<string>();
        }
    }
}
