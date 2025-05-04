using System.Collections.Frozen;
using System.Text.Json.Serialization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts;

public sealed class Dict<T>(DictType type, string name, string path, bool active, int priority, int size, DictOptions options)
    : DictBase(type, name, path, active, priority, size, options) where T : IDictRecord
{
#pragma warning disable CA2227 // Collection properties should be read only
    [JsonIgnore] public IDictionary<string, IList<T>> Contents { get; set; } = FrozenDictionary<string, IList<T>>.Empty;
#pragma warning restore CA2227 // Collection properties should be read only
}
