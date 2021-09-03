using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using JapaneseLookup.EDICT;

namespace JapaneseLookup
{
    public class Dict
    {
        public DictType Type { get; set; }

        public string Path { get; set; }

        public bool Active { get; set; }

        // [JsonIgnore] public bool Loaded { get; set; }

        [JsonIgnore] public Dictionary<string, List<IResult>> Contents { get; set; }

        public Dict(DictType type, string path, bool active
            // , bool loaded,
        )
        {
            Type = type;
            Path = path;
            Active = active;
            // Loaded = loaded;
        }
    }
}