using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class JMnedictEntry : IEntry
    {
        public List<Trans> Trans { get; set; }
        public JMnedictEntry() :base()
        {
            Trans = new List<Trans>();
        }
    }
}