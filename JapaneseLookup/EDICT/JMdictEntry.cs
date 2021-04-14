using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class JMdictEntry : Entry
    {
        public List<Sense> SenseList { get; set; }
        public JMdictEntry() : base()
        {
            SenseList = new List<Sense>();
        }
    }
}
