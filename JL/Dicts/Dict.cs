using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JL.Dicts
{
    public class Dict
    {
        public DictType Type { get; set; }

        public string Path { get; set; }

        public bool Active { get; set; }

        public int Priority { get; set; }

        [JsonIgnore] public Dictionary<string, List<IResult>> Contents { get; set; }

        public Dict(DictType type, string path, bool active, int priority)
        {
            Type = type;
            Path = path;
            Active = active;
            Priority = priority;
        }
    }
}
