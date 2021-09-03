using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JapaneseLookup.EDICT;
using JapaneseLookup.EPWING;
using JapaneseLookup.KANJIDIC;

namespace JapaneseLookup
{
    // could move this to ConfigManager
    public static class Dicts
    {
        public static readonly Dictionary<DictType, Dict> dicts = new();
    }
}