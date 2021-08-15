using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class CustomNameEntry
    {
        public string Spelling { get; set; }
        public string Reading { get; set; }
        public string NameType { get; set; }
        public CustomNameEntry()
        {
            Spelling = null;
            Reading = null;
            NameType = null;
        }
    }
}
