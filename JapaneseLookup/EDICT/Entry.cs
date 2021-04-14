using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    abstract class Entry
    {
        public List<KEle> KEleList { get; set; }
        public List<REle> REleList { get; set; }

        public Entry()
        {
            KEleList = new List<KEle>();
            REleList = new List<REle>();
        }
    }
}