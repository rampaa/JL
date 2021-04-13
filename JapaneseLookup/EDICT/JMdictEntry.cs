using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class JMdictEntry : Entry
    {
        public List<Sense> Sense { get; set; }
        public JMdictEntry() : base()
        {
            Sense = new List<Sense>();
        }
    }
}
