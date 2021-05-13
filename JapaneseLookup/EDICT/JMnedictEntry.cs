using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.EDICT
{
    class JMnedictEntry
    {
        public string Id { get; set; }
        public List<string> KebList { get; set; }
        public List<string> RebList { get; set; }
        public List<Trans> TransList { get; set; }
        public JMnedictEntry()
        {
            Id = null;
            KebList = new List<string>();
            RebList = new List<string>();
            TransList = new List<Trans>();
        }
    }
}
