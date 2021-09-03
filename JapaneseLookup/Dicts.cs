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
        
        //todo
        // public static readonly Dictionary<string, List<JMdictResult>> JMdict = new();

        // public static readonly Dictionary<string, List<JMnedictResult>> JMnedict = new();

        // public static readonly Dictionary<string, KanjiResult> Kanjidic = new();

        // public static readonly Dictionary<DictType, Dictionary<string, List<EpwingResult>>> EpwingDicts = new();

        // freqdic here?

        //TODO: remove
        // // Indexer declaration
        // public Dictionary<string, object> this[Dict dictName]
        // {
        //     // get and set accessors
        //     get
        //     {
        //         foreach ((Dict key, var value) in dicts)
        //         {
        //             if (key == dictName) return value;
        //         }
        //
        //         return null;
        //     }
        //     set
        //     {
        //         if (dicts.All(dict => dict.Key != dictName))
        //         {
        //             dicts.Add(dictName, value);
        //         }
        //     }
        // }
    }
}